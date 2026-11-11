import { IsoDate, IsoDateTime, PageQuery } from './common';

export interface AttendanceRecord {
  id: string;
  workDate: IsoDate;
  checkInAt: IsoDateTime;
  checkOutAt: IsoDateTime | null;
  workedHours: number | null;
}

export type TeamAttendanceStatus = 'Present' | 'CheckedOut' | 'Absent' | 'OnLeave';

/** One row per active employee in scope for the requested day. */
export interface TeamAttendanceEntry {
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  departmentName: string;
  checkInAt: IsoDateTime | null;
  checkOutAt: IsoDateTime | null;
  onLeave: boolean;
  status: TeamAttendanceStatus;
}

export interface MyAttendanceQuery extends PageQuery {
  from?: IsoDate;
  to?: IsoDate;
}

export interface TeamAttendanceQuery extends PageQuery {
  date?: IsoDate; // default: today in the company time zone
  departmentId?: string;
}
