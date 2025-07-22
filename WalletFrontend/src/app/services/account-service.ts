import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { AccountReadDto } from '../models/account-read-dto';
import { BalanceDto } from '../models/balance-dto';
import { map, Observable, shareReplay } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private api = `${environment.apiUrl}/accounts`;

  constructor(private http: HttpClient) { }

  list() {
    return this.http.get<AccountReadDto[]>(this.api);
  }
  create(body: Partial<AccountReadDto>) {
    return this.http.post(this.api, body);
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
  
  balance(id: number): Observable<number> {
    if (!this.balanceCache.has(id)) {
      const obs = this.http.get<BalanceDto>(`${this.api}/${id}/balance`).pipe(
        map(r => r.balance),
        shareReplay(1) //make each observable “sticky” so multiple subscribers don’t re-call HTTP
      );
      this.balanceCache.set(id, obs);
    }
    return this.balanceCache.get(id)!;
  }
  // exposing helper so template can call it
  balance$ = (id: number) => this.balance(id);
}
