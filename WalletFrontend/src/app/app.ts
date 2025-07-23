import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastService } from './services/toast-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('WalletFrontend');
  toast = inject(ToastService);

  ngOnInit() {
    // TEMP: prove toast shows
    // this.toast.show('Toast test'); 
  }
}
