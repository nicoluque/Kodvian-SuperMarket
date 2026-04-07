import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PrintService } from '../core/services/print.service';

@Component({
  standalone: true,
  selector: 'app-print-sale',
  imports: [CommonModule],
  template: `
    <main class="ticket" *ngIf="data">
      <img *ngIf="data.branding?.logoUrl" [src]="data.branding.logoUrl" class="logo" alt="logo" />
      <h1>{{ data.branding?.displayName }}</h1>
      <p>{{ data.branding?.ticketHeaderText }}</p>
      <p>Venta #{{ data.saleId }} · {{ data.createdAt | date:'short' }}</p>
      <p *ngIf="data.invoiceNumber">Comprobante: {{ data.invoiceNumber }}</p>
      <hr />
      <div class="row" *ngFor="let i of data.items">
        <span>{{ i.productName }} x{{ i.quantity }}</span>
        <strong>{{ i.subtotal | number:'1.2-2' }}</strong>
      </div>
      <hr />
      <div class="row"><span>Total</span><strong>{{ data.total | number:'1.2-2' }}</strong></div>
      <div class="row" *ngFor="let p of data.payments"><span>{{ paymentMethodLabel(p.method) }} ({{ paymentStatusLabel(p.status) }})</span><span>{{ p.amount | number:'1.2-2' }}</span></div>
      <hr />
      <p>{{ data.branding?.returnPolicyText }}</p>
      <p>{{ data.branding?.ticketFooterText }}</p>
      <button class="no-print" (click)="print()">Imprimir</button>
    </main>
  `,
  styles: [
    `.ticket{width:80mm;max-width:80mm;margin:0 auto;padding:8px;font-family:'Courier New',monospace;font-size:12px}`,
    `.logo{max-width:52mm;max-height:24mm;display:block;margin:0 auto 6px}`,
    `h1{text-align:center;margin:0 0 4px;font-size:14px}`,
    `p{margin:2px 0;text-align:center}`,
    `.row{display:flex;justify-content:space-between;gap:8px}`,
    `hr{border:none;border-top:1px dashed #333;margin:6px 0}`,
    `.no-print{width:100%;margin-top:8px;padding:8px}`,
    `@media print{.no-print{display:none} body{margin:0}}`
  ]
})
export class PrintSaleComponent {
  data: any;

  constructor(route: ActivatedRoute, private readonly api: PrintService) {
    const id = Number(route.snapshot.paramMap.get('id'));
    const reprint = route.snapshot.queryParamMap.get('reprint') === '1';
    const autoPrint = route.snapshot.queryParamMap.get('autoprint') === '1';
    if (id) void this.load(id, reprint, autoPrint);
  }

  async load(id: number, reprint: boolean, autoPrint: boolean): Promise<void> {
    this.data = await this.api.sale(id, reprint);
    if (autoPrint) {
      setTimeout(() => window.print(), 50);
    }
  }

  paymentMethodLabel(method: string): string {
    if (method === 'Cash') return 'Efectivo';
    if (method === 'Card') return 'Tarjeta';
    if (method === 'Transfer') return 'Transferencia';
    if (method === 'QrMp') return 'QR Mercado Pago';
    if (method === 'Credit') return 'Cuenta corriente';
    return method;
  }

  paymentStatusLabel(status: string): string {
    if (status === 'Confirmed') return 'Confirmado';
    if (status === 'Pending') return 'Pendiente';
    if (status === 'Rejected') return 'Rechazado';
    return status;
  }

  print(): void {
    window.print();
  }
}
