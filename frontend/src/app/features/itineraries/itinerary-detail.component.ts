import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { GoogleMap, MapMarker } from '@angular/google-maps';
import { finalize } from 'rxjs';
import { ChatMessage, Itinerary, ItineraryStop } from '../../core/models/travel.models';
import { AiService } from '../../core/services/ai.service';
import { GoogleMapsLoaderService } from '../../core/services/google-maps-loader.service';
import { ItineraryService } from '../../core/services/itinerary.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-itinerary-detail',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, GoogleMap, MapMarker, FeedbackComponent],
  template: `
    @if (itinerary(); as item) {
      <section class="page-heading compact">
        <div><p>{{ item.destination }}</p><h1>{{ item.title }}</h1><span>{{ item.startDate | date }} - {{ item.endDate | date }}</span></div>
      </section>
      <section class="detail-grid">
        <div class="panel stack">
          <h2>Mapa do roteiro</h2>
          @if (mapsReady()) {
            <google-map height="360px" width="100%" [center]="center()" [zoom]="12">
              @for (stop of item.stops ?? []; track stop.id) {
                <map-marker [position]="{ lat: stop.latitude, lng: stop.longitude }" [title]="stop.name" />
              }
            </google-map>
          } @else {
            <div class="empty-state">
              Configura <strong>googleMapsApiKey</strong> em <strong>src/environments/environment.ts</strong> para ativar o mapa.
            </div>
          }
          <form [formGroup]="stopForm" (ngSubmit)="addStop()" class="stack">
            <h3>Novo ponto</h3>
            <label>Nome<input formControlName="name" /></label>
            <div class="grid two">
              <label>Latitude<input type="number" formControlName="latitude" /></label>
              <label>Longitude<input type="number" formControlName="longitude" /></label>
            </div>
            <label>Notas<textarea formControlName="notes"></textarea></label>
            <button class="primary" [disabled]="stopForm.invalid || loading()">Adicionar ponto</button>
          </form>
          <div class="chips">
            @for (stop of item.stops ?? []; track stop.id) {
              <button class="chip" type="button" (click)="removeStop(stop.id)">{{ stop.name }} x</button>
            }
          </div>
        </div>
        <div class="panel chat stack">
          <h2>Assistente IA</h2>
          <div class="messages">
            @for (message of messages(); track message.id ?? message.createdAt ?? message.content) {
              <p [class.assistant]="message.role === 'assistant'"><strong>{{ message.role }}:</strong> {{ message.content }}</p>
            }
          </div>
          <form [formGroup]="chatForm" (ngSubmit)="sendMessage()" class="chat-form">
            <input formControlName="message" placeholder="Pede sugestões, otimizações ou estimativas..." />
            <button class="primary" [disabled]="chatForm.invalid || loading()">Enviar</button>
          </form>
          <app-feedback [loading]="loading()" [error]="error()" />
        </div>
      </section>
    }
  `
})
export class ItineraryDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly itineraries = inject(ItineraryService);
  private readonly ai = inject(AiService);
  private readonly maps = inject(GoogleMapsLoaderService);
  readonly itinerary = signal<Itinerary | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly loading = signal(false);
  readonly mapsReady = signal(false);
  readonly error = signal('');
  readonly center = computed(() => {
    const first = this.itinerary()?.stops?.[0];
    return first ? { lat: first.latitude, lng: first.longitude } : { lat: 38.7223, lng: -9.1393 };
  });
  readonly stopForm = this.fb.nonNullable.group({ name: ['', Validators.required], latitude: [0, Validators.required], longitude: [0, Validators.required], notes: [''] });
  readonly chatForm = this.fb.nonNullable.group({ message: ['', Validators.required] });

  ngOnInit(): void {
    this.maps.load().then((ready) => this.mapsReady.set(ready)).catch(() => this.mapsReady.set(false));
    this.load();
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.itineraries.get(id).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (item) => {
        this.itinerary.set(item);
        this.ai.history(id).subscribe((messages) => this.messages.set(messages));
      },
      error: () => this.error.set('Não foi possível carregar o roteiro.')
    });
  }

  addStop(): void {
    const item = this.itinerary();
    if (!item) return;
    this.loading.set(true);
    this.itineraries.addStop(item.id, this.stopForm.getRawValue()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (stop) => {
        this.itinerary.update((current) => ({ ...current!, stops: [...(current?.stops ?? []), stop] }));
        this.stopForm.reset({ name: '', latitude: 0, longitude: 0, notes: '' });
      },
      error: () => this.error.set('Não foi possível adicionar o ponto.')
    });
  }

  removeStop(stopId: string): void {
    const item = this.itinerary();
    if (!item) return;
    this.itineraries.removeStop(item.id, stopId).subscribe(() => this.itinerary.update((current) => ({ ...current!, stops: current?.stops?.filter((stop) => stop.id !== stopId) ?? [] })));
  }

  sendMessage(): void {
    const item = this.itinerary();
    if (!item) return;
    const content = this.chatForm.controls.message.value;
    this.messages.update((messages) => [...messages, { itineraryId: item.id, role: 'user', content }]);
    this.loading.set(true);
    this.ai.chat(item.id, content).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (message) => {
        this.messages.update((messages) => [...messages, message]);
        this.chatForm.reset({ message: '' });
      },
      error: () => this.error.set('O assistente não respondeu. Tenta novamente.')
    });
  }
}
