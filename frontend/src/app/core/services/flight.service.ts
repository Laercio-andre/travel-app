import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Booking, Flight, FlightAlert } from '../models/travel.models';
import { unwrapList } from './api-utils';

@Injectable({ providedIn: 'root' })
export class FlightService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/flights`;

  search(query: Record<string, string | number | undefined>) {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== '') params = params.set(key, String(value));
    });
    return this.http.get<Flight[] | { items?: Flight[]; data?: Flight[] }>(`${this.base}/search`, { params }).pipe(map(unwrapList));
  }

  book(payload: Record<string, unknown>) {
    return this.http.post<Booking>(`${this.base}/bookings`, payload);
  }

  alerts() {
    return this.http.get<FlightAlert[] | { items?: FlightAlert[]; data?: FlightAlert[] }>(`${this.base}/alerts`).pipe(map(unwrapList));
  }

  createAlert(payload: Partial<FlightAlert>) {
    return this.http.post<FlightAlert>(`${this.base}/alerts`, payload);
  }

  deleteAlert(id: string) {
    return this.http.delete<void>(`${this.base}/alerts/${id}`);
  }

  toggleAlert(id: string) {
    return this.http.patch<FlightAlert>(`${this.base}/alerts/${id}/toggle`, {});
  }
}
