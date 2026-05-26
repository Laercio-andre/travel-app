import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

declare global {
  interface Window {
    L?: any;
  }
}

@Injectable({ providedIn: 'root' })
export class LeafletLoaderService {
  private readonly document = inject(DOCUMENT);
  private loading?: Promise<boolean>;

  load(): Promise<boolean> {
    if (window.L) {
      return Promise.resolve(true);
    }

    this.loading ??= new Promise((resolve, reject) => {
      this.ensureStylesheet();

      const script = this.document.createElement('script');
      script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
      script.async = true;
      script.defer = true;
      script.onload = () => resolve(!!window.L);
      script.onerror = () => reject(new Error('Leaflet failed to load'));
      this.document.head.appendChild(script);
    });

    return this.loading;
  }

  private ensureStylesheet(): void {
    if (this.document.querySelector('link[data-leaflet-css="true"]')) {
      return;
    }

    const link = this.document.createElement('link');
    link.rel = 'stylesheet';
    link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
    link.dataset['leafletCss'] = 'true';
    this.document.head.appendChild(link);
  }
}
