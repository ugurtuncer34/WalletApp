import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, inject, Input, Output } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, take, tap } from 'rxjs';

import { AccountReadDto } from '../../models/account-read-dto';
import { CategoryReadDto } from '../../models/category-read-dto';
import { TransactionCreateDto } from '../../models/transaction-create-dto';
import { TransactionReadDto } from '../../models/transaction-read-dto';
import { TransactionUpdateDto } from '../../models/transaction-update-dto';
import { PagedResult } from '../../models/paged-result';
import { TransactionService } from '../../services/transaction-service';
import { ReloadService } from '../../services/reload-service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './transaction-list.html',
  styleUrls: ['./transaction-list.css']
})
export class TransactionList {
  @Input() accounts$: Observable<AccountReadDto[]> | null = null;
  @Input() categories$: Observable<CategoryReadDto[]> | null = null;
  @Output() updated = new EventEmitter<void>();

  readonly Math = Math;

  private txService = inject(TransactionService);
  private reloadSvc = inject(ReloadService);
  private destroyRef = inject(DestroyRef);

  // paging / sorting / filters 
  page = 1;
  pageSize = 20;
  sortBy = 'date';
  sortDir: 'asc' | 'desc' = 'desc';

  selectedMonth: string | null = null;
  selectedAccountId: number | null = null;
  selectedCategoryId: number | null = null;

  // summary fields
  totalTransactions = 0;
  // totalAmount = 0;
  firstItem = 0;
  lastItem = 0;

  // the observable for the table
  paged$ = this.loadPage();

  // add-transaction form 
  // newTransaction: TransactionCreateDto = {
  //   date: new Date().toISOString().slice(0, 10),
  //   amount: 0,
  //   direction: 2,
  //   accountId: 0,
  //   categoryId: 0
  // };
  // replaced for reactive forms
  txnForm = new FormGroup({
    date: new FormControl<string>(new Date().toISOString().slice(0, 10), Validators.required),
    accountId: new FormControl<number | null>(null, Validators.required),
    categoryId: new FormControl<number | null>(null, Validators.required),
    direction: new FormControl<number>(2, Validators.required),
    amount: new FormControl<number>(0, [Validators.required, Validators.min(0.01)])
  });

  // edit state...
  editingId: number | null = null;
  editTxn?: TransactionUpdateDto | null = null;

  ngOnInit() {
    this.accounts$?.pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(a => { if (a.length) this.txnForm.controls.accountId.setValue(a[0].id); });
    this.categories$?.pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(c => { if (c.length) this.txnForm.controls.categoryId.setValue(c[0].id); });

    // reload when signalled
    this.reloadSvc.sig$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  /** helper to build & tap summary */
  private loadPage() {
    return this.txService.list(this.currentQuery()).pipe(
      tap(res => this.updateSummary(res))
    );
  }

  /** update pagination & amount summary */
  private updateSummary(res: PagedResult<TransactionReadDto>) {
    this.totalTransactions = res.totalCount;
    // this.totalAmount = res.items
    //   .reduce((sum, x) => sum + (x.direction === 'Income' ? x.amount : -x.amount), 0);
    this.firstItem = (this.page - 1) * this.pageSize + 1;
    this.lastItem = this.firstItem + res.items.length - 1;
  }

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
    this.paged$ = this.loadPage();
  }

  goTo(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.reload();
  }

  changePageSize(s: number) {
    this.page = 1;
    this.pageSize = s;
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

  // txSave() {
  //   this.txService.create(this.newTransaction).subscribe(() => {
  //     this.newTransaction.amount = 0;
  //     this.newTransaction.date = new Date().toISOString().slice(0, 10);
  //     this.page = 1;
  //     this.reload();
  //     this.updated.emit();
  //   });
  // }
  // replaced for reactive forms
  txSave() {
    this.txnForm.markAllAsTouched();
    
    if (this.txnForm.invalid) return;

    const v = this.txnForm.value;
    const dto: TransactionCreateDto = {
      date: new Date(v.date!).toISOString().slice(0, 10),
      accountId: v.accountId!,
      categoryId: v.categoryId!,
      direction: v.direction!,
      amount: v.amount!
    };

    this.txService.create(dto).subscribe(() => {
      // reset only the amount & date
      this.txnForm.controls.amount.setValue(0);
      this.txnForm.controls.date.setValue(new Date().toISOString().slice(0,10));
      this.page = 1; this.reload(); this.updated.emit();
    });
  }

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

  /* ─── reset & export ──────────────────────────────────────────────────── */
  resetFilters() {
    this.selectedMonth = null;
    this.selectedAccountId = null;
    this.selectedCategoryId = null;
    this.page = 1;
    this.reload();
  }

  exportCsv() {
    this.paged$.pipe(take(1)).subscribe(res => {
      const headers = ['Date', 'Account', 'Category', 'Direction', 'Amount'];
      const rows = res.items.map(x => [
        x.date.toString(), x.accountName, x.categoryName, x.direction, x.amount.toFixed(2)
      ]);
      const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
      console.log(csv);
    });
  }

  /* ─── pagination getters ───────────────────────────────────────────────── */
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalTransactions / this.pageSize));
  }
  get hasPrev(): boolean { return this.page > 1; }
  get hasNext(): boolean { return this.page < this.totalPages; }

  trackById = (_: number, item: TransactionReadDto) => item.id;
}