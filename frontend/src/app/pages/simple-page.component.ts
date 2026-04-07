import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-simple-page',
  template: `
    <main style="padding:24px;font-family:Arial,sans-serif">
      <h1>{{ title() }}</h1>
      <p>Vista base lista para integrar funcionalidades.</p>
    </main>
  `
})
export class SimplePageComponent {
  private readonly route = inject(ActivatedRoute);
  readonly title = computed(() => (this.route.snapshot.data['title'] as string) ?? 'Page');
}
