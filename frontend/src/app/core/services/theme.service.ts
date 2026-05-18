import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  readonly theme = signal<Theme>((localStorage.getItem('travel_app_theme') as Theme) || 'light');

  constructor() {
    effect(() => {
      const theme = this.theme();
      localStorage.setItem('travel_app_theme', theme);
      this.document.documentElement.dataset['theme'] = theme;
    });
  }

  toggle(): void {
    this.theme.update((theme) => (theme === 'light' ? 'dark' : 'light'));
  }
}
