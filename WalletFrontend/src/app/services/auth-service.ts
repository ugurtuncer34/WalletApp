import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, tap } from 'rxjs';
import { AuthResponseDto, LoginDto } from '../models/auth';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private tokenKey = 'wallet_token';

  private tokenSubject = new BehaviorSubject<string | null>(this.load());
  token$ = this.tokenSubject.asObservable();
  get token(): string | null { return this.tokenSubject.value; }

  login(dto: LoginDto){
    return this.http.post<AuthResponseDto>(`${environment.apiUrl}/auth/login`, dto)
      .pipe(tap(res => this.save(res.token)));
  }

  logout(){
    this.save(null);
  }

  private save(token: string | null){
    if(token) localStorage.setItem(this.tokenKey, token);
    else localStorage.removeItem(this.tokenKey);
    this.tokenSubject.next(token);
  }

  private load(): string | null {
    return localStorage.getItem(this.tokenKey);
  }
}
