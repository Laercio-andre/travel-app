import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Flight, FlightAlert } from '../../core/models/travel.models';
import { FlightService } from '../../core/services/flight.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-flights',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, EmptyStateComponent, FeedbackComponent],
  template: `
    <section class="page-heading compact"><p>Voos</p><h1>Comparação e alertas</h1></section>
    <form class="toolbar" [formGroup]="form" (ngSubmit)="search()">
      <input formControlName="origin" placeholder="Origem" />
      <input formControlName="destination" placeholder="Destino" />
      <input type="date" formControlName="departureDate" />
      <input type="date" formControlName="returnDate" />
      <button class="primary" [disabled]="form.invalid || loading()">Comparar</button>
    </form>
    <app-feedback [loading]="loading()" [error]="error()" [success]="success()" />
    @if (!loading() && flights().length === 0) {
      <app-empty-state title="Sem voos" description="Preenche origem e destino para comparar preços." />
    }
    <section class="card-grid">
      @for (flight of flights(); track flight.id) {
        <article class="feature-card">
          <strong>{{ flight.airline }}</strong>
          <span>{{ flight.origin }} -> {{ flight.destination }}</span>
          <small>{{ flight.departureAt | date:'short' }} - {{ flight.price | currency:flight.currency }}</small>
          <div class="row">
            <button class="primary" type="button" (click)="book(flight)">Reservar</button>
            <button class="ghost" type="button" (click)="createAlert(flight)">Alerta</button>
          </div>
        </article>
      }
    </section>
    <section class="panel stack">
      <h2>Alertas de preço</h2>
      @for (alert of alerts(); track alert.id) {
        <div class="row">
          <span>{{ alert.origin }} -> {{ alert.destination }} | {{ alert.targetPrice | currency }} | {{ alert.enabled ? 'ativo' : 'pausado' }}</span>
          <div>
            <button class="ghost" (click)="toggle(alert)">Alternar</button>
            <button class="ghost" (click)="removeAlert(alert.id)">Remover</button>
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
    this.service.alerts().subscribe((items) => this.alerts.set(items));
  }

  search(): void {
    this.loading.set(true);
    this.service.search(this.form.getRawValue()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (items) => this.flights.set(items),
      error: () => this.error.set('Não foi possível pesquisar voos.')
    });
  }

  book(flight: Flight): void {
    this.service.book({ flightId: flight.id }).subscribe({
      next: () => this.success.set('Reserva de voo criada com sucesso.'),
      error: () => this.error.set('Não foi possível reservar o voo.')
    });
  }

  createAlert(flight: Flight): void {
    this.service.createAlert({ origin: flight.origin, destination: flight.destination, targetPrice: flight.price, enabled: true }).subscribe((alert) => this.alerts.update((items) => [alert, ...items]));
  }

  toggle(alert: FlightAlert): void {
    this.service.toggleAlert(alert.id).subscribe((updated) => this.alerts.update((items) => items.map((item) => (item.id === updated.id ? updated : item))));
  }

  removeAlert(id: string): void {
    this.service.deleteAlert(id).subscribe(() => this.alerts.update((items) => items.filter((alert) => alert.id !== id)));
  }
}
