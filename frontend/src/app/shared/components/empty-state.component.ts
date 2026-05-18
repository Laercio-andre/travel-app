import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <section class="empty-state">
      <h3>{{ title() }}</h3>
      <p>{{ description() }}</p>
    </section>
  `
})
export class EmptyStateComponent {
  title = input.required<string>();
  description = input<string>('');
}
