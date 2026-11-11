/** A date without time, as the API sends DateOnly: "2026-11-11". */
export type IsoDate = string;

/** A point in time with offset, as the API sends DateTimeOffset: "2026-11-11T02:15:00+00:00". */
export type IsoDateTime = string;

/** Body of every list endpoint. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Query string of every list endpoint. pageSize is 1-100 on the API. */
export interface PageQuery {
  page?: number;
  pageSize?: number;
}

/** Body of a 201 Created response. */
export interface CreatedResponse {
  id: string;
}

/**
 * RFC 7807 error body the API returns for every 4xx/5xx.
 * `errors` is only present on 400 validation failures, keyed by field name.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
