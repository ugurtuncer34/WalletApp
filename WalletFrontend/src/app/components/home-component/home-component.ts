import { Component, inject } from '@angular/core';
import { TransactionService } from '../../services/transaction-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TransactionCreateDto } from '../../models/transaction-create-dto';
import { AccountService } from '../../services/account-service';
import { CategoryService } from '../../services/category-service';
import { AccountReadDto } from '../../models/account-read-dto';
import { CategoryReadDto } from '../../models/category-read-dto';
import { Observable } from 'rxjs';
import { TransactionReadDto } from '../../models/transaction-read-dto';
import { AccountCreateDto } from '../../models/account-create-dto';

@Component({
  selector: 'app-home-component',
  imports: [CommonModule, FormsModule],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css'
})
export class HomeComponent {
  private txService = inject(TransactionService); // no constructor needed
  private acService = inject(AccountService);
  private ctService = inject(CategoryService);

  // filter state (bound via ngModel)
  selectedMonth: string | null = null;  // "2025-07" from <input type="month">
  selectedAccountId: number | null = null;
  selectedCategoryId: number | null = null;

  // dropdown data
  accounts$: Observable<AccountReadDto[]> = this.acService.list();
  categories$: Observable<CategoryReadDto[]> = this.ctService.list();

  // transactions list (Observable reassigned on each reload)
  transactions$: Observable<TransactionReadDto[]> = this.txService.list();

  // called whenever a filter changes
  reload(): void {
    this.transactions$ = this.txService.list({
      month: this.selectedMonth ?? undefined,
      accountId: this.selectedAccountId ?? undefined,
      categoryId: this.selectedCategoryId ?? undefined
    });
  }

  balance$ = this.acService.balance$;   // just re-expose

  newTransaction: TransactionCreateDto = {
    date: new Date(),
    amount: 0,
    direction: 2,
    accountId: 1,
    categoryId: 1
  };

  newAccount: AccountCreateDto = {
    name: '',
    currency: 1 // default TRY
  };

  txSave() {
    this.txService.create(this.newTransaction).subscribe({
      next: () => {
        this.transactions$ = this.txService.list(); //refresh after post
        this.newTransaction = { ...this.newTransaction, amount: 0 } //reset
      },
      error: err => alert('Save failed: ' + err.message)
    });
  }
  txDelete(id: number) {
    if (!confirm('Delete this transaction?')) return;

    this.txService.delete(id).subscribe({
      next: () => {
        // Refresh the list
        this.transactions$ = this.txService.list();
      },
      error: err => alert('Delete failed: ' + err.message)
    });
  }

  // optional: trackBy for performance
  trackById = (_: number, item: TransactionReadDto) => item.id;

  createAccount() {
    if (!this.newAccount.name.trim()) return alert('Name required');

    this.acService.create(this.newAccount).subscribe({
      next: () => {
        this.accounts$ = this.acService.list();  // refresh list
        this.newAccount = { name: '', currency: 1 };
      },
      error: err => alert('Create failed: ' + err.message)
    });
  }

  deleteAccount(id: number) {
    if (!confirm('Delete this account?')) return;
    this.acService.delete(id).subscribe({
      next: () => this.accounts$ = this.acService.list(),
      error: err => alert('Delete failed: ' + err.message)
    });
  }
}
