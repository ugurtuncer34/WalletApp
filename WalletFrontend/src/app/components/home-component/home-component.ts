import { Component, inject } from '@angular/core';
import { TransactionService } from '../../services/transaction-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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

@Component({
  selector: 'app-home-component',
  imports: [CommonModule, FormsModule, AccountList, CategoryList, TransactionList],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css'
})
export class HomeComponent {
  private reloadSvc = inject(ReloadService);
  readonly Math = Math;

  ////// AUTH //////
  loginDto: LoginDto = { email: '', password: '' };
  isLoggingIn = false;
  auth = inject(AuthService);

  doLogin() {
    this.isLoggingIn = true;
    this.auth.login(this.loginDto).subscribe({
      next: () => { this.isLoggingIn = false; this.reload(); }, // reload data now authorized
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
}
