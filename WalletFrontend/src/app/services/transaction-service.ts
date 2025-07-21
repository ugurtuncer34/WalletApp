import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TransactionReadDto } from '../models/transaction-read-dto';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { TransactionCreateDto } from '../models/transaction-create-dto';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  constructor(private http: HttpClient) { }

  list(): Observable<TransactionReadDto[]> {
    return this.http.get<TransactionReadDto[]>(`${environment.apiUrl}/transactions`);
  }
  create(body: TransactionCreateDto) {
    return this.http.post<TransactionReadDto>(`${environment.apiUrl}/transactions`, body);
  }
  delete(id: number) {
    return this.http.delete(`${environment.apiUrl}/transactions/${id}`);
  }
}
