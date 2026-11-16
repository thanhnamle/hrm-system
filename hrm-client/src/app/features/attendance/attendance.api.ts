import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { toParams } from '../../core/http/params';
import {
  AttendanceRecord,
  MyAttendanceQuery,
  TeamAttendanceEntry,
  TeamAttendanceQuery,
  TodayAttendance,
} from '../../core/models/attendance';
import { PagedResult } from '../../core/models/common';
import { Department } from '../../core/models/department';

/** Every call the Attendance feature makes. The server records the times; the client sends none. */
@Injectable({ providedIn: 'root' })
export class AttendanceApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/attendance`;

  today() {
    return httpResource<TodayAttendance>(() => `${this.url}/today`);
  }

  mine(query: () => MyAttendanceQuery) {
    return httpResource<PagedResult<AttendanceRecord>>(() => ({
      url: `${this.url}/mine`,
      params: toParams(query()),
    }));
  }

  team(query: () => TeamAttendanceQuery) {
    return httpResource<PagedResult<TeamAttendanceEntry>>(() => ({
      url: this.url,
      params: toParams(query()),
    }));
  }

  departments() {
    return httpResource<Department[]>(() => `${environment.apiBaseUrl}/departments`, {
      defaultValue: [],
    });
  }

  checkIn() {
    return this.http.post<AttendanceRecord>(`${this.url}/check-in`, null);
  }

  checkOut() {
    return this.http.post<AttendanceRecord>(`${this.url}/check-out`, null);
  }
}
