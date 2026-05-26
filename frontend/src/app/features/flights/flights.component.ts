import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { Flight, FlightAlert } from '../../core/models/travel.models';
import { FlightService } from '../../core/services/flight.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { FeedbackComponent } from '../../shared/components/feedback.component';
import { IconComponent } from '../../shared/components/icon.component';

@Component({
  selector: 'app-flights',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, TranslateModule, EmptyStateComponent, FeedbackComponent, IconComponent],
  template: `
    <section class="page-heading compact"><p>{{ 'FLIGHTS.SUBTITLE' | translate }}</p><h1><app-icon name="plane" />{{ 'FLIGHTS.TITLE' | translate }}</h1></section>
    <form class="toolbar" [formGroup]="form" (ngSubmit)="search()">
      <input formControlName="origin" [placeholder]="'COMMON.ORIGIN' | translate" />
      <input formControlName="destination" [placeholder]="'COMMON.DESTINATION_CODE' | translate" />
      <input type="date" formControlName="departureDate" />
      <input type="date" formControlName="returnDate" />
      <button class="primary icon-label" [disabled]="form.invalid || loading()"><app-icon name="search" />{{ 'FLIGHTS.COMPARE' | translate }}</button>
    </form>
    <app-feedback [loading]="loading()" [error]="error()" [success]="success()" />
    @if (!loading() && flights().length === 0) {
      <app-empty-state [title]="'FLIGHTS.EMPTY_TITLE' | translate" [description]="'FLIGHTS.EMPTY_DESCRIPTION' | translate" />
    }
    <section class="card-grid">
      @for (flight of flights(); track flight.id) {
        <article class="feature-card">
          <strong>{{ flight.airline }}</strong>
          <span>{{ flight.origin }} -> {{ flight.destination }}</span>
          <small>{{ flight.departureAt | date:'short' }} - {{ flight.price | currency:flight.currency }}</small>
          <div class="row">
            <button class="primary icon-label" type="button" (click)="book(flight)"><app-icon name="check" />{{ 'COMMON.BOOK' | translate }}</button>
            <button class="ghost icon-label" type="button" (click)="createAlert(flight)"><app-icon name="mail" />{{ 'FLIGHTS.ALERT' | translate }}</button>
          </div>
        </article>
      }
    </section>
    <section class="panel stack">
      <h2>{{ 'FLIGHTS.ALERTS' | translate }}</h2>
      @for (alert of alerts(); track alert.id) {
        <div class="row">
          <span>{{ alert.origin }} -> {{ alert.destination }} | {{ alert.targetPrice | currency }} | {{ (alert.enabled ? 'COMMON.ACTIVE' : 'COMMON.PAUSED') | translate }}</span>
          <div>
            <button class="ghost" (click)="toggle(alert)">{{ 'COMMON.TOGGLE' | translate }}</button>
            <button class="ghost" (click)="removeAlert(alert.id)">{{ 'COMMON.REMOVE' | translate }}</button>
          </div>
        </div>
      }
    </section>
  `
})
export class FlightsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FlightService);
  readonly flights = signal<Flight[]>([]);
  readonly alerts = signal<FlightAlert[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly form = this.fb.nonNullable.group({ origin: ['', Validators.required], destination: ['', Validators.required], departureDate: ['', Validators.required], returnDate: [''] });

  ngOnInit(): void {
    this.form.patchValue({ origin: 'LAD', destination: 'SDD', departureDate: this.defaultDepartureDate(), returnDate: '' });
    this.search();
    this.service.alerts().subscribe((items) => this.alerts.set(items));
  }

  search(): void {
    this.loading.set(true);
    this.service.search(this.form.getRawValue()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (items) => this.flights.set(items),
      error: () => this.error.set('FLIGHTS.SEARCH_ERROR')
    });
  }

  book(flight: Flight): void {
    this.service.book({ flightId: flight.id }).subscribe({
      next: () => this.success.set('FLIGHTS.BOOK_SUCCESS'),
      error: () => this.error.set('FLIGHTS.BOOK_ERROR')
    });
  }

  createAlert(flight: Flight): void {
    this.service.createAlert({ origin: flight.origin, destination: flight.destination, targetPrice: flight.price, enabled: true }).subscribe((alert) => this.alerts.update((items) => [alert, ...items]));
  }

  toggle(alert: FlightAlert): void {
    this.service.toggleAlert(alert.id).subscribe(() => this.alerts.update((items) => items.map((item) => (item.id === alert.id ? { ...item, enabled: !item.enabled } : item))));
  }

  removeAlert(id: string): void {
    this.service.deleteAlert(id).subscribe(() => this.alerts.update((items) => items.filter((alert) => alert.id !== id)));
  }

  private defaultDepartureDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + 10);
    return date.toISOString().slice(0, 10);
  }
}
