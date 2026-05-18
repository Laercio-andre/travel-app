import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ReportSummary } from '../models/travel.models';
import { downloadBlob } from './api-utils';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/reports`;

  summary(itineraryId: string) {
    return this.http.get<ReportSummary>(`${this.base}/summary/${itineraryId}`);
  }

  exportPdf(itineraryId: string) {
    return this.http.post(`${this.base}/pdf`, { itineraryId }, { responseType: 'blob' }).subscribe((blob) => downloadBlob(blob, `itinerary-${itineraryId}.pdf`));
  }

  exportCsv(itineraryId: string) {
    return this.http.post(`${this.base}/csv`, { itineraryId }, { responseType: 'blob' }).subscribe((blob) => downloadBlob(blob, `itinerary-${itineraryId}.csv`));
  }
}
