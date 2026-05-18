import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Booking, Hotel } from '../models/travel.models';
import { unwrapList } from './api-utils';

@Injectable({ providedIn: 'root' })
export class HotelService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/hotels`;

  search(query: Record<string, string | number | undefined>) {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== '') params = params.set(key, String(value));
    });
    return this.http.get<Hotel[] | { items?: Hotel[]; data?: Hotel[] }>(`${this.base}/search`, { params }).pipe(map(unwrapList));
  }

  get(id: string) {
    return this.http.get<Hotel>(`${this.base}/${id}`);
  }

  bookings() {
    return this.http.get<Booking[] | { items?: Booking[]; data?: Booking[] }>(`${this.base}/bookings`).pipe(map(unwrapList));
  }

  book(payload: Record<string, unknown>) {
    return this.http.post<Booking>(`${this.base}/bookings`, payload);
  }

  cancelBooking(id: string) {
    return this.http.delete<void>(`${this.base}/bookings/${id}`);
  }
}
