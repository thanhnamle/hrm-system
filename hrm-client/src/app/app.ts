import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ConfirmDialog } from './shared/ui/confirm/confirm-dialog';
import { ToastContainer } from './shared/ui/toast-container/toast-container';

@Component({
  imports: [RouterOutlet, ToastContainer, ConfirmDialog],
  selector: 'app-root',
  template: '<router-outlet /><app-toast-container /><app-confirm-dialog />',
})
export class App {}
