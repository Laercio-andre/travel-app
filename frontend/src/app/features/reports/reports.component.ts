import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Itinerary, ReportSummary } from '../../core/models/travel.models';
import { ItineraryService } from '../../core/services/itinerary.service';
import { ReportService } from '../../core/services/report.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, FeedbackComponent],
  template: `
    <section class="page-heading compact"><p>Finanças</p><h1>Relatórios e exportação</h1></section>
    <form class="toolbar" [formGroup]="form" (ngSubmit)="loadSummary()">
      <select formControlName="itineraryId">
        <option value="">Selecionar roteiro</option>
        @for (itinerary of itineraries(); track itinerary.id) {
          <option [value]="itinerary.id">{{ itinerary.title }}</option>
        }
      </select>
      <button class="primary" [disabled]="form.invalid || loading()">Ver resumo</button>
      <button class="ghost" type="button" [disabled]="form.invalid" (click)="exportPdf()">PDF</button>
      <button class="ghost" type="button" [disabled]="form.invalid" (click)="exportCsv()">CSV</button>
    </form>
    <app-feedback [loading]="loading()" [error]="error()" />
    @if (summary(); as report) {
      <section class="metrics">
        <article><span>Orçamento</span><strong>{{ report.totalBudget || 0 | currency }}</strong></article>
        <article><span>Gasto</span><strong>{{ report.totalSpent | currency }}</strong></article>
        <article><span>Saldo</span><strong>{{ report.balance || 0 | currency }}</strong></article>
      </section>
      <section class="panel stack">
        <h2>Despesas</h2>
        @for (expense of report.expenses; track expense.id) {
          <div class="row"><span>{{ expense.category }} - {{ expense.description }}</span><strong>{{ expense.amount | currency }}</strong><small>{{ expense.date | date }}</small></div>
        }
      </section>
    }
  `
})
export class ReportsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly itinerariesService = inject(ItineraryService);
  private readonly reports = inject(ReportService);
  readonly itineraries = signal<Itinerary[]>([]);
  readonly summary = signal<ReportSummary | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly form = this.fb.nonNullable.group({ itineraryId: ['', Validators.required] });

  ngOnInit(): void {
    this.itinerariesService.list().subscribe((items) => this.itineraries.set(items));
  }

  loadSummary(): void {
    this.loading.set(true);
    this.reports.summary(this.form.controls.itineraryId.value).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (summary) => this.summary.set(summary),
      error: () => this.error.set('Não foi possível carregar o relatório.')
    });
  }

  exportPdf(): void {
    this.reports.exportPdf(this.form.controls.itineraryId.value);
  }

  exportCsv(): void {
    this.reports.exportCsv(this.form.controls.itineraryId.value);
  }
}
