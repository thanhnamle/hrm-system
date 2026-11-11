/**
 * Production settings (ng build). Everything here ends up in the browser bundle and is readable
 * by anyone: never put secrets in an environment file.
 *
 * The API is always reached at the same origin under /api: Nginx forwards it in production,
 * proxy.conf.json does the same during `ng serve`. That keeps the refresh cookie same-site
 * and avoids CORS entirely.
 */
export const environment = {
  production: true,
  apiBaseUrl: '/api',
};
