import { Role } from '../auth/permissions';

/** GET /api/users (Admin). */
export interface UserAccount {
  id: string;
  email: string;
  role: Role;
  employeeId: string | null;
  fullName: string | null;
  isLockedOut: boolean;
}

/** POST /api/users. The login email is taken from the employee record. */
export interface CreateUserAccountRequest {
  employeeId: string;
  role: Role;
  password: string;
}

export interface ChangeRoleRequest {
  role: Role;
}

export interface ResetPasswordRequest {
  newPassword: string;
}
