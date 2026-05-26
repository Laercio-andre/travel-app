import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { forkJoin, finalize } from 'rxjs';
import { Flight, Hotel, Itinerary } from '../../core/models/travel.models';
import { AuthService } from '../../core/services/auth.service';
import { FlightService } from '../../core/services/flight.service';
import { HotelService } from '../../core/services/hotel.service';
import { ItineraryService } from '../../core/services/itinerary.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';
import { IconComponent } from '../../shared/components/icon.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslateModule, CurrencyPipe, DatePipe, FeedbackComponent, IconComponent],
  template: `
    <section class="dashboard-hero">
      <div>
        <p>{{ 'DASHBOARD.WELCOME_BACK' | translate }}</p>
        <h1>{{ auth.currentUser()?.firstName || 'Traveler' }}</h1>
        <span>{{ 'DASHBOARD.AUTO_LOAD' | translate }}</span>
      </div>
      <a class="primary icon-label" routerLink="/itineraries"><app-icon name="route" />{{ 'ITINERARIES.NEW' | translate }}</a>
    </section>

    <app-feedback [loading]="loading()" [error]="error()" />

    <section class="metrics">
      <article><app-icon name="route" /><span>{{ 'NAV.ITINERARIES' | translate }}</span><strong>{{ itineraries().length }}</strong></article>
      <article><app-icon name="hotel" /><span>{{ 'NAV.HOTELS' | translate }}</span><strong>{{ hotels().length }}</strong></article>
      <article><app-icon name="plane" /><span>{{ 'NAV.FLIGHTS' | translate }}</span><strong>{{ flights().length }}</strong></article>
    </section>

    <section class="dashboard-grid">
      <div class="panel stack">
        <div class="section-title"><app-icon name="route" /><h2>{{ 'NAV.ITINERARIES' | translate }}</h2><a routerLink="/itineraries">{{ 'COMMON.VIEW_ALL' | translate }}</a></div>
        @for (item of itineraries().slice(0, 3); track item.id) {
          <a class="data-row" [routerLink]="['/itineraries', item.id]">
            <span><strong>{{ item.title }}</strong><small>{{ item.destination }}</small></span>
            <small>{{ item.startDate | date }}</small>
          </a>
        }
      </div>

      <div class="panel stack">
        <div class="section-title"><app-icon name="hotel" /><h2>{{ 'NAV.HOTELS' | translate }}</h2><a routerLink="/hotels">{{ 'COMMON.VIEW_ALL' | translate }}</a></div>
        @for (hotel of hotels().slice(0, 3); track hotel.id) {
          <a class="media-row" routerLink="/hotels">
            <img [src]="hotel.imageUrl || fallbackHotelImage" [alt]="hotel.name" (error)="useFallbackImage($event)" />
            <span><strong>{{ hotel.name }}</strong><small>{{ hotel.city }} · {{ hotel.pricePerNight | currency:hotel.currencyCode }}</small></span>
          </a>
        }
      </div>

      <div class="panel stack">
        <div class="section-title"><app-icon name="plane" /><h2>{{ 'NAV.FLIGHTS' | translate }}</h2><a routerLink="/flights">{{ 'COMMON.VIEW_ALL' | translate }}</a></div>
        @for (flight of flights().slice(0, 3); track flight.id) {
          <a class="data-row" routerLink="/flights">
            <span><strong>{{ flight.airline }}</strong><small>{{ flight.origin }} → {{ flight.destination }}</small></span>
            <small>{{ flight.price | currency:flight.currency }}</small>
          </a>
        }
      </div>
    </section>
  `
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly itineraryService = inject(ItineraryService);
  private readonly hotelService = inject(HotelService);
  private readonly flightService = inject(FlightService);

  readonly itineraries = signal<Itinerary[]>([]);
  readonly hotels = signal<Hotel[]>([]);
  readonly flights = signal<Flight[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly fallbackHotelImage = 'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80';

  ngOnInit(): void {
    this.loading.set(true);
    const dates = this.defaultDates();
    forkJoin({
      itineraries: this.itineraryService.list(),
      hotels: this.hotelService.search({ city: 'Luanda', checkIn: dates.checkIn, checkOut: dates.checkOut, guests: 2 }),
      flights: this.flightService.search({ origin: 'LAD', destination: 'SDD', departureDate: dates.departureDate })
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: ({ itineraries, hotels, flights }) => {
        this.itineraries.set(itineraries);
        this.hotels.set(hotels);
        this.flights.set(flights);
      },
      error: () => this.error.set('DASHBOARD.LOAD_ERROR')
    });
  }

  useFallbackImage(event: Event): void {
    (event.target as HTMLImageElement).src = this.fallbackHotelImage;
  }

  private defaultDates(): { checkIn: string; checkOut: string; departureDate: string } {
    const checkIn = new Date();
    checkIn.setDate(checkIn.getDate() + 10);
    const checkOut = new Date(checkIn);
    checkOut.setDate(checkOut.getDate() + 2);
    return {
      checkIn: this.toDateInput(checkIn),
      checkOut: this.toDateInput(checkOut),
      departureDate: this.toDateInput(checkIn)
    };
  }

  private toDateInput(date: Date): string {
    return date.toISOString().slice(0, 10);
  }
}
