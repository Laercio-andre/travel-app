import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  template: `
    <section class="page-heading">
      <p>{{ 'DASHBOARD.WELCOME_BACK' | translate }}</p>
      <h1>{{ auth.currentUser()?.firstName || 'Traveler' }}</h1>
    </section>
    <section class="action-grid">
      <a class="feature-card" routerLink="/itineraries"><strong>{{ 'NAV.ITINERARIES' | translate }}</strong><span>{{ 'DASHBOARD.ITINERARIES_COPY' | translate }}</span></a>
      <a class="feature-card" routerLink="/hotels"><strong>{{ 'NAV.HOTELS' | translate }}</strong><span>{{ 'DASHBOARD.HOTELS_COPY' | translate }}</span></a>
      <a class="feature-card" routerLink="/flights"><strong>{{ 'NAV.FLIGHTS' | translate }}</strong><span>{{ 'DASHBOARD.FLIGHTS_COPY' | translate }}</span></a>
      <a class="feature-card" routerLink="/reports"><strong>{{ 'NAV.REPORTS' | translate }}</strong><span>{{ 'DASHBOARD.REPORTS_COPY' | translate }}</span></a>
    </section>
  `
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
}
