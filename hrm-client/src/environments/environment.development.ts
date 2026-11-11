/** Development settings (ng serve). Replaces environment.ts via fileReplacements in angular.json. */
export const environment = {
  production: false,
  apiBaseUrl: '/api', // forwarded to http://localhost:5183 by proxy.conf.json
};
