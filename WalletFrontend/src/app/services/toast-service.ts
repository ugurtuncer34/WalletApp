import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _msg$ = new BehaviorSubject<string | null>(null);
  message$ = this._msg$.asObservable();

  show(msg: string) {
    this._msg$.next(msg);
    setTimeout(() => this._msg$.next(null), 3000);
  }
}