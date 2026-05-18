import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Itinerary, ItineraryStop } from '../models/travel.models';
import { unwrapList } from './api-utils';

@Injectable({ providedIn: 'root' })
export class ItineraryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/itineraries`;

  list() {
    return this.http.get<Itinerary[] | { items?: Itinerary[]; data?: Itinerary[] }>(this.base).pipe(map(unwrapList));
  }

  get(id: string) {
    return this.http.get<Itinerary>(`${this.base}/${id}`);
  }

  create(payload: Partial<Itinerary>) {
    return this.http.post<Itinerary>(this.base, payload);
  }

  update(id: string, payload: Partial<Itinerary>) {
    return this.http.put<Itinerary>(`${this.base}/${id}`, payload);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  addStop(id: string, payload: Partial<ItineraryStop>) {
    return this.http.post<ItineraryStop>(`${this.base}/${id}/stops`, payload);
  }

  removeStop(id: string, stopId: string) {
    return this.http.delete<void>(`${this.base}/${id}/stops/${stopId}`);
  }
}
