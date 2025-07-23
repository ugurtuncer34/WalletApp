import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TransactionReadDto } from '../models/transaction-read-dto';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { TransactionCreateDto } from '../models/transaction-create-dto';
import { TransactionUpdateDto } from '../models/transaction-update-dto';
import { PagedResult } from '../models/paged-result';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  constructor(private http: HttpClient) { }

  list(opts: {
    month?: string;
    accountId?: number;
    categoryId?: number;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDir?: 'asc' | 'desc';
  } = {}) {
    let params = new HttpParams();
    Object.entries(opts).forEach(([k, v]) => {
      if (v !== undefined && v !== null) params = params.set(k, String(v));
    });

    // if (opts.month) params = params.set('month', opts.month); // "2025-07"
    // if (opts.accountId != null) params = params.set('accountId', opts.accountId);
    // if (opts.categoryId != null) params = params.set('categoryId', opts.categoryId);

    return this.http.get<PagedResult<TransactionReadDto>>(`${environment.apiUrl}/transactions`, { params });
  }
  create(body: TransactionCreateDto) {
    return this.http.post<TransactionReadDto>(`${environment.apiUrl}/transactions`, body);
  }
  update(id: number, body: TransactionUpdateDto) {
    return this.http.put(`${environment.apiUrl}/transactions/${id}`, body);
  }
  delete(id: number) {
    return this.http.delete(`${environment.apiUrl}/transactions/${id}`);
  }
}
