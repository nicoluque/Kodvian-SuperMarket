import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { OperatorSessionResponse, OperatorSessionService } from './operator-session.service';

export interface ProductLookupResponse {
  id: number;
  name: string;
  barcode?: string;
  quickCode?: string;
  saleType?: string;
  allowsManualPrice?: boolean;
  unitName?: string;
  defaultPrice?: number;
  defaultPricePerKg?: number;
}

export interface ProductResponse {
  id: number;
  name: string;
  barcode?: string;
  quickCode?: string;
  saleType?: string;
  allowsManualPrice?: boolean;
  stockControl?: boolean;
  catalogStatus?: string;
  defaultPrice?: number;
  defaultPricePerKg?: number;
}

export interface CartItem {
  id: number;
  productId?: number;
  productCode: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  unit: string;
  discount: number;
  subtotal: number;
}

export interface CartInboxItem {
  id: number;
  deviceId: number;
  deviceName: string;
  createdAt: string;
  sentToCashierAt?: string;
  items: CartItem[];
  total: number;
}

export interface CashSessionResponse {
  id: number;
  shift: string;
  status: string;
  openingCash: number;
  totalCash: number;
  totalCard: number;
  totalTransfer: number;
  totalCredit: number;
  openedAt: string;
  openedByUsuarioId?: number;
  openedByUsername?: string;
  currentUsuarioId?: number;
  currentUsername?: string;
  closedByUsuarioId?: number;
  closedByUsername?: string;
}

export interface CashSessionHandoverResponse {
  id: number;
  cashSessionId: number;
  fromUsuarioId?: number;
  fromUsername?: string;
  toUsuarioId: number;
  toUsername: string;
  reason: string;
  notes?: string;
  createdAt: string;
}

export interface CashSessionHandoverAuthResponse {
  handover: CashSessionHandoverResponse;
  operatorSession: OperatorSessionResponse;
}

export interface CashSessionCloseResponse {
  session: CashSessionResponse;
  blockedByTasks: string[];
  missingRequiredTasks: string[];
  blockedByCigarettesCount: boolean;
  hasNonBlockingPendingTasks: boolean;
  pendingNonBlockingTasks: string[];
}

export interface PendingTransferCancelResponse {
  id: number;
  status: string;
}

export interface CartResponse {
  id: number;
  deviceId: number;
  status: string;
  items: CartItem[];
  total: number;
  cigaretteSurcharge?: number;
}

export interface SaleResponse {
  id: number;
  customerId?: number;
  status?: string;
   payments?: Array<{ id: number; paymentMethod: string; status: string; amount: number; reference?: string; confirmedAt?: string }>;
  items?: Array<{ id: number; productCode: string; productName: string; quantity: number; unitPrice: number; subtotal: number }>;
  total: number;
  cigaretteSurcharge?: number;
}

export interface CustomerSummary {
  customerId: number;
  customerName: string;
  totalDebt: number;
  totalCredit: number;
  availableCredit: number;
  creditLimit: number;
  hasOverdueDebt: boolean;
}

export interface CustomerRef {
  id: number;
  fullName: string;
  dni?: string;
  phone?: string;
  status?: string;
  effectiveStatus?: string;
  allowsCredit?: boolean;
  creditLimit?: number;
  currentDebt?: number;
  availableCredit?: number;
  creditUsedPct?: number;
  isCritical?: boolean;
  isCreditBlocked?: boolean;
}

export interface OccasionalCreditCustomerCreateRequest {
  fullName: string;
  phone?: string;
  creditLimit: number;
}

export interface OccasionalContainerCustomerCreateRequest {
  fullName: string;
  phone?: string;
}

export interface CartContainerCheckItem {
  itemId: number;
  productName: string;
  quantity: number;
  containerReturnedNowQty: number;
  owedQty: number;
}

export interface CartContainerCheckResponse {
  cartId: number;
  hasOwedContainers: boolean;
  items: CartContainerCheckItem[];
}

export interface CustomerContainerSummary {
  customerId: number;
  owedByType: Array<{ containerTypeId: number; containerTypeName: string; owedQty: number }>;
}

export interface CashMovement {
  id: number;
  cashSessionId: number;
  method: string;
  signedAmount: number;
  type: string;
  reason: string;
  category?: string;
  createdAt: string;
}

export interface CigaretteCountReport {
  cigaretteCountId: number;
  cashSessionId: number;
  shift: string;
  countDate: string;
  totalDiffQty: number;
}

export interface CigaretteStockBalance {
  productId: number;
  productCode?: string;
  productName?: string;
  vendible: number;
  reclamo: number;
  merma: number;
  total: number;
}

export interface CigaretteCountCreateLine {
  productId: number;
  countedQty: number;
}

export interface CigaretteCountCreateRequest {
  notes?: string;
  lines: CigaretteCountCreateLine[];
}

export interface PendingTransferPayment {
  id: number;
  paymentMethod: string;
  status: string;
  amount: number;
  reference?: string;
}

export type PendingTransferScope = 'current-session' | 'device';

export interface PendingTransferSale {
  saleId: number;
  customerId?: number;
  total: number;
  createdAt: string;
  payments: PendingTransferPayment[];
}

export interface PendingTransferAuthorizeCancelRequest {
  reason: string;
  approverUsername: string;
  approverPassword: string;
  approverPin: string;
}

export interface CashSessionSaleSummary {
  saleId: number;
  createdAt: string;
  status: string;
  total: number;
  customerName: string;
  paymentMethodsLabel: string;
  invoiceNumber?: string;
}

export interface CashSessionSalesListResponse {
  cashSessionId: number;
  totalCount: number;
  totalAmount: number;
  items: CashSessionSaleSummary[];
}

export interface ReturnEligibleSale {
  id: number;
  createdAt: string;
  total: number;
  customerId?: number;
  customerName?: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class PosCajaService {
  private readonly base = '/api/v1';

  constructor(
    private readonly http: HttpClient,
    private readonly operatorSessionService: OperatorSessionService
  ) {}

  private operatorHeaders(): HttpHeaders {
    const session = this.operatorSessionService.getSessionToken();
    if (!session) return new HttpHeaders();
    return new HttpHeaders({ 'X-Operator-Session': session });
  }

  openCashSession(shift: string, openingCash: number): Promise<CashSessionResponse> {
    return firstValueFrom(this.http.post<CashSessionResponse>(`${this.base}/cash-sessions/open`, { shift, openingCash }, { headers: this.operatorHeaders() }));
  }

  getCurrentCashSession(): Promise<CashSessionResponse> {
    return firstValueFrom(this.http.get<CashSessionResponse>(`${this.base}/cash-sessions/current`, { headers: this.operatorHeaders() }));
  }

  getInbox(): Promise<CartInboxItem[]> {
    return firstValueFrom(this.http.get<CartInboxItem[]>(`${this.base}/cashier/inbox`, { headers: this.operatorHeaders() }));
  }

  getCart(cartId: number): Promise<CartResponse> {
    return firstValueFrom(this.http.get<CartResponse>(`${this.base}/carts/${cartId}`, { headers: this.operatorHeaders() }));
  }

  createCart(): Promise<CartResponse> {
    return firstValueFrom(this.http.post<CartResponse>(`${this.base}/carts`, {}, { headers: this.operatorHeaders() }));
  }

  addCartItem(
    cartId: number,
    payload: { productId?: number; productCode: string; productName: string; unitPrice: number; quantity: number; unit: string; discount: number }
  ): Promise<CartItem> {
    return firstValueFrom(this.http.post<CartItem>(`${this.base}/carts/${cartId}/items`, payload, { headers: this.operatorHeaders() }));
  }

  removeCartItem(cartId: number, itemId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/carts/${cartId}/items/${itemId}`, { headers: this.operatorHeaders() }));
  }

  getCartContainerCheck(cartId: number): Promise<CartContainerCheckResponse> {
    return firstValueFrom(this.http.get<CartContainerCheckResponse>(`${this.base}/carts/${cartId}/container-check`, { headers: this.operatorHeaders() }));
  }

  createSaleFromCart(cartId: number, payload: unknown): Promise<SaleResponse> {
    return firstValueFrom(this.http.post<SaleResponse>(`${this.base}/sales/from-cart/${cartId}`, payload, { headers: this.operatorHeaders() }));
  }

  sendCartToCashier(cartId: number): Promise<CartResponse> {
    return firstValueFrom(this.http.post<CartResponse>(`${this.base}/carts/${cartId}/send-to-cashier`, {}, { headers: this.operatorHeaders() }));
  }

  createSaleDirect(payload: unknown): Promise<SaleResponse> {
    return firstValueFrom(this.http.post<SaleResponse>(`${this.base}/sales`, payload, { headers: this.operatorHeaders() }));
  }

  getSaleById(saleId: number): Promise<SaleResponse> {
    return firstValueFrom(this.http.get<SaleResponse>(`${this.base}/sales/${saleId}`, { headers: this.operatorHeaders() }));
  }

  getReturnEligibleSales(hours = 48, q = ''): Promise<ReturnEligibleSale[]> {
    const params = new URLSearchParams();
    params.set('hours', `${hours}`);
    if (q.trim()) params.set('q', q.trim());
    return firstValueFrom(this.http.get<ReturnEligibleSale[]>(`${this.base}/sales/return-eligible?${params.toString()}`, { headers: this.operatorHeaders() }));
  }

  createReturn(
    saleId: number,
    payload: { refundPreference: string; customerId?: number; customerAlias?: string; lines: Array<{ originalSaleItemId: number; qtyReturned: number; condition: string }> }
  ): Promise<{ id: number }> {
    return firstValueFrom(this.http.post<{ id: number }>(`${this.base}/sales/${saleId}/returns`, payload, { headers: this.operatorHeaders() }));
  }

  closeCashSession(cashSessionId: number, payload: unknown): Promise<CashSessionCloseResponse> {
    return firstValueFrom(this.http.post<CashSessionCloseResponse>(`${this.base}/cash-sessions/${cashSessionId}/close`, payload, { headers: this.operatorHeaders() }));
  }

  handoverCashSession(cashSessionId: number, payload: { reason: string; notes?: string }): Promise<CashSessionHandoverResponse> {
    return firstValueFrom(this.http.post<CashSessionHandoverResponse>(`${this.base}/cash-sessions/${cashSessionId}/handover`, payload, { headers: this.operatorHeaders() }));
  }

  handoverCashSessionWithAuth(
    cashSessionId: number,
    payload: { reason: string; notes?: string; newOperatorUsername: string; newOperatorPassword: string; newOperatorPin: string }
  ): Promise<CashSessionHandoverAuthResponse> {
    return firstValueFrom(
      this.http.post<CashSessionHandoverAuthResponse>(`${this.base}/cash-sessions/${cashSessionId}/handover-auth`, payload, { headers: this.operatorHeaders() })
    );
  }

  getCashSessionHandoverHistory(cashSessionId: number): Promise<CashSessionHandoverResponse[]> {
    return firstValueFrom(this.http.get<CashSessionHandoverResponse[]>(`${this.base}/cash-sessions/${cashSessionId}/handover-history`, { headers: this.operatorHeaders() }));
  }

  getPendingTransfers(olderThanHours?: number, scope: PendingTransferScope = 'device'): Promise<PendingTransferSale[]> {
    const params = new URLSearchParams();
    params.set('scope', scope);
    if (olderThanHours) params.set('olderThanHours', `${olderThanHours}`);
    return firstValueFrom(this.http.get<PendingTransferSale[]>(`${this.base}/sales/pending-transfers?${params.toString()}`, { headers: this.operatorHeaders() }));
  }

  getCurrentCashSessionSales(limit = 20, offset = 0): Promise<CashSessionSalesListResponse> {
    const params = new URLSearchParams();
    params.set('limit', `${limit}`);
    params.set('offset', `${offset}`);
    return firstValueFrom(
      this.http.get<CashSessionSalesListResponse>(`${this.base}/cash-sessions/current/sales?${params.toString()}`, { headers: this.operatorHeaders() })
    );
  }

  confirmTransfer(saleId: number, paymentId: number, reference?: string, notes?: string): Promise<unknown> {
    return firstValueFrom(this.http.post(`${this.base}/sales/${saleId}/confirm-transfer`, { paymentId, reference, notes }, { headers: this.operatorHeaders() }));
  }

  cancelPendingTransfer(saleId: number, reason: string): Promise<PendingTransferCancelResponse> {
    return firstValueFrom(
      this.http.post<PendingTransferCancelResponse>(
        `${this.base}/sales/${saleId}/cancel-pending-transfer`,
        { reason },
        { headers: this.operatorHeaders() }
      )
    );
  }

  cancelPendingTransferWithAuthorization(saleId: number, payload: PendingTransferAuthorizeCancelRequest): Promise<PendingTransferCancelResponse> {
    return firstValueFrom(
      this.http.post<PendingTransferCancelResponse>(
        `${this.base}/sales/${saleId}/cancel-pending-transfer/authorize`,
        payload,
        { headers: this.operatorHeaders() }
      )
    );
  }

  getCustomers(): Promise<CustomerRef[]> {
    return firstValueFrom(this.http.get<CustomerRef[]>(`${this.base}/customers`, { headers: this.operatorHeaders() }));
  }

  createOccasionalCreditCustomer(payload: OccasionalCreditCustomerCreateRequest): Promise<CustomerRef> {
    return firstValueFrom(
      this.http.post<CustomerRef>(
        `${this.base}/customers`,
        {
          fullName: payload.fullName,
          phone: payload.phone || null,
          isFixedCustomer: false,
          allowsCredit: true,
          creditLimit: Number(payload.creditLimit || 0),
          status: 'Active'
        },
        { headers: this.operatorHeaders() }
      )
    );
  }

  createOccasionalContainerCustomer(payload: OccasionalContainerCustomerCreateRequest): Promise<CustomerRef> {
    return firstValueFrom(
      this.http.post<CustomerRef>(
        `${this.base}/customers`,
        {
          fullName: payload.fullName,
          phone: payload.phone || null,
          isFixedCustomer: false,
          allowsCredit: false,
          creditLimit: 0,
          status: 'Active'
        },
        { headers: this.operatorHeaders() }
      )
    );
  }

  getCustomerAccountSummary(customerId: number): Promise<CustomerSummary> {
    return firstValueFrom(this.http.get<CustomerSummary>(`${this.base}/customers/${customerId}/account-summary`, { headers: this.operatorHeaders() }));
  }

  async createCustomerAccountPayment(customerId: number, payload: { amount: number; reference?: string; notes?: string }): Promise<{ id: number }> {
    try {
      return await firstValueFrom(
        this.http.post<{ id: number }>(`${this.base}/customers/${customerId}/account/payments`, payload, { headers: this.operatorHeaders() })
      );
    } catch {
      return firstValueFrom(
        this.http.post<{ id: number }>(
          `${this.base}/customers/payment`,
          {
            customerId,
            amount: payload.amount,
            reference: payload.reference,
            notes: payload.notes
          },
          { headers: this.operatorHeaders() }
        )
      );
    }
  }

  getCustomerContainerSummary(customerId: number): Promise<CustomerContainerSummary> {
    return firstValueFrom(this.http.get<CustomerContainerSummary>(`${this.base}/customers/${customerId}/containers/summary`, { headers: this.operatorHeaders() }));
  }

  registerContainerReturn(customerId: number, payload: { containerTypeId: number; qty: number }): Promise<unknown> {
    return firstValueFrom(this.http.post(`${this.base}/customers/${customerId}/containers/return`, payload, { headers: this.operatorHeaders() }));
  }

  getCashMovements(cashSessionId: number): Promise<CashMovement[]> {
    return firstValueFrom(this.http.get<CashMovement[]>(`${this.base}/cash-sessions/${cashSessionId}/money-movements`, { headers: this.operatorHeaders() }));
  }

  createCashMovement(
    cashSessionId: number,
    payload: { method: string; amount: number; type: string; reason: string; category?: string; refType?: string; refId?: number }
  ): Promise<CashMovement> {
    return firstValueFrom(this.http.post<CashMovement>(`${this.base}/cash-sessions/${cashSessionId}/money-movements`, payload, { headers: this.operatorHeaders() }));
  }

  getCigaretteCountsReport(): Promise<CigaretteCountReport[]> {
    return firstValueFrom(this.http.get<CigaretteCountReport[]>(`${this.base}/reports/cigarettes/counts`, { headers: this.operatorHeaders() }));
  }

  getStockCigarettes(): Promise<CigaretteStockBalance[]> {
    return firstValueFrom(this.http.get<CigaretteStockBalance[]>(`${this.base}/reports/stock/cigarettes`, { headers: this.operatorHeaders() }));
  }

  createCigaretteCount(cashSessionId: number, payload: CigaretteCountCreateRequest): Promise<unknown> {
    return firstValueFrom(
      this.http.post(`${this.base}/cash-sessions/${cashSessionId}/cigarettes-count`, payload, { headers: this.operatorHeaders() })
    );
  }

  async getProductByScan(scan: string): Promise<ProductLookupResponse> {
    const code = scan.trim();

    if (code.startsWith('QC:')) {
      const quickCode = encodeURIComponent(code.slice(3));
      try {
        return await firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/by-quickcode/${quickCode}`, { headers: this.operatorHeaders() }));
      } catch {
        return firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/quickcode/${quickCode}`, { headers: this.operatorHeaders() }));
      }
    }

    if (code.startsWith('PID:')) {
      const productId = encodeURIComponent(code.slice(4));
      return firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/${productId}`, { headers: this.operatorHeaders() }));
    }

    const barcode = encodeURIComponent(code);
    try {
      return await firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/by-barcode/${barcode}`, { headers: this.operatorHeaders() }));
    } catch {
      try {
        return await firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/barcode/${barcode}`, { headers: this.operatorHeaders() }));
      } catch {
        const quickCode = encodeURIComponent(code);
        try {
          return await firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/by-quickcode/${quickCode}`, { headers: this.operatorHeaders() }));
        } catch {
          return firstValueFrom(this.http.get<ProductLookupResponse>(`${this.base}/products/quickcode/${quickCode}`, { headers: this.operatorHeaders() }));
        }
      }
    }
  }

  getActiveProducts(): Promise<ProductLookupResponse[]> {
    return firstValueFrom(this.http.get<ProductLookupResponse[]>(`${this.base}/products?status=Active`, { headers: this.operatorHeaders() }));
  }

  createProduct(payload: {
    name: string;
    barcode?: string;
    quickCode?: string;
    saleType: string;
    allowsManualPrice: boolean;
    stockControl: boolean;
    defaultPrice: number;
    defaultPricePerKg: number;
  }): Promise<ProductResponse> {
    return firstValueFrom(this.http.post<ProductResponse>(`${this.base}/products`, payload, { headers: this.operatorHeaders() }));
  }

  getPendingProducts(): Promise<ProductResponse[]> {
    return firstValueFrom(this.http.get<ProductResponse[]>(`${this.base}/products/pending`, { headers: this.operatorHeaders() }));
  }

  updateProduct(productId: number, payload: {
    name?: string;
    barcode?: string;
    quickCode?: string;
    saleType?: string;
    allowsManualPrice?: boolean;
    stockControl?: boolean;
    catalogStatus?: string;
    defaultPrice?: number;
    defaultPricePerKg?: number;
  }): Promise<ProductResponse> {
    return firstValueFrom(this.http.put<ProductResponse>(`${this.base}/products/${productId}`, payload, { headers: this.operatorHeaders() }));
  }
}
