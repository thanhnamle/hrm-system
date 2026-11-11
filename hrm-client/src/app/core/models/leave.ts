import { IsoDate, IsoDateTime, PageQuery } from './common';

export type LeaveType = 'Annual' | 'Sick' | 'Unpaid';
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  departmentName: string;
  type: LeaveType;
  startDate: IsoDate;
  endDate: IsoDate;
  days: number; // working days (Mon-Fri)
  reason: string;
  status: LeaveStatus;
  reviewedByName: string | null;
  reviewedAt: IsoDateTime | null;
  reviewNote: string | null;
  createdAt: IsoDateTime;
}

/** GET /api/leave-requests/balance. `available` = balance - pending annual days. */
export interface LeaveBalance {
  annualLeaveBalance: number;
  pendingAnnualDays: number;
  available: number;
}

export interface SubmitLeaveRequest {
  type: LeaveType;
  startDate: IsoDate;
  endDate: IsoDate;
  reason: string;
}

/** Body of approve (optional note) and reject (note required). */
export interface ReviewNote {
  note?: string | null;
}

export interface MyLeaveQuery extends PageQuery {
  status?: LeaveStatus;
}

/** GET /api/leave-requests (HR/Admin). from/to match requests overlapping that period. */
export interface LeaveQuery extends MyLeaveQuery {
  type?: LeaveType;
  employeeId?: string;
  departmentId?: string;
  from?: IsoDate;
  to?: IsoDate;
}
