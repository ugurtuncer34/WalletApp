import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ReloadService {
  private _sig$ = new Subject<void>();
  sig$ = this._sig$.asObservable();
  trigger() { this._sig$.next(); }
}
