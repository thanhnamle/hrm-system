import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { polyfillDialog, provideTestApp, signInAs, typeInto } from '../../../testing/test-helpers';
import { Permissions } from '../../core/auth/permissions';
import { DepartmentList } from './department-list/department-list';

const departments = [
  {
    id: 'eng',
    code: 'ENG',
    name: 'Engineering',
    description: 'Product',
    managerId: 'm1',
    managerName: 'Minh Nguyen',
    employeeCount: 5,
  },
];

describe('DepartmentList', () => {
  let http: HttpTestingController;
  beforeAll(polyfillDialog);

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp() });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render() {
    const fixture = TestBed.createComponent(DepartmentList);
    TestBed.tick(); // runs effects: the resource sends its request
    http.expectOne('/api/departments').flush(departments);
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('is read-only for roles without Departments.Manage', async () => {
    signInAs('Employee', [Permissions.DepartmentsView]);
    const { el } = await render();

    expect(el.textContent).toContain('Minh Nguyen');
    expect(el.textContent).not.toContain('Add department');
    expect(el.textContent).not.toContain('Delete');
  });

  it('creates a department from the modal and reloads the list', async () => {
    signInAs('HR', [Permissions.DepartmentsView, Permissions.DepartmentsManage]);
    const { fixture, el } = await render();

    [...el.querySelectorAll('button')]
      .find((b) => b.textContent?.trim() === 'Add department')!
      .click();
    await fixture.whenStable();
    expect(el.querySelector('dialog')?.open).toBe(true);

    typeInto(el, '#code', 'QA');
    typeInto(el, '#name', 'Quality');
    el.querySelector<HTMLFormElement>('#department-form')!.dispatchEvent(new Event('submit'));

    const request = http.expectOne((r) => r.method === 'POST' && r.url === '/api/departments');
    expect(request.request.body).toEqual({ code: 'QA', name: 'Quality', description: null });
    request.flush({ id: 'qa' }, { status: 201, statusText: 'Created' });
    TestBed.tick();

    http
      .expectOne('/api/departments')
      .flush([...departments, { ...departments[0], id: 'qa', code: 'QA', name: 'Quality' }]);
    await fixture.whenStable();
    expect(el.querySelector('dialog')?.open).toBe(false);
    expect(el.textContent).toContain('Quality');
  });
});
