import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Route params (e.g. /employees/:id) arrive as component inputs.
    provideRouter(routes, withComponentInputBinding()),
    // Interceptors (auth, refresh, errors) are added here with the AuthService.
    provideHttpClient(),
  ],
};
