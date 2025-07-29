import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccountService } from '../../services/account-service';
import { CategoryService } from '../../services/category-service';
import { AccountReadDto } from '../../models/account-read-dto';
import { CategoryReadDto } from '../../models/category-read-dto';
import { Observable } from 'rxjs';
import { LoginDto } from '../../models/auth';
import { AuthService } from '../../services/auth-service';
import { AccountList } from '../account-list/account-list';
import { CategoryList } from '../category-list/category-list';
import { TransactionList } from '../transaction-list/transaction-list';
import { ReloadService } from '../../services/reload-service';
import { CategoryExpenseChart } from '../category-expense-chart/category-expense-chart';

@Component({
  selector: 'app-home-component',
  imports: [CommonModule, ReactiveFormsModule, AccountList, CategoryList, TransactionList, CategoryExpenseChart],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css'
})
export class HomeComponent {
  private reloadSvc = inject(ReloadService);
  readonly Math = Math;

  ////// AUTH //////
  isLoggingIn = false;
  auth = inject(AuthService);

  loginForm = new FormGroup({
    email: new FormControl<string>('', [Validators.required, Validators.email]),
    password: new FormControl<string>('', [Validators.required, Validators.minLength(6)])
  });

  doLogin() {
    // mark controls touched so errors show
    this.loginForm.markAllAsTouched();

    if (this.loginForm.invalid) {
      return;
    }

    this.isLoggingIn = true;
    const dto = this.loginForm.value as LoginDto;
    this.auth.login(dto).subscribe({
      next: () => {
        this.isLoggingIn = false;
        this.loginForm.controls.password.reset();
        this.reload();
      },
      error: () => { this.isLoggingIn = false; }
    });
  }

  doLogout() {
    this.auth.logout();
    this.reload(); // will 401, handle gracefully or hide tables when logged out
  }
  ////// AUTH //////

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
