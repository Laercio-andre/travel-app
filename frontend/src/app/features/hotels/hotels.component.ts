import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { Booking, Hotel } from '../../core/models/travel.models';
import { HotelService } from '../../core/services/hotel.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-hotels',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, TranslateModule, EmptyStateComponent, FeedbackComponent],
  template: `
    <section class="page-heading compact"><p>{{ 'HOTELS.SUBTITLE' | translate }}</p><h1>{{ 'HOTELS.TITLE' | translate }}</h1></section>
    <form class="toolbar" [formGroup]="form" (ngSubmit)="search()">
      <input formControlName="city" [placeholder]="'COMMON.CITY' | translate" />
      <input type="date" formControlName="checkIn" />
      <input type="date" formControlName="checkOut" />
      <input type="number" formControlName="guests" min="1" />
      <button class="primary" [disabled]="form.invalid || loading()">{{ 'COMMON.SEARCH' | translate }}</button>
    </form>
    <app-feedback [loading]="loading()" [error]="error()" [success]="success()" />
    @if (!loading() && hotels().length === 0) {
      <app-empty-state [title]="'HOTELS.EMPTY_TITLE' | translate" [description]="'HOTELS.EMPTY_DESCRIPTION' | translate" />
    }
    <section class="card-grid">
      @for (hotel of hotels(); track hotel.id) {
        <article class="feature-card">
          @if (hotel.imageUrl) { <img [src]="hotel.imageUrl" [alt]="hotel.name" /> }
          <strong>{{ hotel.name }}</strong>
          <span>{{ hotel.city }} {{ hotel.country }}</span>
          <small>{{ hotel.rating || 0 }}/5 - {{ hotel.pricePerNight | currency }}</small>
          <button class="primary" type="button" (click)="book(hotel)">{{ 'COMMON.BOOK' | translate }}</button>
        </article>
      }
    </section>
    <section class="panel stack">
      <h2>{{ 'HOTELS.BOOKINGS' | translate }}</h2>
      @for (booking of bookings(); track booking.id) {
        <div class="row">
          <span>{{ booking.status }} - {{ booking.totalPrice | currency }}</span>
          <button class="ghost" type="button" (click)="cancel(booking.id)">{{ 'COMMON.CANCEL' | translate }}</button>
        </div>
      }
    </section>
  `
})
export class HotelsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(HotelService);
  readonly hotels = signal<Hotel[]>([]);
  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly form = this.fb.nonNullable.group({ city: ['', Validators.required], checkIn: [''], checkOut: [''], guests: [1, Validators.required] });

  ngOnInit(): void {
    this.service.bookings().subscribe((items) => this.bookings.set(items));
  }

  search(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.search(this.form.getRawValue()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (items) => this.hotels.set(items),
      error: () => this.error.set('HOTELS.SEARCH_ERROR')
    });
  }

  book(hotel: Hotel): void {
    this.service.book({ hotelId: hotel.id, ...this.form.getRawValue() }).subscribe({
      next: (booking) => {
        this.bookings.update((items) => [booking, ...items]);
        this.success.set('HOTELS.BOOK_SUCCESS');
      },
      error: () => this.error.set('HOTELS.BOOK_ERROR')
    });
  }

  cancel(id: string): void {
    this.service.cancelBooking(id).subscribe(() => this.bookings.update((items) => items.filter((booking) => booking.id !== id)));
  }
}
