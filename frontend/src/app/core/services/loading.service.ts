import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly pendingSubject = new BehaviorSubject<number>(0);
  readonly pending$ = this.pendingSubject.asObservable();

  get isLoading(): boolean {
    return this.pendingSubject.value > 0;
  }

  begin(): void {
    this.pendingSubject.next(this.pendingSubject.value + 1);
  }

  end(): void {
    this.pendingSubject.next(Math.max(0, this.pendingSubject.value - 1));
  }
}
