import { Component, input } from '@angular/core';

type IconName = 'route' | 'hotel' | 'plane' | 'report' | 'user' | 'shield' | 'lock' | 'mail' | 'check' | 'ban' | 'search' | 'calendar' | 'map-pin' | 'star';

const icons: Record<IconName, string> = {
  route: 'M4 18c2 0 2-3 4-3s2 3 4 3 2-3 4-3 4 3 4 3M8 15V7m0 0 3 3M8 7 5 10m11 5V6m0 0 3 3m-3-3-3 3',
  hotel: 'M4 20V5a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v15M2 20h20M8 8h1m3 0h1M8 12h1m3 0h1M8 20v-4h4v4m6 0v-8h2a2 2 0 0 1 2 2v6',
  plane: 'M2 16l20-8-20-8v7l13 1-13 1v7z',
  report: 'M6 2h9l5 5v15H6zM14 2v6h6M9 13h6M9 17h8',
  user: 'M20 21a8 8 0 0 0-16 0M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
  shield: 'M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z',
  lock: 'M7 11V8a5 5 0 0 1 10 0v3M6 11h12v10H6zM12 15v2',
  mail: 'M4 6h16v12H4zM4 7l8 6 8-6',
  check: 'M20 6L9 17l-5-5',
  ban: 'M4.93 4.93a10 10 0 1 0 14.14 14.14A10 10 0 0 0 4.93 4.93zM4.93 4.93l14.14 14.14',
  search: 'M10 18a8 8 0 1 1 5.66-2.34L21 21',
  calendar: 'M7 2v4m10-4v4M3 9h18M5 5h14a2 2 0 0 1 2 2v13H3V7a2 2 0 0 1 2-2z',
  'map-pin': 'M12 21s7-5.33 7-12a7 7 0 1 0-14 0c0 6.67 7 12 7 12zM12 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4z',
  star: 'M12 2l3 6 7 .9-5 4.8 1.2 6.8L12 17l-6.2 3.5L7 13.7 2 8.9 9 8z'
};

@Component({
  selector: 'app-icon',
  standalone: true,
  template: `
    <svg class="app-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path [attr.d]="icons[name()]" />
    </svg>
  `
})
export class IconComponent {
  name = input.required<IconName>();
  protected readonly icons = icons;
}
