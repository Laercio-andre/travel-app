import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthUser, LoginRequest, RegisterRequest, ResetPasswordRequest, UserRole } from '../models/auth.models';

const STORAGE_KEY = 'travel_app_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = environment.apiUrl;

  readonly currentUser = signal<AuthUser | null>(this.restoreUser());
  readonly isRefreshing = signal(false);
  readonly isAuthenticated = computed(() => !!this.currentUser()?.accessToken);
  readonly role = computed<UserRole | null>(() => this.currentUser()?.role ?? null);

  login(payload: LoginRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.apiUrl}/api/auth/login`, payload).pipe(tap((user) => this.setSession(user)));
  }

  register(payload: RegisterRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.apiUrl}/api/auth/register`, payload).pipe(tap((user) => this.setSession(user)));
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/api/auth/logout`, {}).pipe(catchError(() => throwError(() => null))).subscribe();
    localStorage.removeItem(STORAGE_KEY);
    this.currentUser.set(null);
    this.router.navigateByUrl('/auth/login');
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/api/auth/forgot-password`, { email });
  }

  resetPassword(payload: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/api/auth/reset-password`, payload);
  }

  profile(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${this.apiUrl}/api/auth/profile`);
  }

  updateProfile(payload: Partial<AuthUser>): Observable<AuthUser> {
    return this.http.put<AuthUser>(`${this.apiUrl}/api/auth/profile`, payload).pipe(tap((user) => this.setSession({ ...this.currentUser(), ...user } as AuthUser)));
  }

  refreshToken(): Observable<AuthUser> {
    const refreshToken = this.currentUser()?.refreshToken;
    if (!refreshToken) {
      return throwError(() => new Error('Missing refresh token'));
    }

    this.isRefreshing.set(true);
    return this.http.post<AuthUser>(`${this.apiUrl}/api/auth/refresh`, { refreshToken }).pipe(
      tap((user) => this.setSession(user)),
      finalize(() => this.isRefreshing.set(false))
    );
  }

  setSession(user: AuthUser): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  hasRole(roles: UserRole[]): boolean {
    const role = this.currentUser()?.role;
    return !!role && roles.includes(role);
  }

  get accessToken(): string | null {
    return this.currentUser()?.accessToken ?? null;
  }

  private restoreUser(): AuthUser | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
