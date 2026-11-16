import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainer } from './shared/ui/toast-container/toast-container';

@Component({
  imports: [RouterOutlet, ToastContainer],
  selector: 'app-root',
  template: '<router-outlet /><app-toast-container />',
})
export class App {}
