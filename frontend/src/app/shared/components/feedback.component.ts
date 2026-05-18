import { Component, input } from '@angular/core';

@Component({
  selector: 'app-feedback',
  standalone: true,
  template: `
    @if (loading()) {
      <p class="feedback loading">{{ loadingText() }}</p>
    }
    @if (error()) {
      <p class="feedback error">{{ error() }}</p>
    }
    @if (success()) {
      <p class="feedback success">{{ success() }}</p>
    }
  `
})
export class FeedbackComponent {
  loading = input(false);
  error = input('');
  success = input('');
  loadingText = input('A carregar...');
}
