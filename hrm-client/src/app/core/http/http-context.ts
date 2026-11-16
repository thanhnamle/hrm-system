import { HttpContextToken } from '@angular/common/http';

/**
 * Set on a request whose caller shows the error itself (e.g. a form showing field errors),
 * so the error interceptor doesn't also pop up a toast.
 */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);
