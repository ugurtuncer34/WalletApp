import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, inject, Input, Output } from '@angular/core';
import { AccountReadDto } from '../../models/account-read-dto';
import { AccountService } from '../../services/account-service';
import { Observable } from 'rxjs';
import { AccountUpdateDto } from '../../models/account-update-dto';
import { FormsModule } from '@angular/forms';
import { ReloadService } from '../../services/reload-service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-account-item',
  imports: [CommonModule, FormsModule],
  templateUrl: './account-item.html',
  styleUrl: './account-item.css'
})
export class AccountItem {
  @Input() account!: AccountReadDto;
  @Output() updated = new EventEmitter<void>();
  @Output() removed = new EventEmitter<void>();

  private acService = inject(AccountService);
  balance$!: Observable<number>;

  private reloadSvc = inject(ReloadService);
  private destroyRef = inject(DestroyRef);

  isEditing = false;
  editModel: AccountUpdateDto = { name: '', currency: 1 };

  ngOnInit() {
    this.balance$ = this.acService.balance(this.account.id);
    this.reloadSvc.sig$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  reload() {
    this.balance$ = this.acService.balance(this.account.id, true);
  }

  startEdit() {
    this.isEditing = true;
    this.editModel = {
      name: this.account.name,
      currency: this.currencyStringToNumber(this.account.currency)
    };
  }

  cancel() {
    this.isEditing = false;
  }

  save() {
    this.acService.update(this.account.id, this.editModel).subscribe({
      next: () => {
        this.updated.emit();
        this.isEditing = false;
      }
    });
  }

  delete() {
    if (!confirm('Delete this account?')) return;
    this.acService.delete(this.account.id).subscribe({
      next: () => this.removed.emit()
    });
  }

  private currencyStringToNumber(cur: string): number {
    switch (cur) {
      case 'TRY': return 1;
      case 'EUR': return 2;
      case 'USD': return 3;
      default: return 1;
    }
  }

  balanceClass(value: number | null): string {
    if (value === null) return '';
    return value >= 0 ? 'balance-positive' : 'balance-negative';
  }
}
