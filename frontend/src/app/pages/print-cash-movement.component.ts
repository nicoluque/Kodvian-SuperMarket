import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PrintService } from '../core/services/print.service';

@Component({
  standalone: true,
  selector: 'app-print-cash-movement',
  imports: [CommonModule],
  template: `
    <main class="ticket" *ngIf="data">
      <h1>{{ data.branding?.displayName }}</h1>
      <p>Movimiento #{{ data.movementId }}</p>
      <p>{{ data.createdAt | date:'short' }}</p>
      <hr />
      <div class="row"><span>Tipo</span><span>{{ movementTypeLabel(data.type) }}</span></div>
      <div class="row"><span>Metodo</span><span>{{ paymentMethodLabel(data.method) }}</span></div>
      <div class="row"><span>Monto</span><strong>{{ data.signedAmount | number:'1.2-2' }}</strong></div>
      <p>{{ data.reason }}</p>
      <button class="no-print" (click)="print()">Imprimir</button>
    </main>
  `,
  styles: [`.ticket{width:80mm;max-width:80mm;margin:0 auto;padding:8px;font-family:'Courier New',monospace;font-size:12px}.row{display:flex;justify-content:space-between}p,h1{text-align:center;margin:2px 0}hr{border:none;border-top:1px dashed #333;margin:6px 0}.no-print{width:100%;margin-top:8px;padding:8px}@media print{.no-print{display:none}}`]
})
export class PrintCashMovementComponent {
  data: any;
  constructor(route: ActivatedRoute, private readonly api: PrintService) {
    const id = Number(route.snapshot.paramMap.get('id'));
    const reprint = route.snapshot.queryParamMap.get('reprint') === '1';
    const autoPrint = route.snapshot.queryParamMap.get('autoprint') === '1';
    if (id) void this.load(id, reprint, autoPrint);
  }
  async load(id: number, reprint: boolean, autoPrint: boolean): Promise<void> {
    this.data = await this.api.cashMovement(id, reprint);
    if (autoPrint) setTimeout(() => window.print(), 50);
  }
  paymentMethodLabel(method: string): string {
    if (method === 'Cash') return 'Efectivo';
    if (method === 'Card') return 'Tarjeta';
    if (method === 'Transfer') return 'Transferencia';
    if (method === 'QrMp') return 'QR Mercado Pago';
    if (method === 'Credit') return 'Cuenta corriente';
    return method;
  }
  movementTypeLabel(type: string): string {
    if (type === 'In') return 'Ingreso';
    if (type === 'Out') return 'Egreso';
    return type;
  }
  print(): void { window.print(); }
}
