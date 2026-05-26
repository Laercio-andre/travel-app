import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { IconComponent } from '../shared/components/icon.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslateModule, IconComponent],
  template: `
    <header class="topbar">
      <a class="brand" routerLink="/dashboard">TravelOps</a>
      <nav class="nav-links">
        <a routerLink="/dashboard" routerLinkActive="active"><app-icon name="star" />{{ 'NAV.DASHBOARD' | translate }}</a>
        <a routerLink="/itineraries" routerLinkActive="active"><app-icon name="route" />{{ 'NAV.ITINERARIES' | translate }}</a>
        <a routerLink="/hotels" routerLinkActive="active"><app-icon name="hotel" />{{ 'NAV.HOTELS' | translate }}</a>
        <a routerLink="/flights" routerLinkActive="active"><app-icon name="plane" />{{ 'NAV.FLIGHTS' | translate }}</a>
        <a routerLink="/reports" routerLinkActive="active"><app-icon name="report" />{{ 'NAV.REPORTS' | translate }}</a>
        @if (auth.role() === 'Admin') {
          <a routerLink="/admin" routerLinkActive="active"><app-icon name="shield" />{{ 'NAV.ADMIN' | translate }}</a>
        }
      </nav>
      <div class="topbar-actions">
        <button type="button" class="icon-button" (click)="toggleLanguage()" [attr.aria-label]="'NAV.LANGUAGE' | translate">
          {{ translate.currentLang === 'pt' ? 'EN' : 'PT' }}
        </button>
        <button type="button" class="icon-button" (click)="theme.toggle()" [attr.aria-label]="'NAV.THEME' | translate">
          {{ theme.theme() === 'light' ? '◐' : '☼' }}
        </button>
        <button type="button" class="ghost icon-label" (click)="auth.logout()"><app-icon name="lock" />{{ 'NAV.LOGOUT' | translate }}</button>
      </div>
    </header>
  `
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  readonly translate = inject(TranslateService);

  toggleLanguage(): void {
    const next = this.translate.currentLang === 'pt' ? 'en' : 'pt';
    localStorage.setItem('travel_app_lang', next);
    this.translate.use(next);
  }
}
