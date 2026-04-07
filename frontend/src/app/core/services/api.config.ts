import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ApiConfigService {
  readonly posBase = '/pos/api/v1';
  readonly boBase = '/bo/api/v1';
  readonly apiBase = '/api/v1';
}
