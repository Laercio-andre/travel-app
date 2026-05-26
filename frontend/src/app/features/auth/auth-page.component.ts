import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';

type AuthMode = 'login' | 'register' | 'forgot' | 'reset';

@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslateModule, FeedbackComponent],
  template: `
    <main class="auth-screen">
      <section class="auth-panel">
        <a class="brand large" routerLink="/auth/login">TravelOps</a>
        <h1>{{ title() | translate }}</h1>
        <form [formGroup]="form" (ngSubmit)="submit()" class="stack">
          @if (mode() === 'register') {
            <div class="grid two">
              <label>{{ 'AUTH.FIRST_NAME' | translate }}<input formControlName="firstName" /></label>
              <label>{{ 'AUTH.LAST_NAME' | translate }}<input formControlName="lastName" /></label>
            </div>
          }
          <label>{{ 'AUTH.EMAIL' | translate }}<input type="email" formControlName="email" /></label>
          @if (mode() !== 'forgot') {
            <label>{{ mode() === 'reset' ? ('AUTH.NEW_PASSWORD' | translate) : ('AUTH.PASSWORD' | translate) }}<input type="password" formControlName="password" /></label>
          }
          @if (mode() === 'reset') {
            <label>Token<input formControlName="token" /></label>
          }
          <app-feedback [loading]="loading()" [error]="error()" [success]="success()" />
          <button class="primary" type="submit" [disabled]="form.invalid || loading()">{{ submitLabel() | translate }}</button>
        </form>
        <div class="auth-links">
          <a routerLink="/auth/login">{{ 'AUTH.LOGIN' | translate }}</a>
          <a routerLink="/auth/register">{{ 'AUTH.REGISTER' | translate }}</a>
          <a routerLink="/auth/forgot-password">{{ 'AUTH.FORGOT' | translate }}</a>
        </div>
      </section>
    </main>
  `
})
export class AuthPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly mode = computed<AuthMode>(() => (this.route.snapshot.data['mode'] as AuthMode) ?? 'login');
  readonly title = computed(() => `AUTH.${this.mode().toUpperCase()}_TITLE`);
  readonly submitLabel = computed(() => `AUTH.${this.mode().toUpperCase()}_ACTION`);

  readonly form = this.fb.nonNullable.group({
    firstName: [''],
    lastName: [''],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    token: ['']
  });

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email');
    const token = this.route.snapshot.queryParamMap.get('token');

    this.form.patchValue({
      email: email ?? '',
      token: token ?? ''
    });
  }

  submit(): void {
    this.error.set('');
    this.success.set('');

    if (!this.validateMode()) {
      return;
    }

    this.loading.set(true);
    const value = this.form.getRawValue();

    const request: Observable<unknown> =
      this.mode() === 'register'
        ? this.auth.register({ email: value.email, password: value.password, firstName: value.firstName, lastName: value.lastName })
        : this.mode() === 'forgot'
          ? this.auth.forgotPassword(value.email)
          : this.mode() === 'reset'
            ? this.auth.resetPassword({ email: value.email, token: value.token, newPassword: value.password })
            : this.auth.login({ email: value.email, password: value.password });

    request.pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (result) => {
        if (this.mode() === 'forgot') {
          const response = result as { resetToken?: string | null; resetUrl?: string | null };
          this.success.set(response.resetToken
            ? this.translate.instant('AUTH.FORGOT_DEV_SUCCESS', { token: response.resetToken, url: response.resetUrl })
            : 'AUTH.FORGOT_SUCCESS');
          return;
        }

        if (this.mode() === 'reset') {
          this.success.set('AUTH.RESET_SUCCESS');
          return;
        }
        this.router.navigateByUrl('/dashboard');
      },
      error: (err: HttpErrorResponse) => this.error.set(this.authError(err))
    });
  }

  private validateMode(): boolean {
    const value = this.form.getRawValue();

    if (this.form.controls.email.invalid) {
      this.error.set('AUTH.ERROR_INVALID_EMAIL');
      return false;
    }

    if (this.mode() === 'register' && (!value.firstName.trim() || !value.lastName.trim())) {
      this.error.set('AUTH.ERROR_NAME_REQUIRED');
      return false;
    }

    if (this.mode() !== 'forgot' && !value.password.trim()) {
      this.error.set('AUTH.ERROR_PASSWORD_REQUIRED');
      return false;
    }

    if (this.mode() === 'reset' && !value.token.trim()) {
      this.error.set('AUTH.ERROR_TOKEN_REQUIRED');
      return false;
    }

    return true;
  }

  private authError(err: HttpErrorResponse): string {
    const code = err.error?.error ?? err.error?.message ?? '';
    const identityMessage = typeof code === 'string' ? code : '';

    if (identityMessage.includes('DuplicateEmail') || identityMessage === 'EMAIL_ALREADY_EXISTS') {
      return 'AUTH.ERROR_EMAIL_EXISTS';
    }

    if (identityMessage.includes('Passwords must have at least one digit') || identityMessage.includes('digit')) {
      return 'AUTH.ERROR_PASSWORD_DIGIT';
    }

    if (identityMessage.includes('PasswordTooShort')) {
      return 'AUTH.ERROR_PASSWORD_SHORT';
    }

    if (identityMessage === 'INVALID_CREDENTIALS') {
      return 'AUTH.ERROR_INVALID_CREDENTIALS';
    }

    return 'AUTH.ERROR_GENERIC';
  }
}
