import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [NavbarComponent, RouterOutlet],
  template: `
    <app-navbar />
    <main class="app-main">
      <router-outlet />
    </main>
    <footer class="footer">TravelOps</footer>
  `
})
export class AppShellComponent {}
