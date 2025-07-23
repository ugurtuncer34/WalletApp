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
import { TransactionUpdateDto } from '../../models/transaction-update-dto';
import { AccountUpdateDto } from '../../models/account-update-dto';
import { CategoryCreateDto } from '../../models/category-create-dto';
import { CategoryUpdateDto } from '../../models/category-update-dto';
import { LoginDto } from '../../models/auth';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-home-component',
  imports: [CommonModule, FormsModule],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css'
})
export class HomeComponent {
  readonly Math = Math;
  ////// AUTH //////
  loginDto: LoginDto = { email: '', password: '' };
  isLoggingIn = false;
  auth = inject(AuthService);

  doLogin() {
    this.isLoggingIn = true;
    this.auth.login(this.loginDto).subscribe({
      next: () => { this.isLoggingIn = false; this.reload(); }, // reload data now authorized
      error: err => { this.isLoggingIn = false; alert('Login failed'); }
    });
  }

  doLogout() {
    this.auth.logout();
    this.reload(); // will 401, handle gracefully or hide tables when logged out
  }
  ////// AUTH //////

  private txService = inject(TransactionService); // no constructor needed
  private acService = inject(AccountService);
  private ctService = inject(CategoryService);

  page = 1;
  pageSize = 20;
  sortBy = 'date';
  sortDir: 'asc' | 'desc' = 'desc';

  // filter state (bound via ngModel)
  selectedMonth: string | null = null;  // "2025-07" from <input type="month">
  selectedAccountId: number | null = null;
  selectedCategoryId: number | null = null;

  // dropdown data
  accounts$: Observable<AccountReadDto[]> = this.acService.list();
  categories$: Observable<CategoryReadDto[]> = this.ctService.list();

  // transactions list (Observable reassigned on each reload)
  // transactions$: Observable<TransactionReadDto[]> = this.txService.list();
  paged$ = this.txService.list(this.currentQuery());

  currentQuery() {
    return {
      month: this.selectedMonth ?? undefined,
      accountId: this.selectedAccountId ?? undefined,
      categoryId: this.selectedCategoryId ?? undefined,
      page: this.page,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortDir: this.sortDir
    };
  }

  // called whenever a filter changes
  reload() {
    this.paged$ = this.txService.list(this.currentQuery());
  }

  // helpers
  goTo(page: number) {
    if (page < 1) return;
    this.page = page;
    this.reload();
  }

  changePageSize(size: number) {
    this.pageSize = size;
    this.page = 1;
    this.reload();
  }

  sort(col: string) {
    if (this.sortBy === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = col;
      this.sortDir = 'asc';
    }
    this.reload();
  }

  balance$ = this.acService.balance$;   // just re-expose

  currencyStringToNumber(cur: string): number {
    switch (cur) {
      case 'TRY': return 1;
      case 'EUR': return 2;
      case 'USD': return 3;
      default: return 1;
    }
  }

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

  newCategory: CategoryCreateDto = {
    name: ''
  };

  editingId: number | null = null;
  editTxn: TransactionUpdateDto | null = null;

  startEdit(t: TransactionReadDto) {
    this.editingId = t.id;
    this.editTxn = {
      date: t.date.toString(),
      amount: t.amount,
      direction: t.direction === 'Income' ? 1 : 2,
      accountId: t.accountId,
      categoryId: t.categoryId
    };
  }

  cancelEdit() {
    this.editingId = null;
    this.editTxn = null;
  }

  saveEdit(id: number) {
    if (!this.editTxn) return;
    this.txService.update(id, this.editTxn).subscribe({
      next: () => {
        this.cancelEdit();
        this.reload();
      },
      error: err => alert('Update failed: ' + err.message)
    });
  }

  txSave() {
    this.txService.create(this.newTransaction).subscribe({
      next: () => {
        this.newTransaction = { ...this.newTransaction, amount: 0 }; //reset
        this.page = 1;
        this.reload();
      },
      error: err => alert('Save failed: ' + err.message)
    });
  }
  txDelete(id: number) {
    if (!confirm('Delete this transaction?')) return;

    this.txService.delete(id).subscribe({
      next: () => {
        this.reload();
      },
      error: err => alert('Delete failed: ' + err.message)
    });
  }

  // optional: trackBy for performance when not using new @for usage
  trackById = (_: number, item: TransactionReadDto) => item.id;

  //////// Account section
  editingAccountId: number | null = null;
  editAccount: AccountUpdateDto | null = null;

  editingCategoryId: number | null = null;
  editCategory: CategoryUpdateDto | null = null;

  startEditAccount(a: AccountReadDto) {
    this.editingAccountId = a.id;
    this.editAccount = {
      name: a.name,
      currency: this.currencyStringToNumber(a.currency) // readDto string updateDto number (backend enum)
    };
  }

  cancelEditAccount() {
    this.editingAccountId = null;
    this.editAccount = null;
  }

  saveEditAccount(id: number) {
    if (!this.editAccount) return;
    this.acService.update(id, this.editAccount).subscribe({
      next: () => {
        this.accounts$ = this.acService.list();  // refresh
        this.cancelEditAccount();
      },
      error: err => alert('Update failed: ' + err.message)
    });
  }

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

  /////// Category section
  startEditCategory(c: CategoryReadDto) {
    this.editingCategoryId = c.id;
    this.editCategory = {
      name: c.name
    };
  }

  cancelEditCategory() {
    this.editingCategoryId = null;
    this.editCategory = null;
  }

  saveEditCategory(id: number) {
    if (!this.editCategory) return;
    this.ctService.update(id, this.editCategory).subscribe({
      next: () => {
        this.categories$ = this.ctService.list();  // refresh
        this.cancelEditCategory();
      },
      error: err => alert('Update failed: ' + err.message)
    });
  }

  createCategory() {
    if (!this.newCategory.name.trim()) return alert('Name required');

    this.ctService.create(this.newCategory).subscribe({
      next: () => {
        this.categories$ = this.ctService.list();
        this.newCategory = { name: '' };
      },
      error: err => alert('Create failed: ' + err.message)
    });
  }

  deleteCategory(id: number) {
    if (!confirm('Delete this category?')) return;
    this.ctService.delete(id).subscribe({
      next: () => this.categories$ = this.ctService.list(),
      error: err => alert('Delete failed: ' + err.message)
    });
  }
}
