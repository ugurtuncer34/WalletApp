import { Component, inject } from '@angular/core';
import { TransactionService } from '../../services/transaction-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TransactionCreateDto } from '../../models/transaction-create-dto';
import { AccountService } from '../../services/account-service';
import { CategoryService } from '../../services/category-service';
import { AccountReadDto } from '../../models/account-read-dto';
import { CategoryReadDto } from '../../models/category-read-dto';

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

  transactions$ = this.txService.list(); // Observable<TransactionReadDto[]>
  accounts$ = this.acService.list();
  categories$ = this.ctService.list();

  balance$  = this.acService.balance$;   // just re-expose

  newTransaction: TransactionCreateDto = {
    date: new Date(),
    amount: 0,
    direction: 2,
    accountId: 1,
    categoryId: 1
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
}
