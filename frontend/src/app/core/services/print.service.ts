import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PrintService {
  constructor(private readonly http: HttpClient) {}

  sale(id: number, reprint = false): Promise<any> {
    return firstValueFrom(this.http.get(`/api/v1/print/sales/${id}${this.toQuery(reprint)}`));
  }

  customerPayment(id: number, reprint = false): Promise<any> {
    return firstValueFrom(this.http.get(`/api/v1/print/customer-payments/${id}${this.toQuery(reprint)}`));
  }

  return(id: number, reprint = false): Promise<any> {
    return firstValueFrom(this.http.get(`/api/v1/print/returns/${id}${this.toQuery(reprint)}`));
  }

  cashMovement(id: number, reprint = false): Promise<any> {
    return firstValueFrom(this.http.get(`/api/v1/print/cash-movements/${id}${this.toQuery(reprint)}`));
  }

  cashClose(id: number, reprint = false): Promise<any> {
    return firstValueFrom(this.http.get(`/api/v1/print/cash-sessions/${id}/close-summary${this.toQuery(reprint)}`));
  }

  private toQuery(reprint: boolean): string {
    return reprint ? '?reprint=1' : '';
  }
}
