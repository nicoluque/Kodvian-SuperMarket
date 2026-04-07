import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogService } from '../core/services/dialog.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';
import { BoPurchasesService, PurchasePayload } from '../core/services/bo-purchases.service';

type PurchaseLineDraft = {
  productId: number | null;
  quantity: number;
  unitCost: number;
  expiryDate: string;
  damagedForClaimQty: number;
  discardQty: number;
  updateSalePrice: boolean;
  newSalePrice: number | null;
  newPricePerKg: number | null;
};

@Component({
  standalone: true,
  selector: 'app-bo-compras-manuales',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Órdenes de compra manuales</h1>
            <p>Crea y edita órdenes una por una en borrador, luego confirma para impactar stock.</p>
          </div>
          <span class="hero-pill">Flujo recomendado: borrador -> confirmar</span>
        </header>

        <section class="card list-card">
          <div class="row between">
            <h3>Listado de órdenes</h3>
            <div class="row">
              <select [(ngModel)]="statusFilter" (ngModelChange)="loadPurchases()">
                <option value="">Todos</option>
                <option value="Draft">Borrador</option>
                <option value="Confirmed">Confirmada</option>
                <option value="Cancelled">Cancelada</option>
              </select>
              <button class="btn btn-secondary" (click)="newOrder()">Nueva orden</button>
              <button class="btn btn-secondary" (click)="loadPurchases()" [disabled]="busy">Recargar</button>
            </div>
          </div>

          <div class="table-scroll" *ngIf="purchases.length > 0">
            <table>
              <thead>
                <tr><th>ID</th><th>Proveedor</th><th>Documento</th><th>Fecha</th><th>Estado</th><th>Motivo</th><th>Total</th><th></th></tr>
              </thead>
              <tbody>
                <tr *ngFor="let p of purchases" [class.active-row]="selectedPurchaseId === p.id">
                  <td>#{{ p.id }}</td>
                  <td>{{ p.supplierName || '-' }}</td>
                  <td>{{ p.docType }} {{ p.docNumber || '' }}</td>
                  <td>{{ p.purchaseDate | date:'shortDate' }}</td>
                  <td>{{ statusLabel(p.status) }}</td>
                  <td><span class="reason-cell" [title]="(p.cancelReason || p.CancelReason || '-')">{{ (p.cancelReason || p.CancelReason || '-') }}</span></td>
                  <td>{{ p.total | number:'1.2-2' }}</td>
                  <td><button class="btn btn-secondary" (click)="openOrder(p.id)" [disabled]="busy">Abrir</button></td>
                </tr>
              </tbody>
            </table>
          </div>
          <p *ngIf="purchases.length === 0" class="meta">No hay órdenes para el filtro seleccionado.</p>
        </section>

        <section class="card form-card">
          <div class="row between">
            <h3>{{ selectedPurchaseId ? 'Editar orden #' + selectedPurchaseId : 'Nueva orden' }}</h3>
            <span class="pill">Estado: {{ statusLabel(formStatus) }}</span>
          </div>

          <section class="cancel-reason" *ngIf="formStatus === 'Cancelled'">
            <strong>Motivo de cancelación:</strong>
            <span>{{ currentCancelReason || '-' }}</span>
          </section>

          <div class="grid">
            <label>Proveedor
              <select [(ngModel)]="form.supplierId" [disabled]="!isDraft()">
                <option [ngValue]="null">Sin proveedor</option>
                <option *ngFor="let s of suppliers" [ngValue]="s.id">{{ s.name }}</option>
              </select>
            </label>
            <label>Tipo documento
              <input [(ngModel)]="form.docType" [disabled]="!isDraft()" />
            </label>
            <label>Nro documento
              <input [(ngModel)]="form.docNumber" [disabled]="!isDraft()" />
            </label>
            <label>Fecha compra
              <input type="date" [(ngModel)]="form.purchaseDate" [disabled]="!isDraft()" />
            </label>
          </div>

          <div class="row between">
            <h4>Detalle de líneas</h4>
            <button class="btn btn-secondary" (click)="addLine()" [disabled]="!isDraft()">Agregar línea</button>
          </div>

          <section class="scanner-box">
            <div class="scanner-head">
              <h4>Escaneo rápido</h4>
              <button class="btn btn-secondary scanner-btn" type="button" (click)="focusScanInput()" [disabled]="!isDraft()">Enfocar escáner</button>
            </div>

            <div class="scanner-body">
              <input
                #scanInput
                [(ngModel)]="scanCode"
                (keydown.enter)="scanByCode(); $event.preventDefault()"
                [disabled]="!isDraft() || scanBusy"
                placeholder="Escanear código de barras o código rápido"
              />
              <select [(ngModel)]="scanQtyStep" [disabled]="!isDraft() || scanBusy" aria-label="Cantidad por escaneo">
                <option [ngValue]="1">Cantidad por escaneo: +1</option>
                <option [ngValue]="6">Cantidad por escaneo: +6</option>
                <option [ngValue]="12">Cantidad por escaneo: +12</option>
              </select>
              <button class="btn btn-primary scanner-btn" type="button" (click)="scanByCode()" [disabled]="!isDraft() || scanBusy">{{ scanBusy ? 'Buscando...' : 'Agregar por código' }}</button>
            </div>

            <p class="scan-ok" *ngIf="scanMessage">{{ scanMessage }}</p>
            <p class="scan-error" *ngIf="scanError">{{ scanError }}</p>
          </section>

          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Producto</th><th>Cantidad</th><th>Costo unit.</th><th>Dañados</th><th>Descarte</th><th>Vencimiento</th><th>Actualizar precio</th><th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let l of form.items; let i = index">
                  <td>
                    <select [(ngModel)]="l.productId" (ngModelChange)="onLineProductChange(l)" [disabled]="!isDraft()">
                      <option [ngValue]="null">Seleccionar</option>
                      <option *ngFor="let p of products" [ngValue]="p.id">{{ p.name }}</option>
                    </select>
                  </td>
                  <td><input type="number" min="0" step="0.001" [(ngModel)]="l.quantity" [disabled]="!isDraft()" /></td>
                  <td><input type="number" min="0" step="0.01" [(ngModel)]="l.unitCost" [disabled]="!isDraft()" /></td>
                  <td><input type="number" min="0" step="0.001" [(ngModel)]="l.damagedForClaimQty" [disabled]="!isDraft()" /></td>
                  <td><input type="number" min="0" step="0.001" [(ngModel)]="l.discardQty" [disabled]="!isDraft()" /></td>
                  <td>
                    <input type="date" [(ngModel)]="l.expiryDate" [disabled]="!isDraft() || !productTracksExpiry(l.productId)" />
                    <small class="meta" *ngIf="!productTracksExpiry(l.productId)">No controla vencimiento</small>
                  </td>
                  <td>
                    <label class="inline-check"><input type="checkbox" [(ngModel)]="l.updateSalePrice" [disabled]="!isDraft()" /> Sí</label>
                  </td>
                  <td><button class="btn btn-danger" (click)="removeLine(i)" [disabled]="!isDraft()">Quitar</button></td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="totals">Subtotal estimado: <strong>{{ subtotal() | number:'1.2-2' }}</strong></div>

          <div class="row actions">
            <button class="btn btn-primary" (click)="saveDraft()" [disabled]="busy || !isDraft()">Guardar borrador</button>
            <button class="btn btn-secondary" (click)="confirmOrder()" [disabled]="busy || !selectedPurchaseId || !isDraft()">Confirmar orden</button>
            <button class="btn btn-danger" (click)="cancelOrder()" [disabled]="busy || !selectedPurchaseId || !isDraft()">Cancelar orden</button>
          </div>
        </section>

        <section class="alert ok" *ngIf="message">{{ message }}</section>
        <section class="alert error" *ngIf="error">{{ error }}</section>
      </section>
    </main>
  `,
  styles: [
    `:host{display:block}`,
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.content{position:relative;z-index:1;display:flex;flex-direction:column;gap:14px;max-width:1120px;width:100%;margin:0 auto}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:240px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero h1{margin:0 0 8px;font-size:clamp(24px,2.4vw,36px);line-height:1.1}`,
    `.hero p{margin:0;color:rgba(255,255,255,.95)}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:16px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);display:flex;flex-direction:column;gap:12px}`,
    `.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}`,
    `.between{justify-content:space-between}`,
    `.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:10px}`,
    `label{display:flex;flex-direction:column;gap:4px;color:#1f6048;font-weight:700}`,
    `input,select{border:1px solid #d5e6dd;border-radius:10px;padding:9px 10px;background:#fff;outline:none;min-height:42px}`,
    `.table-scroll{overflow:auto;border:1px solid #e5f0ea;border-radius:12px}`,
    `table{width:100%;border-collapse:collapse;min-width:1000px;background:#fff}`,
    `th,td{border-bottom:1px solid #edf3ef;padding:8px;text-align:left;vertical-align:middle}`,
    `th{background:#f5fbf8;color:#1e5f47}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:8px 12px;font-weight:700;cursor:pointer;min-height:40px}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2f8e67;background:#e8f5ef;color:#1f6f50}`,
    `.btn-danger{border-color:#dcaaaa;background:#fff1f1;color:#9f1f1f}`,
    `.pill{display:inline-flex;background:#edf8f2;color:#16543d;border:1px solid #bfd9cc;border-radius:999px;padding:5px 10px;font-size:12px;font-weight:700}`,
    `.active-row{background:#f5fbf8}`,
    `.reason-cell{display:inline-block;max-width:260px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;vertical-align:middle}`,
    `.cancel-reason{display:flex;gap:8px;align-items:flex-start;padding:10px 12px;border:1px solid #efc7c9;background:#fff3f3;border-radius:12px;color:#7b1f1f}`,
    `.inline-check{display:inline-flex;gap:6px;align-items:center}`,
    `.scanner-box{border:1px solid #d8e8df;background:#f7fcf9;border-radius:12px;padding:12px;display:flex;flex-direction:column;gap:10px}`,
    `.scanner-head{display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap}`,
    `.scanner-head h4{margin:0;color:#184f3c}`,
    `.scanner-body{display:grid;grid-template-columns:minmax(0,1fr) 240px auto;gap:10px;align-items:stretch}`,
    `.scanner-body input{min-width:0}`,
    `.scanner-btn{width:190px;max-width:100%;justify-content:center}`,
    `.scan-ok{margin:0;color:#0a7a32;font-weight:700}`,
    `.scan-error{margin:0;color:#b3261e;font-weight:700}`,
    `.totals{color:#184f3c;font-weight:600}`,
    `.actions{justify-content:flex-end}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.alert{border-radius:12px;padding:12px 14px;font-weight:700}`,
    `.alert.ok{background:#edf8f2;border:1px solid #bfd9cc;color:#16543d}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f}`,
    `@media (max-width: 900px){.wrap{padding:16px}.scanner-head{align-items:flex-start}.scanner-head .btn{width:100%}.scanner-body{grid-template-columns:1fr}.scanner-body .btn{width:100%}.actions{justify-content:stretch}.actions .btn{flex:1 1 100%}}`
  ]
})
export class BoComprasManualesComponent {
  @ViewChild('scanInput') private scanInputRef?: ElementRef<HTMLInputElement>;

  suppliers: any[] = [];
  products: any[] = [];
  purchases: any[] = [];
  statusFilter = '';
  selectedPurchaseId: number | null = null;
  formStatus = 'Draft';
  currentCancelReason = '';

  form: { supplierId: number | null; docType: string; docNumber: string; purchaseDate: string; items: PurchaseLineDraft[] } = {
    supplierId: null,
    docType: 'Invoice',
    docNumber: '',
    purchaseDate: new Date().toISOString().slice(0, 10),
    items: []
  };

  busy = false;
  scanBusy = false;
  scanCode = '';
  scanQtyStep = 1;
  scanMessage = '';
  scanError = '';
  message = '';
  error = '';

  constructor(private readonly api: BoPurchasesService, private readonly dialog: DialogService) {
    void this.loadBoot();
  }

  async loadBoot(): Promise<void> {
    await this.run(async () => {
      this.suppliers = await this.api.suppliers();
      this.products = await this.api.products();
      await this.loadPurchases();
      this.newOrder();
    });
  }

  async loadPurchases(): Promise<void> {
    this.purchases = await this.api.list(this.statusFilter || undefined);
  }

  statusLabel(status: string): string {
    if (status === 'Draft') return 'Borrador';
    if (status === 'Confirmed') return 'Confirmada';
    if (status === 'Cancelled') return 'Cancelada';
    return status || '-';
  }

  isDraft(): boolean {
    return this.formStatus === 'Draft';
  }

  newOrder(): void {
    this.selectedPurchaseId = null;
    this.formStatus = 'Draft';
    this.currentCancelReason = '';
    this.form = {
      supplierId: null,
      docType: 'Invoice',
      docNumber: '',
      purchaseDate: new Date().toISOString().slice(0, 10),
      items: [this.blankLine()]
    };
    this.error = '';
    this.message = '';
    this.scanMessage = '';
    this.scanError = '';
    this.scanCode = '';
    setTimeout(() => this.focusScanInput(), 0);
  }

  addLine(): void {
    this.form.items.push(this.blankLine());
  }

  focusScanInput(): void {
    this.scanInputRef?.nativeElement.focus();
    this.scanInputRef?.nativeElement.select();
  }

  async scanByCode(): Promise<void> {
    if (!this.isDraft()) return;
    const code = (this.scanCode || '').trim();
    if (!code) {
      this.scanError = 'Ingresá o escaneá un código.';
      this.scanMessage = '';
      return;
    }

    this.scanBusy = true;
    this.scanError = '';
    this.scanMessage = '';

    try {
      const product = await this.api.findProductByCode(code);
      if (!product) {
        this.scanError = 'Código no encontrado. Verificá barcode o código rápido.';
        return;
      }

      this.addOrIncrementScannedProduct(product);
      this.scanMessage = `Agregado: ${product?.name ?? product?.Name ?? ('Producto #' + (product?.id ?? product?.Id))} (+${this.scanQtyStep})`;
      this.scanCode = '';
      setTimeout(() => this.focusScanInput(), 0);
    } catch {
      this.scanError = 'No se pudo procesar el escaneo.';
    } finally {
      this.scanBusy = false;
    }
  }

  private addOrIncrementScannedProduct(product: any): void {
    const productId = Number(product?.id ?? product?.Id ?? 0);
    if (!productId) return;

    if (!this.products.some(p => Number(p?.id ?? p?.Id ?? 0) === productId)) {
      this.products = [...this.products, product];
    }

    const existing = this.form.items.find(l => Number(l.productId) === productId);
    if (existing) {
      existing.quantity = Number(existing.quantity || 0) + Number(this.scanQtyStep || 1);
      return;
    }

    const blank = this.form.items.find(l => !l.productId && Number(l.quantity || 0) === 1 && Number(l.unitCost || 0) === 0);
    if (blank) {
      blank.productId = productId;
      blank.quantity = Number(this.scanQtyStep || 1);
      blank.unitCost = Number(product?.lastCost ?? product?.LastCost ?? 0) || 0;
      blank.expiryDate = '';
      blank.damagedForClaimQty = 0;
      blank.discardQty = 0;
      blank.updateSalePrice = false;
      blank.newSalePrice = null;
      blank.newPricePerKg = null;
      return;
    }

    const tracksExpiry = !!(product?.tracksExpiry ?? product?.TracksExpiry);
    const lastCost = Number(product?.lastCost ?? product?.LastCost ?? 0);

    this.form.items.push({
      productId,
      quantity: Number(this.scanQtyStep || 1),
      unitCost: lastCost > 0 ? lastCost : 0,
      expiryDate: '',
      damagedForClaimQty: 0,
      discardQty: 0,
      updateSalePrice: false,
      newSalePrice: null,
      newPricePerKg: null
    });

    if (!tracksExpiry) {
      const line = this.form.items[this.form.items.length - 1];
      line.expiryDate = '';
    }
  }

  removeLine(index: number): void {
    this.form.items.splice(index, 1);
    if (this.form.items.length === 0) this.form.items.push(this.blankLine());
  }

  onLineProductChange(line: PurchaseLineDraft): void {
    if (!this.productTracksExpiry(line.productId)) {
      line.expiryDate = '';
    }
  }

  subtotal(): number {
    return this.form.items.reduce((acc, i) => acc + (Number(i.quantity) || 0) * (Number(i.unitCost) || 0), 0);
  }

  productTracksExpiry(productId: number | null): boolean {
    if (!productId) return false;
    const product = this.products.find(p => p.id === productId);
    return !!product?.tracksExpiry;
  }

  async openOrder(id: number): Promise<void> {
    await this.run(async () => {
      const order = await this.api.getById(id);
      this.selectedPurchaseId = Number(order?.id ?? order?.Id ?? id);
      this.formStatus = String(order?.status ?? order?.Status ?? 'Draft');
      this.currentCancelReason = String(order?.cancelReason ?? order?.CancelReason ?? '');
      const rawItems = Array.isArray(order?.items)
        ? order.items
        : Array.isArray(order?.Items)
          ? order.Items
          : [];

      this.form = {
        supplierId: Number(order?.supplierId ?? order?.SupplierId ?? 0) || null,
        docType: String(order?.docType ?? order?.DocType ?? 'Invoice'),
        docNumber: String(order?.docNumber ?? order?.DocNumber ?? ''),
        purchaseDate: this.toInputDate(order?.purchaseDate ?? order?.PurchaseDate),
        items: rawItems.map((it: any) => ({
          productId: Number(it?.productId ?? it?.ProductId ?? 0) || null,
          quantity: Number(it?.quantity ?? it?.Quantity ?? 0),
          unitCost: Number(it?.unitCost ?? it?.UnitCost ?? 0),
          expiryDate: this.toInputDate(it?.expiryDate ?? it?.ExpiryDate),
          damagedForClaimQty: Number(it?.damagedForClaimQty ?? it?.DamagedForClaimQty ?? 0),
          discardQty: Number(it?.discardQty ?? it?.DiscardQty ?? 0),
          updateSalePrice: !!(it?.updateSalePrice ?? it?.UpdateSalePrice),
          newSalePrice: (it?.newSalePrice ?? it?.NewSalePrice) ?? null,
          newPricePerKg: (it?.newPricePerKg ?? it?.NewPricePerKg) ?? null
        }))
      };
      if (this.form.items.length === 0) {
        this.form.items.push(this.blankLine());
        this.error = `La orden #${this.selectedPurchaseId} no trae líneas de detalle. Verifica que el borrador se haya guardado con items.`;
      }
      this.scanMessage = '';
      this.scanError = '';
      this.scanCode = '';
      setTimeout(() => this.focusScanInput(), 0);
    });
  }

  async saveDraft(): Promise<void> {
    await this.run(async () => {
      this.validateForm();
      const payload = this.toPayload();
      const saved = this.selectedPurchaseId
        ? await this.api.update(this.selectedPurchaseId, payload)
        : await this.api.create(payload);

      this.selectedPurchaseId = saved.id;
      this.formStatus = saved.status;
      this.message = this.selectedPurchaseId ? `Borrador #${saved.id} guardado` : 'Borrador guardado';
      await this.loadPurchases();
      await this.openOrder(saved.id);
    });
  }

  async confirmOrder(): Promise<void> {
    if (!this.selectedPurchaseId) return;
    await this.run(async () => {
      await this.api.confirm(this.selectedPurchaseId!);
      this.message = `Orden #${this.selectedPurchaseId} confirmada`;
      await this.loadPurchases();
      await this.openOrder(this.selectedPurchaseId!);
    });
  }

  async cancelOrder(): Promise<void> {
    if (!this.selectedPurchaseId) return;
    const reason = await this.dialog.prompt({
      title: 'Cancelar orden de compra',
      message: `Orden #${this.selectedPurchaseId}. Ingresa el motivo de cancelacion.`,
      inputLabel: 'Motivo',
      inputPlaceholder: 'Ej: proveedor anulo la entrega',
      yesLabel: 'Cancelar orden',
      noLabel: 'Volver',
      inputRequired: true
    });
    if (!reason) return;
    await this.run(async () => {
      await this.api.cancel(this.selectedPurchaseId!, reason);
      this.message = `Orden #${this.selectedPurchaseId} cancelada. Motivo: ${reason}`;
      await this.loadPurchases();
      await this.openOrder(this.selectedPurchaseId!);
    });
  }

  private validateForm(): void {
    if (!this.form.docType.trim()) throw new Error('El tipo de documento es obligatorio.');
    if (this.form.items.length === 0) throw new Error('Debes cargar al menos una línea.');

    for (const [idx, l] of this.form.items.entries()) {
      if (!l.productId) throw new Error(`Línea ${idx + 1}: selecciona producto.`);
      if (l.quantity <= 0) throw new Error(`Línea ${idx + 1}: cantidad debe ser mayor a 0.`);
      if (l.unitCost <= 0) throw new Error(`Línea ${idx + 1}: costo unitario debe ser mayor a 0.`);
      if (l.damagedForClaimQty < 0 || l.discardQty < 0) throw new Error(`Línea ${idx + 1}: dañados y descarte no pueden ser negativos.`);
      if (l.damagedForClaimQty + l.discardQty > l.quantity) {
        throw new Error(`Línea ${idx + 1}: dañados + descarte no puede superar la cantidad.`);
      }
      if (!this.productTracksExpiry(l.productId) && !!l.expiryDate) {
        throw new Error(`Línea ${idx + 1}: el producto seleccionado no maneja vencimiento.`);
      }
    }
  }

  private toPayload(): PurchasePayload {
    return {
      supplierId: this.form.supplierId,
      docType: this.form.docType.trim(),
      docNumber: this.form.docNumber?.trim() || null,
      purchaseDate: new Date(this.form.purchaseDate || new Date().toISOString()).toISOString(),
      items: this.form.items.map(i => ({
        productId: Number(i.productId),
        quantity: Number(i.quantity),
        unitCost: Number(i.unitCost),
        expiryDate: this.productTracksExpiry(i.productId) && i.expiryDate ? new Date(i.expiryDate).toISOString() : null,
        damagedForClaimQty: Number(i.damagedForClaimQty || 0),
        discardQty: Number(i.discardQty || 0),
        updateSalePrice: !!i.updateSalePrice,
        newSalePrice: i.updateSalePrice ? (i.newSalePrice ?? null) : null,
        newPricePerKg: i.updateSalePrice ? (i.newPricePerKg ?? null) : null
      }))
    };
  }

  private blankLine(): PurchaseLineDraft {
    return {
      productId: null,
      quantity: 1,
      unitCost: 0,
      expiryDate: '',
      damagedForClaimQty: 0,
      discardQty: 0,
      updateSalePrice: false,
      newSalePrice: null,
      newPricePerKg: null
    };
  }

  private toInputDate(value: string | null | undefined): string {
    if (!value) return '';
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return '';
    return d.toISOString().slice(0, 10);
  }

  private async run(fn: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.error = '';
    this.message = '';
    try {
      await fn();
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'Operación fallida';
    } finally {
      this.busy = false;
    }
  }
}
