import { Role } from '../auth/permissions';
import { IsoDate, IsoDateTime } from './common';

export type JobChangeType = 'Hired' | 'PositionChanged' | 'Transferred' | 'Terminated';
export type QualificationType = 'Education' | 'Certificate' | 'PreviousJob';

/** GET /api/employees/{id}/profile */
export interface EmployeeProfile {
  employeeId: string;
  account: { role: Role; isLockedOut: boolean } | null; // null: no login yet
  skills: string[];
  qualifications: Qualification[];
  jobHistory: JobHistoryEntry[]; // oldest first
  cv: CvInfo | null;
}

export interface JobHistoryEntry {
  id: string;
  changeType: JobChangeType;
  effectiveDate: IsoDate;
  position: string;
  departmentId: string;
  departmentName: string;
  note: string | null;
}

export interface Qualification {
  id: string;
  type: QualificationType;
  title: string; // degree, certificate or job title
  organization: string; // school, issuer or employer
  startDate: IsoDate | null;
  endDate: IsoDate | null;
  description: string | null;
}

export type QualificationRequest = Omit<Qualification, 'id'>;

export interface CvInfo {
  fileName: string;
  sizeBytes: number;
  uploadedAt: IsoDateTime;
}

/** Same limit as the API (EmployeeDocument.MaxSizeBytes). */
export const MAX_CV_BYTES = 5 * 1024 * 1024;
