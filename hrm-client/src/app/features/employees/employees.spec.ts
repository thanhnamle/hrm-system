import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { polyfillDialog, provideTestApp, signInAs, typeInto } from '../../../testing/test-helpers';
import { Permissions } from '../../core/auth/permissions';
import { EmployeeDetail } from '../../core/models/employee';
import { ConfirmService } from '../../shared/ui/confirm/confirm.service';
import { EmployeeForm } from './employee-form/employee-form';
import { EmployeeList } from './employee-list/employee-list';
import { EmployeeView } from './employee-view/employee-view';

const page = (items: unknown[] = [], extra = {}) => ({
  items,
  page: 1,
  pageSize: 20,
  totalCount: items.length,
  totalPages: 1,
  ...extra,
});
const departments = [
  {
    id: 'eng',
    code: 'ENG',
    name: 'Engineering',
    description: null,
    managerId: null,
    managerName: null,
    employeeCount: 5,
  },
  {
    id: 'sal',
    code: 'SAL',
    name: 'Sales',
    description: null,
    managerId: null,
    managerName: null,
    employeeCount: 3,
  },
];
const employee: EmployeeDetail = {
  id: 'e1',
  employeeCode: 'EMP-0004',
  firstName: 'Huy',
  lastName: 'Le',
  fullName: 'Huy Le',
  email: 'huy.le@hrm.local',
  phone: null,
  dateOfBirth: null,
  hireDate: '2023-11-01',
  position: 'Senior Developer',
  departmentId: 'eng',
  departmentName: 'Engineering',
  isDepartmentManager: false,
  status: 'Active',
  annualLeaveBalance: 12,
  createdAt: '2026-11-03T04:00:00Z',
  updatedAt: null,
};

describe('Employees feature', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp() });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('EmployeeList', () => {
    it('loads active employees first and goes back to page 1 when a filter changes', async () => {
      signInAs('HR', [Permissions.EmployeesView, Permissions.EmployeesCreate]);
      const fixture = TestBed.createComponent(EmployeeList);
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them

      http.expectOne('/api/departments').flush(departments);
      http
        .expectOne((r) => r.url === '/api/employees')
        .flush(
          page(
            [
              {
                id: 'e1',
                employeeCode: 'EMP-0004',
                fullName: 'Huy Le',
                email: 'huy.le@hrm.local',
                position: 'Dev',
                departmentId: 'eng',
                departmentName: 'Engineering',
                status: 'Active',
                hireDate: '2023-11-01',
              },
            ],
            { totalCount: 45, totalPages: 3 },
          ),
        );
      await fixture.whenStable();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Huy Le');
      expect(el.querySelector('a.btn-primary')?.textContent).toContain('Add employee');

      // Page 2, then a filter: the next request must be page 1 again.
      el.querySelectorAll<HTMLButtonElement>('app-pagination button')[1].click();
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      expect(http.expectOne((r) => r.url === '/api/employees').request.params.get('page')).toBe(
        '2',
      );

      typeInto(el, '#department', 'sal');
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      const filtered = http.expectOne((r) => r.url === '/api/employees').request.params;
      expect(filtered.get('departmentId')).toBe('sal');
      expect(filtered.get('status')).toBe('Active');
      expect(filtered.get('page')).toBe('1');
    });

    it('hides "Add employee" from roles without Employees.Create', async () => {
      signInAs('Manager', [Permissions.EmployeesView]);
      const fixture = TestBed.createComponent(EmployeeList);
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      http.expectOne('/api/departments').flush(departments);
      http.expectOne((r) => r.url === '/api/employees').flush(page());
      await fixture.whenStable();

      expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Add employee');
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('No employees found');
    });
  });

  describe('EmployeeForm', () => {
    async function openCreateForm() {
      signInAs('HR', [Permissions.EmployeesCreate]);
      const fixture = TestBed.createComponent(EmployeeForm);
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      http.expectOne('/api/departments').flush(departments);
      await fixture.whenStable();
      const el = fixture.nativeElement as HTMLElement;
      typeInto(el, '#firstName', 'Nam');
      typeInto(el, '#lastName', 'Le');
      typeInto(el, '#email', 'nam.le@hrm.local');
      typeInto(el, '#position', 'Intern Developer');
      typeInto(el, '#departmentId', 'eng');
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      return { fixture, el };
    }

    it('creates the employee with empty optional fields as null, then opens it', async () => {
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      await openCreateForm();

      const request = http.expectOne('/api/employees');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toMatchObject({
        firstName: 'Nam',
        departmentId: 'eng',
        phone: null,
        dateOfBirth: null,
      });
      request.flush({ id: 'new-id' }, { status: 201, statusText: 'Created' });

      expect(navigate).toHaveBeenCalledWith(['/employees', 'new-id']);
    });

    it("shows the API's 400 field error next to the input", async () => {
      const { fixture, el } = await openCreateForm();

      http
        .expectOne('/api/employees')
        .flush(
          { status: 400, errors: { Email: ['Email is already used by an account.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await fixture.whenStable();

      expect(el.querySelector('#email-error')?.textContent).toContain(
        'Email is already used by an account.',
      );
    });
  });

  describe('EmployeeView', () => {
    beforeAll(polyfillDialog);

    it('terminates after confirmation and reloads the record', async () => {
      signInAs('HR', [Permissions.EmployeesView, Permissions.EmployeesTerminate]);
      const fixture = TestBed.createComponent(EmployeeView);
      fixture.componentRef.setInput('id', 'e1');
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      http.expectOne('/api/employees/e1').flush(employee);
      await fixture.whenStable();
      const el = fixture.nativeElement as HTMLElement;
      // Regression: a date-only value must show the same day in every time zone (was off by one in UTC+7).
      expect(el.textContent).toContain('November 1, 2023');
      const confirm = vi.spyOn(TestBed.inject(ConfirmService), 'ask').mockResolvedValue(true);

      [...el.querySelectorAll('button')]
        .find((b) => b.textContent?.trim() === 'Terminate')!
        .click();
      await new Promise((resolve) => setTimeout(resolve)); // let the awaited confirm resolve

      expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ tone: 'danger' }));
      http
        .expectOne('/api/employees/e1/terminate')
        .flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      http.expectOne('/api/employees/e1').flush({ ...employee, status: 'Terminated' });
      await fixture.whenStable();
      expect(el.textContent).toContain('Terminated');
      expect(el.textContent).not.toContain('Edit');
    });

    it('treats 404 as "not found or not visible to your role"', async () => {
      signInAs('Employee', [Permissions.EmployeesView]);
      const fixture = TestBed.createComponent(EmployeeView);
      fixture.componentRef.setInput('id', 'other');
      TestBed.tick(); // send pending resource requests; whenStable() would wait for them
      http.expectOne('/api/employees/other').flush(null, { status: 404, statusText: 'Not Found' });
      await fixture.whenStable();

      expect((fixture.nativeElement as HTMLElement).textContent).toContain(
        "isn't visible to your role",
      );
    });
  });
});
