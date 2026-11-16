import { TestBed } from '@angular/core/testing';
import { humanize, statusTone } from './badge/badge';
import { ConfirmDialog } from './confirm/confirm-dialog';
import { ConfirmService } from './confirm/confirm.service';
import { Pagination } from './pagination/pagination';

describe('badge helpers', () => {
  it.each([
    ['CheckedOut', 'Checked out'],
    ['OnLeave', 'On leave'],
    ['Pending', 'Pending'],
  ])('humanize(%s) = %s', (value, expected) => expect(humanize(value)).toBe(expected));

  it('maps API statuses to tones, unknown ones to neutral', () => {
    expect(statusTone('Approved')).toBe('success');
    expect(statusTone('Rejected')).toBe('danger');
    expect(statusTone('Something')).toBe('neutral');
  });
});

describe('Pagination', () => {
  async function render(page: number, totalCount: number) {
    const fixture = TestBed.createComponent(Pagination);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', 20);
    fixture.componentRef.setInput('totalCount', totalCount);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const [previous, next] = [...el.querySelectorAll('button')];
    return { fixture, el, previous, next };
  }

  it('shows the range and disables buttons at the ends', async () => {
    const { el, previous, next } = await render(3, 57);

    expect(el.textContent).toContain('41–57');
    expect(el.textContent).toContain('Page 3 of 3');
    expect(previous.disabled).toBe(false);
    expect(next.disabled).toBe(true);
  });

  it('emits the requested page', async () => {
    const { fixture, next } = await render(1, 57);
    const pages: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => pages.push(p));

    next.click();

    expect(pages).toEqual([2]);
  });

  it('renders nothing for an empty list', async () => {
    const { el } = await render(1, 0);
    expect(el.querySelector('nav')).toBeNull();
  });
});

describe('ConfirmService + ConfirmDialog', () => {
  // jsdom has <dialog> but not showModal()/close(); every real browser does. Minimal stand-in.
  beforeAll(() => {
    const proto = HTMLDialogElement.prototype;
    proto.showModal ??= function (this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
    proto.close ??= function (this: HTMLDialogElement) {
      this.removeAttribute('open');
      this.dispatchEvent(new Event('close'));
    };
  });

  it('resolves true when the user confirms', async () => {
    const fixture = TestBed.createComponent(ConfirmDialog);
    const confirm = TestBed.inject(ConfirmService);

    const answer = confirm.ask({
      title: 'Delete department?',
      message: 'This cannot be undone.',
      confirmText: 'Delete',
      tone: 'danger',
    });
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('dialog')?.open).toBe(true);
    expect(el.textContent).toContain('This cannot be undone.');

    [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Delete')!.click();

    await expect(answer).resolves.toBe(true);
    await fixture.whenStable();
    expect(el.querySelector('dialog')?.open).toBe(false);
  });

  it('resolves false on cancel, and an unanswered question when a newer one arrives', async () => {
    const confirm = TestBed.inject(ConfirmService);

    const first = confirm.ask({ title: 'First', message: '' });
    const second = confirm.ask({ title: 'Second', message: '' });
    confirm.answer(false);

    await expect(first).resolves.toBe(false);
    await expect(second).resolves.toBe(false);
  });
});
