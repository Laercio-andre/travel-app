import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    google?: unknown;
  }
}

@Injectable({ providedIn: 'root' })
export class GoogleMapsLoaderService {
  private readonly document = inject(DOCUMENT);
  private loading?: Promise<boolean>;

  load(): Promise<boolean> {
    if (window.google) {
      return Promise.resolve(true);
    }

    if (!environment.googleMapsApiKey) {
      return Promise.resolve(false);
    }

    this.loading ??= new Promise((resolve, reject) => {
      const script = this.document.createElement('script');
      script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}`;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve(true);
      script.onerror = () => reject(new Error('Google Maps failed to load'));
      this.document.head.appendChild(script);
    });

    return this.loading;
  }
}
