import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, inject, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../../services/account-service';
import { Observable } from 'rxjs';
import { AccountReadDto } from '../../models/account-read-dto';
import { AccountCreateDto } from '../../models/account-create-dto';
import { AccountItem } from '../account-item/account-item';
import { ReloadService } from '../../services/reload-service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-account-list',
  imports: [CommonModule, FormsModule, AccountItem],
  templateUrl: './account-list.html',
  styleUrl: './account-list.css'
})
export class AccountList {
  @Output() updated = new EventEmitter<void>();

  private acService = inject(AccountService);
  accounts$: Observable<AccountReadDto[]> = this.acService.list();

  private reloadSvc = inject(ReloadService);
  private destroyRef = inject(DestroyRef);

  ngOnInit() {
    this.reloadSvc.sig$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  newAccount: AccountCreateDto = { name: '', currency: 1 };

  // balance$ = this.acService.balance$;   // just re-expose

  createAccount() {
    if (!this.newAccount.name.trim()) return alert('Name required');

    this.acService.create(this.newAccount).subscribe({
      next: () => {
        this.accounts$ = this.acService.list();  // refresh list
        this.newAccount = { name: '', currency: 1 };
        this.updated.emit();
      },
    });
  }

  reload() {
    this.accounts$ = this.acService.list();
    // this.updated.emit(); // causing stack overflow
  }

  onAccountRemoved() {
    this.reload();
    this.updated.emit(); // emit only when an account is deleted
  }

  trackById = (_: number, item: AccountReadDto) => item.id;

}
