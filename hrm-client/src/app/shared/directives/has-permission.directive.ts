import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { Permission } from '../../core/auth/permissions';

/**
 * Renders its element only if the user holds the permission (any of them, for a list):
 *
 *   <button *appHasPermission="Permissions.EmployeesCreate">Add employee</button>
 *
 * UX only: the button disappears, but the API still rejects the call for other roles.
 */
@Directive({ selector: '[appHasPermission]' })
export class HasPermission {
  private readonly auth = inject(AuthService);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly container = inject(ViewContainerRef);
  private rendered = false;

  readonly appHasPermission = input.required<Permission | readonly Permission[]>();

  constructor() {
    // Re-runs when the input or the signed-in user changes (e.g. after logout/login).
    effect(() => {
      const wanted = this.appHasPermission();
      const allowed = (Array.isArray(wanted) ? wanted : [wanted]).some((p) =>
        this.auth.hasPermission(p),
      );

      if (allowed && !this.rendered) {
        this.container.createEmbeddedView(this.template);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.container.clear();
        this.rendered = false;
      }
    });
  }
}
