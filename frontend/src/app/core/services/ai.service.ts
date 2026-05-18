import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatMessage } from '../models/travel.models';
import { unwrapList } from './api-utils';

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/ai`;

  chat(itineraryId: string, message: string) {
    return this.http.post<ChatMessage>(`${this.base}/chat`, { itineraryId, message });
  }

  suggest(itineraryId: string, prompt: string) {
    return this.http.post<ChatMessage | { suggestions: string[] }>(`${this.base}/suggest`, { itineraryId, prompt });
  }

  history(itineraryId: string) {
    return this.http.get<ChatMessage[] | { items?: ChatMessage[]; data?: ChatMessage[] }>(`${this.base}/chat/${itineraryId}`).pipe(map(unwrapList));
  }
}
