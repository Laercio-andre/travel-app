import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { Booking, Hotel } from '../../core/models/travel.models';
import { HotelService } from '../../core/services/hotel.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { FeedbackComponent } from '../../shared/components/feedback.component';
import { IconComponent } from '../../shared/components/icon.component';

@Component({
  selector: 'app-hotels',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, TranslateModule, EmptyStateComponent, FeedbackComponent, IconComponent],
  template: `
    <section class="page-heading compact"><p>{{ 'HOTELS.SUBTITLE' | translate }}</p><h1><app-icon name="hotel" />{{ 'HOTELS.TITLE' | translate }}</h1></section>
    <form class="toolbar" [formGroup]="form" (ngSubmit)="search()">
      <input formControlName="city" [placeholder]="'COMMON.CITY' | translate" />
      <input type="date" formControlName="checkIn" />
      <input type="date" formControlName="checkOut" />
      <input type="number" formControlName="guests" min="1" />
      <button class="primary icon-label" [disabled]="form.invalid || loading()"><app-icon name="search" />{{ 'COMMON.SEARCH' | translate }}</button>
    </form>
    <app-feedback [loading]="loading()" [error]="error()" [success]="success()" />
    @if (!loading() && hotels().length === 0) {
      <app-empty-state [title]="'HOTELS.EMPTY_TITLE' | translate" [description]="'HOTELS.EMPTY_DESCRIPTION' | translate" />
    }
    <section class="card-grid">
      @for (hotel of hotels(); track hotel.id) {
        <article class="feature-card">
          <img [src]="hotel.imageUrl || fallbackHotelImage" [alt]="hotel.name" (error)="useFallbackImage($event)" />
          <strong>{{ hotel.name }}</strong>
          <span>{{ hotel.city }} {{ hotel.country }}</span>
          <small>{{ hotel.rating || 0 }}/5 - {{ hotel.pricePerNight | currency }}</small>
          <button class="primary icon-label" type="button" (click)="book(hotel)"><app-icon name="check" />{{ 'COMMON.BOOK' | translate }}</button>
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
  readonly fallbackHotelImage = 'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80';
  readonly form = this.fb.nonNullable.group({ city: ['', Validators.required], checkIn: [''], checkOut: [''], guests: [1, Validators.required] });

  ngOnInit(): void {
    const dates = this.defaultDates();
    this.form.patchValue({ city: 'Luanda', checkIn: dates.checkIn, checkOut: dates.checkOut, guests: 2 });
    this.search();
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

  useFallbackImage(event: Event): void {
    (event.target as HTMLImageElement).src = this.fallbackHotelImage;
  }

  private defaultDates(): { checkIn: string; checkOut: string } {
    const checkIn = new Date();
    checkIn.setDate(checkIn.getDate() + 7);
    const checkOut = new Date(checkIn);
    checkOut.setDate(checkOut.getDate() + 2);
    return { checkIn: this.toDateInput(checkIn), checkOut: this.toDateInput(checkOut) };
  }

  private toDateInput(date: Date): string {
    return date.toISOString().slice(0, 10);
  }
}
