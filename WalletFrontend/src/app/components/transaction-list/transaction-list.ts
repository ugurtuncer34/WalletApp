import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, inject, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { AccountReadDto } from '../../models/account-read-dto';
import { CategoryReadDto } from '../../models/category-read-dto';
import { TransactionCreateDto } from '../../models/transaction-create-dto';
import { TransactionReadDto } from '../../models/transaction-read-dto';
import { TransactionUpdateDto } from '../../models/transaction-update-dto';
import { TransactionService } from '../../services/transaction-service';
import { ReloadService } from '../../services/reload-service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-transaction-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './transaction-list.html',
  styleUrl: './transaction-list.css'
})
export class TransactionList {
  @Input() accounts$: Observable<AccountReadDto[]> | null = null;
  @Input() categories$: Observable<CategoryReadDto[]> | null = null;
  @Output() updated = new EventEmitter<void>();
  readonly Math = Math;

  // services
  private txService = inject(TransactionService);
  private reloadSvc = inject(ReloadService);
  private destroyRef = inject(DestroyRef);

  // paging / sorting / filters 
  page = 1;
  pageSize = 20;
  sortBy = 'date';
  sortDir: 'asc' | 'desc' = 'desc';

  selectedMonth: string | null = null; // yyyy-MM
  selectedAccountId: number | null = null;
  selectedCategoryId: number | null = null;

  // paged transactions
  paged$ = this.txService.list(this.currentQuery());

  // add-transaction form 
  newTransaction: TransactionCreateDto = {
    date: new Date(),
    amount: 0,
    direction: 2,
    accountId: 1,
    categoryId: 1
  };

  // edit state
  editingId: number | null = null;
  editTxn: TransactionUpdateDto | null = null;

  ngOnInit() {
    this.reloadSvc.sig$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  /* ----------------- helpers ----------------- */
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

  reload() {
    this.paged$ = this.txService.list(this.currentQuery());
    // this.updated.emit(); // causing infinite loop
  }

  /* ------------- paging / sorting ------------- */
  goTo(p: number) {
    if (p < 1) return;
    this.page = p; this.reload();
  }
  changePageSize(s: number) {
    this.pageSize = s; this.page = 1; this.reload();
  }
  sort(col: string) {
    if (this.sortBy === col) this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = col; this.sortDir = 'asc'; }
    this.reload();
  }

  /* ------------- CRUD handlers --------------- */
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
  cancelEdit() { this.editingId = null; this.editTxn = null; }

  saveEdit(id: number) {
    if (!this.editTxn) return;
    this.txService.update(id, this.editTxn).subscribe(() => {
      // this.acService.invalidateBalance(this.editTxn!.accountId);
      this.cancelEdit();
      this.reload();
      this.updated.emit();
    });
  }
  txDelete(id: number) {
    if (!confirm('Delete this transaction?')) return;
    this.txService.delete(id).subscribe(() => {
      // this.acService.invalidateBalance(deletedTxnAccountId);
      this.reload();
      this.updated.emit();
    });
  }
  txSave() {
    this.txService.create(this.newTransaction).subscribe(() => {
      // this.acService.invalidateBalance(this.newTransaction.accountId);
      this.newTransaction = { ...this.newTransaction, amount: 0 };
      this.page = 1; this.reload();
      this.updated.emit();
    });
  }

  /* trackBy for *ngFor */
  trackById = (_: number, item: TransactionReadDto) => item.id;
}
