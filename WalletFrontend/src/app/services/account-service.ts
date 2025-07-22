import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { AccountReadDto } from '../models/account-read-dto';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private api = `${environment.apiUrl}/accounts`;

  constructor(private http: HttpClient) { }

  list() {
    return this.http.get<AccountReadDto[]>(this.api);
  }
  create(body: Partial<AccountReadDto>){
    return this.http.post(this.api, body);
  }
  delete(id: number){
    return this.http.delete(`${this.api}/${id}`);
  }
  balance(id: number){
    return this.http.get<Number>(`${this.api}/${id}/balance`);
  }
}
