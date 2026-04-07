import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoExportsService {
  private readonly base = '/api/v1/exports';

  constructor(private readonly http: HttpClient) {}

  async download(path: string, params: Record<string, string | number | undefined | null>): Promise<void> {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value === undefined || value === null || value === '') continue;
      httpParams = httpParams.set(key, String(value));
    }

    const response = await firstValueFrom(this.http.get(`${this.base}${path}`, {
      params: httpParams,
      responseType: 'blob',
      observe: 'response'
    }));

    this.saveBlob(response);
  }

  private saveBlob(response: HttpResponse<Blob>): void {
    const blob = response.body;
    if (!blob) return;

    const disposition = response.headers.get('content-disposition') ?? '';
    const match = /filename\*?=(?:UTF-8''|\")?([^\";]+)/i.exec(disposition);
    const filename = match?.[1] ? decodeURIComponent(match[1].replace(/\"/g, '')) : `export-${Date.now()}`;

    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }
}
