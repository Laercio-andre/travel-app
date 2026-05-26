import { Component, input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-feedback',
  standalone: true,
  imports: [TranslateModule],
  template: `
    @if (loading()) {
      <p class="feedback loading">{{ loadingText() | translate }}</p>
    }
    @if (error()) {
      <p class="feedback error">{{ error() | translate }}</p>
    }
    @if (success()) {
      <p class="feedback success">{{ success() | translate }}</p>
    }
  `
})
export class FeedbackComponent {
  loading = input(false);
  error = input('');
  success = input('');
  loadingText = input('COMMON.LOADING');
}
