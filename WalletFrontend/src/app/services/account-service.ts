import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { AccountReadDto } from '../models/account-read-dto';
import { BalanceDto } from '../models/balance-dto';
import { map, Observable, shareReplay } from 'rxjs';
import { AccountCreateDto } from '../models/account-create-dto';
import { AccountUpdateDto } from '../models/account-update-dto';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private api = `${environment.apiUrl}/accounts`;

  constructor(private http: HttpClient) { }

  list() {
    return this.http.get<AccountReadDto[]>(this.api);
  }
  create(body: AccountCreateDto) {
    return this.http.post<AccountReadDto>(`${this.api}`, body);
  }
  update(id: number, body: AccountUpdateDto) {
    return this.http.put(`${this.api}/${id}`, body);
  }
  delete(id: number) {
    return this.http.delete(`${this.api}/${id}`);
  }

  // balance(id: number) {
  //   return this.http.get<BalanceDto>(`${this.api}/${id}/balance`).pipe(
  //     map(r => r.balance)
  //   );
  // } // simpler solution

  // balance cache in order to pipe accounts
  private balanceCache = new Map<number, Observable<number>>(); //remember observables per account id

  invalidateBalance(id: number) {
    this.balanceCache.delete(id); // forget old value
  }

  /** forceRefresh = true -> always hit server  */
  balance(id: number, forceRefresh = false): Observable<number> {
    if (forceRefresh) this.balanceCache.delete(id);

    if (!this.balanceCache.has(id)) {
      const obs = this.http.get<BalanceDto>(`${this.api}/${id}/balance`).pipe(
        map(r => r.balance),
        shareReplay(1)
      );
      this.balanceCache.set(id, obs);
    }
    return this.balanceCache.get(id)!;
  }
  // exposing helper so template can call it
  balance$ = (id: number) => this.balance(id);
}
