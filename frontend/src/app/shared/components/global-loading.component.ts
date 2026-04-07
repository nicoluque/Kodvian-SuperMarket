import { AsyncPipe, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { map } from 'rxjs';
import { LoadingService } from '../../core/services/loading.service';

@Component({
  standalone: true,
  selector: 'app-global-loading',
  imports: [NgIf, AsyncPipe],
  template: `
    <div class="loading-bar" *ngIf="isLoading$ | async"></div>
  `,
  styles: [
    `.loading-bar{position:fixed;left:0;top:0;height:3px;width:100%;z-index:250;background:linear-gradient(90deg,#1c9a6b,#48c7d9,#1c9a6b);background-size:180% 100%;animation:move 1s linear infinite}`,
    `@keyframes move{0%{background-position:0 0}100%{background-position:180% 0}}`
  ]
})
export class GlobalLoadingComponent {
  readonly isLoading$;

  constructor(loading: LoadingService) {
    this.isLoading$ = loading.pending$.pipe(map(v => v > 0));
  }
}
