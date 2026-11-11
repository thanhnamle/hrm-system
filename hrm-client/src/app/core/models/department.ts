/** GET /api/departments. employeeCount counts active employees only. */
export interface Department {
  id: string;
  code: string;
  name: string;
  description: string | null;
  managerId: string | null;
  managerName: string | null;
  employeeCount: number;
}

export interface CreateDepartmentRequest {
  code: string; // letters, digits, - and _; fixed once created
  name: string;
  description?: string | null;
}

export type UpdateDepartmentRequest = Omit<CreateDepartmentRequest, 'code'>;

/** PUT /api/departments/{id}/manager. The employee must belong to the department. */
export interface AssignManagerRequest {
  employeeId: string;
}
