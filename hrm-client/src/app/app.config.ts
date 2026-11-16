import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor, refreshInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { errorInterceptor } from './core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Route params and query params (e.g. ?returnUrl=) arrive as component inputs.
    provideRouter(routes, withComponentInputBinding()),
    // Order matters: a request passes these top to bottom, the response bottom to top.
    provideHttpClient(withInterceptors([authInterceptor, refreshInterceptor, errorInterceptor])),
    // Before the first route is resolved: use the refresh cookie to restore the session after a reload.
    provideAppInitializer(() => inject(AuthService).restoreSession()),
  ],
};
