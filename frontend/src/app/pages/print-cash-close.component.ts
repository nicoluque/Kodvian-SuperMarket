import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PrintService } from '../core/services/print.service';

@Component({
  standalone: true,
  selector: 'app-print-cash-close',
  imports: [CommonModule],
  template: `
    <main class="ticket" *ngIf="data">
      <h1>{{ data.branding?.displayName }}</h1>
      <p>Cierre caja #{{ data.cashSessionId }} ({{ shiftLabel(data.shift) }})</p>
      <p>{{ data.closedAt | date:'short' }}</p>
      <hr />
      <div class="row"><span>Ventas efectivo</span><strong>{{ data.totalCash | number:'1.2-2' }}</strong></div>
      <div class="row"><span>Ventas tarjeta</span><strong>{{ data.totalCard | number:'1.2-2' }}</strong></div>
      <div class="row"><span>Ventas transferencia</span><strong>{{ data.totalTransfer | number:'1.2-2' }}</strong></div>
      <div class="row"><span>Ventas cuenta corriente</span><strong>{{ data.totalCredit | number:'1.2-2' }}</strong></div>
      <div class="row"><span>Diferencia total</span><strong>{{ data.diffTotal | number:'1.2-2' }}</strong></div>
      <hr />
      <p>{{ data.branding?.ticketFooterText }}</p>
      <button class="no-print" (click)="print()">Imprimir</button>
    </main>
  `,
  styles: [`.ticket{width:80mm;max-width:80mm;margin:0 auto;padding:8px;font-family:'Courier New',monospace;font-size:12px}.row{display:flex;justify-content:space-between}p,h1{text-align:center;margin:2px 0}hr{border:none;border-top:1px dashed #333;margin:6px 0}.no-print{width:100%;margin-top:8px;padding:8px}@media print{.no-print{display:none}}`]
})
export class PrintCashCloseComponent {
  data: any;
  constructor(route: ActivatedRoute, private readonly api: PrintService) {
    const id = Number(route.snapshot.paramMap.get('id'));
    const reprint = route.snapshot.queryParamMap.get('reprint') === '1';
    const autoPrint = route.snapshot.queryParamMap.get('autoprint') === '1';
    if (id) void this.load(id, reprint, autoPrint);
  }
  async load(id: number, reprint: boolean, autoPrint: boolean): Promise<void> {
    this.data = await this.api.cashClose(id, reprint);
    if (autoPrint) setTimeout(() => window.print(), 50);
  }
  shiftLabel(shift: string): string {
    if (shift === 'Morning') return 'Mañana';
    if (shift === 'Afternoon') return 'Tarde';
    if (shift === 'Night') return 'Noche';
    return shift;
  }
  print(): void { window.print(); }
}
