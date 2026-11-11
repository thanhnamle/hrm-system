import { Permission, Role } from '../auth/permissions';
import { IsoDateTime } from './common';

export interface LoginRequest {
  email: string;
  password: string;
}

/** GET /api/auth/me, and the `user` part of login/refresh. */
export interface AuthUser {
  id: string;
  email: string;
  role: Role;
  employeeId: string | null; // null for the system admin account
  fullName: string | null;
  permissions: Permission[];
}

/** POST /api/auth/login and /api/auth/refresh. The refresh token is only in an HttpOnly cookie. */
export interface AuthResponse {
  accessToken: string;
  expiresAt: IsoDateTime;
  user: AuthUser;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
