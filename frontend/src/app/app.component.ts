import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  private readonly theme = inject(ThemeService);

  constructor() {
    const lang = localStorage.getItem('travel_app_lang') || 'pt';
    this.translate.addLangs(['pt', 'en']);
    this.translate.use(lang);
    this.theme.theme();
  }
}
