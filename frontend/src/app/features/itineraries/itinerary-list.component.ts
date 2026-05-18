import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { Itinerary } from '../../core/models/travel.models';
import { ItineraryService } from '../../core/services/itinerary.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-itinerary-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslateModule, DatePipe, EmptyStateComponent, FeedbackComponent],
  template: `
    <section class="page-heading compact">
      <div><p>{{ 'ITINERARIES.SUBTITLE' | translate }}</p><h1>{{ 'ITINERARIES.TITLE' | translate }}</h1></div>
    </section>
    <section class="split">
      <form class="panel stack" [formGroup]="form" (ngSubmit)="create()">
        <h2>{{ 'ITINERARIES.NEW' | translate }}</h2>
        <label>{{ 'COMMON.TITLE' | translate }}<input formControlName="title" /></label>
        <label>{{ 'COMMON.DESTINATION' | translate }}<input formControlName="destination" /></label>
        <div class="grid two">
          <label>{{ 'COMMON.START' | translate }}<input type="date" formControlName="startDate" /></label>
          <label>{{ 'COMMON.END' | translate }}<input type="date" formControlName="endDate" /></label>
        </div>
        <label>{{ 'COMMON.BUDGET' | translate }}<input type="number" formControlName="budget" /></label>
        <app-feedback [loading]="saving()" [error]="error()" />
        <button class="primary" [disabled]="form.invalid || saving()">{{ 'COMMON.CREATE' | translate }}</button>
      </form>
      <div class="stack">
        <app-feedback [loading]="loading()" [error]="error()" />
        @if (!loading() && itineraries().length === 0) {
          <app-empty-state title="Sem roteiros" description="Cria o primeiro roteiro para começares a planear." />
        }
        <div class="card-grid">
          @for (itinerary of itineraries(); track itinerary.id) {
            <a class="feature-card" [routerLink]="['/itineraries', itinerary.id]">
              <strong>{{ itinerary.title }}</strong>
              <span>{{ itinerary.destination }}</span>
              <small>{{ itinerary.startDate | date }} - {{ itinerary.endDate | date }}</small>
            </a>
          }
        </div>
      </div>
    </section>
  `
})
export class ItineraryListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ItineraryService);
  readonly itineraries = signal<Itinerary[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    destination: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    budget: [0]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.list().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (items) => this.itineraries.set(items),
      error: () => this.error.set('Não foi possível carregar os roteiros.')
    });
  }

  create(): void {
    this.saving.set(true);
    this.service.create(this.form.getRawValue()).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (item) => {
        this.itineraries.update((items) => [item, ...items]);
        this.form.reset({ title: '', destination: '', startDate: '', endDate: '', budget: 0 });
      },
      error: () => this.error.set('Não foi possível criar o roteiro.')
    });
  }
}
