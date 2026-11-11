import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes)],
    }).compileComponents();
  });

  it('creates the app', () => {
    expect(TestBed.createComponent(App).componentInstance).toBeTruthy();
  });

  it('redirects the root URL to the dashboard inside the shell', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(TestBed.inject(Router).url).toBe('/dashboard');
    expect(page.querySelector('app-sidebar')).toBeTruthy();
    expect(page.querySelector('h1')?.textContent).toContain('Dashboard');
  });

  it('shows the not-found page for unknown URLs', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/does-not-exist');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toContain(
      'Page not found',
    );
  });
});
