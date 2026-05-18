import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

const refreshSubject = new BehaviorSubject<string | null>(null);
let refreshInFlight = false;

export const jwtInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const isAuthEndpoint = request.url.includes('/api/auth/login') || request.url.includes('/api/auth/register') || request.url.includes('/api/auth/refresh');
  const token = auth.accessToken;
  const authRequest = token && !isAuthEndpoint ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(authRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      if (refreshInFlight) {
        return refreshSubject.pipe(
          filter(Boolean),
          take(1),
          switchMap((newToken) => next(request.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })))
        );
      }

      refreshInFlight = true;
      refreshSubject.next(null);

      return auth.refreshToken().pipe(
        switchMap((user) => {
          refreshInFlight = false;
          refreshSubject.next(user.accessToken);
          return next(request.clone({ setHeaders: { Authorization: `Bearer ${user.accessToken}` } }));
        }),
        catchError((refreshError) => {
          refreshInFlight = false;
          refreshSubject.next(null);
          localStorage.removeItem('travel_app_auth');
          auth.currentUser.set(null);
          router.navigateByUrl('/auth/login');
          return throwError(() => refreshError);
        })
      );
    })
  );
};
