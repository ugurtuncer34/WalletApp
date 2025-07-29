import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { AccountService } from '../../services/account-service';
import { CategoryService } from '../../services/category-service';
import { AccountReadDto } from '../../models/account-read-dto';
import { CategoryReadDto } from '../../models/category-read-dto';
import { Observable } from 'rxjs';
import { AuthService } from '../../services/auth-service';
import { AccountList } from '../account-list/account-list';
import { CategoryList } from '../category-list/category-list';
import { TransactionList } from '../transaction-list/transaction-list';
import { ReloadService } from '../../services/reload-service';
import { CategoryExpenseChart } from '../category-expense-chart/category-expense-chart';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home-component',
  imports: [CommonModule, ReactiveFormsModule, AccountList, CategoryList, TransactionList, CategoryExpenseChart],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css'
})
export class HomeComponent {
  private reloadSvc = inject(ReloadService);
  private router = inject(Router);
  readonly Math = Math;

  auth = inject(AuthService);

  doLogout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  private acService = inject(AccountService);
  private ctService = inject(CategoryService);

  // dropdown data
  accounts$: Observable<AccountReadDto[]> = this.acService.list();
  categories$: Observable<CategoryReadDto[]> = this.ctService.list();

  reloadDropdowns() {
    this.accounts$ = this.acService.list();
    this.categories$ = this.ctService.list();
  }
  reload() {
    this.reloadDropdowns();
    this.reloadSvc.trigger();   // tells every subscriber to refresh itself
  }

  showCategories = false;

  openCategories() {
    this.showCategories = true;
  }

  closeCategories() {
    this.showCategories = false;
  }

  onCategoryUpdated() {
    this.closeCategories();
    this.reload();
  }
}
