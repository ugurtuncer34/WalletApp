import { Routes } from '@angular/router';
import { HomeComponent } from './components/home-component/home-component';
import { Login } from './components/login/login';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  // unauthenticated
  { path: 'login', component: Login, title: 'Login' },

  // authenticated dashboard (protected)
  { path: 'dashboard', component: HomeComponent, canActivate: [authGuard], title: 'Wallet' },

  // default redirect
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

  // wildcard → login (or 404 page)
  { path: '**', redirectTo: 'dashboard' }
];
