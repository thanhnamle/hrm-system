import { HttpParams } from '@angular/common/http';

type QueryValue = string | number | boolean | null | undefined;

/**
 * Builds a query string from a query object, skipping empty values, so feature APIs can pass
 * their whole filter state: toParams({ search: '', page: 2 }) → "page=2".
 */
export function toParams(query: object = {}): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query) as [string, QueryValue][]) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
