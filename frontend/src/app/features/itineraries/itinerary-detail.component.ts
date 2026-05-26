import { DatePipe } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { ChatMessage, Itinerary, ItineraryStop } from '../../core/models/travel.models';
import { AiService } from '../../core/services/ai.service';
import { ItineraryService } from '../../core/services/itinerary.service';
import { LeafletLoaderService } from '../../core/services/leaflet-loader.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-itinerary-detail',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, TranslateModule, FeedbackComponent],
  template: `
    @if (itinerary(); as item) {
      <section class="page-heading compact">
        <div><p>{{ item.destination }}</p><h1>{{ item.title }}</h1><span>{{ item.startDate | date }} - {{ item.endDate | date }}</span></div>
      </section>
      <section class="detail-grid">
        <div class="panel stack">
          <h2>{{ 'ITINERARY_DETAIL.MAP_TITLE' | translate }}</h2>
          @if (mapError()) {
            <div class="empty-state">{{ 'ITINERARY_DETAIL.MAP_ERROR' | translate }}</div>
          } @else {
            <div #mapCanvas class="travel-map"></div>
            <small class="map-hint">{{ 'ITINERARY_DETAIL.MAP_HINT' | translate }}</small>
          }
          <form [formGroup]="stopForm" (ngSubmit)="addStop()" class="stack">
            <h3>{{ 'ITINERARY_DETAIL.NEW_STOP' | translate }}</h3>
            <label>{{ 'COMMON.NAME' | translate }}<input formControlName="name" /></label>
            <label>{{ 'COMMON.ADDRESS' | translate }}<input formControlName="address" /></label>
            <div class="grid two">
              <label>{{ 'COMMON.LATITUDE' | translate }}<input type="number" step="0.000001" formControlName="latitude" /></label>
              <label>{{ 'COMMON.LONGITUDE' | translate }}<input type="number" step="0.000001" formControlName="longitude" /></label>
            </div>
            <div class="grid two">
              <label>{{ 'COMMON.DAY' | translate }}<input type="number" min="1" formControlName="dayNumber" /></label>
              <label>{{ 'COMMON.DURATION' | translate }}<input type="number" min="15" formControlName="durationMinutes" /></label>
            </div>
            <label>{{ 'COMMON.NOTES' | translate }}<textarea formControlName="notes"></textarea></label>
            <button class="primary" [disabled]="stopForm.invalid || loading()">{{ 'ITINERARY_DETAIL.ADD_STOP' | translate }}</button>
          </form>
          <div class="stack">
            @for (day of stopsByDay(); track day.dayNumber) {
              <section class="day-plan">
                <h3>{{ 'COMMON.DAY' | translate }} {{ day.dayNumber }}</h3>
                <div class="chips">
                  @for (stop of day.stops; track stop.id) {
                    <button class="chip" type="button" (click)="focusStop(stop)">{{ (stop.orderIndex ?? 0) + 1 }}. {{ stop.name }}</button>
                    <button class="ghost" type="button" (click)="removeStop(stop.id)">{{ 'COMMON.REMOVE' | translate }}</button>
                  }
                </div>
              </section>
            }
          </div>
        </div>
        <div class="panel chat stack">
          <h2>{{ 'ITINERARY_DETAIL.AI_TITLE' | translate }}</h2>
          <div class="messages">
            @for (message of messages(); track message.id ?? message.createdAt ?? message.content) {
              <p [class.assistant]="message.role === 'assistant'"><strong>{{ message.role }}:</strong> {{ message.content }}</p>
            }
          </div>
          <form [formGroup]="chatForm" (ngSubmit)="sendMessage()" class="chat-form">
            <input formControlName="message" [placeholder]="'ITINERARY_DETAIL.AI_PLACEHOLDER' | translate" />
            <button class="primary" [disabled]="chatForm.invalid || loading()">{{ 'COMMON.SEND' | translate }}</button>
          </form>
          <app-feedback [loading]="loading()" [error]="error()" />
        </div>
      </section>
    }
  `
})
export class ItineraryDetailComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('mapCanvas') private mapCanvas?: ElementRef<HTMLElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly itineraries = inject(ItineraryService);
  private readonly ai = inject(AiService);
  private readonly leaflet = inject(LeafletLoaderService);
  readonly itinerary = signal<Itinerary | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly loading = signal(false);
  readonly mapsReady = signal(false);
  readonly mapError = signal(false);
  readonly error = signal('');
  readonly center = computed(() => {
    const first = this.itinerary()?.stops?.[0];
    return first ? { lat: first.latitude, lng: first.longitude } : { lat: -8.839, lng: 13.2894 };
  });
  readonly stopsByDay = computed(() => {
    const stops = [...(this.itinerary()?.stops ?? [])].sort((a, b) => (a.dayNumber ?? 1) - (b.dayNumber ?? 1) || (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
    const grouped = new Map<number, ItineraryStop[]>();
    for (const stop of stops) {
      const day = stop.dayNumber ?? 1;
      grouped.set(day, [...(grouped.get(day) ?? []), stop]);
    }
    return [...grouped.entries()].map(([dayNumber, dayStops]) => ({ dayNumber, stops: dayStops }));
  });
  readonly stopForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    address: [''],
    latitude: [-8.839, Validators.required],
    longitude: [13.2894, Validators.required],
    dayNumber: [1, [Validators.required, Validators.min(1)]],
    durationMinutes: [90, [Validators.required, Validators.min(15)]],
    notes: ['']
  });
  readonly chatForm = this.fb.nonNullable.group({ message: ['', Validators.required] });
  private map?: any;
  private markers: any[] = [];
  private routeLine?: any;

  ngOnInit(): void {
    this.leaflet.load()
      .then((ready) => {
        this.mapsReady.set(ready);
        this.renderMap();
      })
      .catch(() => this.mapError.set(true));
    this.load();
  }

  ngAfterViewInit(): void {
    this.renderMap();
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.itineraries.get(id).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (item) => {
        this.itinerary.set(item);
        this.stopForm.patchValue({
          latitude: item.latitude ?? this.center().lat,
          longitude: item.longitude ?? this.center().lng
        });
        this.renderMap();
        this.ai.history(id).subscribe((messages) => this.messages.set(messages));
      },
      error: () => this.error.set('ITINERARY_DETAIL.LOAD_ERROR')
    });
  }

  addStop(): void {
    const item = this.itinerary();
    if (!item) return;
    this.loading.set(true);
    const stops = item.stops ?? [];
    const value = this.stopForm.getRawValue();
    const orderIndex = stops.filter((stop) => (stop.dayNumber ?? 1) === value.dayNumber).length;
    this.itineraries.addStop(item.id, { ...value, orderIndex, category: 99 }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (stop) => {
        this.itinerary.update((current) => ({ ...current!, stops: [...(current?.stops ?? []), stop] }));
        this.stopForm.reset({ name: '', address: '', latitude: stop.latitude, longitude: stop.longitude, dayNumber: value.dayNumber, durationMinutes: 90, notes: '' });
        this.renderMap();
      },
      error: () => this.error.set('ITINERARY_DETAIL.ADD_STOP_ERROR')
    });
  }

  removeStop(stopId: string): void {
    const item = this.itinerary();
    if (!item) return;
    this.itineraries.removeStop(item.id, stopId).subscribe(() => {
      this.itinerary.update((current) => ({ ...current!, stops: current?.stops?.filter((stop) => stop.id !== stopId) ?? [] }));
      this.renderMap();
    });
  }

  focusStop(stop: ItineraryStop): void {
    this.map?.setView([stop.latitude, stop.longitude], 14);
    this.stopForm.patchValue({ latitude: stop.latitude, longitude: stop.longitude });
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
      error: () => this.error.set('ITINERARY_DETAIL.AI_ERROR')
    });
  }

  private renderMap(): void {
    if (!this.mapsReady() || !this.mapCanvas?.nativeElement || !window.L) {
      return;
    }

    const L = window.L;
    const center = this.center();

    if (!this.map) {
      this.map = L.map(this.mapCanvas.nativeElement).setView([center.lat, center.lng], 11);
      L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(this.map);
      this.map.on('click', (event: any) => {
        this.stopForm.patchValue({
          latitude: Number(event.latlng.lat.toFixed(6)),
          longitude: Number(event.latlng.lng.toFixed(6))
        });
      });
    }

    this.map.setView([center.lat, center.lng], this.itinerary()?.stops?.length ? 12 : 7);
    for (const marker of this.markers) marker.remove();
    this.routeLine?.remove();

    const stops = this.itinerary()?.stops ?? [];
    this.markers = stops.map((stop) => L.marker([stop.latitude, stop.longitude])
      .addTo(this.map)
      .bindPopup(`<strong>${this.escapeHtml(stop.name)}</strong><br>${this.escapeHtml(String(stop.address ?? ''))}<br>Day ${stop.dayNumber ?? 1}`));

    if (stops.length > 1) {
      this.routeLine = L.polyline(stops.map((stop) => [stop.latitude, stop.longitude]), { color: '#107c8c', weight: 4 }).addTo(this.map);
      this.map.fitBounds(this.routeLine.getBounds(), { padding: [24, 24] });
    }

    setTimeout(() => this.map?.invalidateSize(), 0);
  }

  private escapeHtml(value: string): string {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }
}
