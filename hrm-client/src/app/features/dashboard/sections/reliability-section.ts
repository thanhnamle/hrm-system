import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataQualitySummary, HealthStatus, SystemStatus } from '../../../core/models/dashboard';
import { Badge, BadgeTone } from '../../../shared/ui/badge/badge';
import { Icon, Meter } from '../widgets/widgets';

export const HEALTH: Record<HealthStatus, { label: string; tone: BadgeTone }> = {
  Healthy: { label: 'Operational', tone: 'success' },
  Degraded: { label: 'Degraded', tone: 'warning' },
  Unhealthy: { label: 'Down', tone: 'danger' },
};

/** Health check names from the API, as people would say them. */
const COMPONENT_NAMES: Record<string, string> = { postgresql: 'Database (PostgreSQL)' };

/** "1.0.0+3f2a9c1d…" → "1.0.0 (3f2a9c1)": the build number and the short commit it came from. */
export function shortVersion(version: string): string {
  const [number, commit] = version.split('+');
  return commit ? `${number} (${commit.slice(0, 7)})` : number;
}

/** "3 d 4 h", "2 h 15 min", "6 min". */
export function uptime(from: string, to: Date): string {
  const minutes = Math.max(0, Math.floor((to.getTime() - new Date(from).getTime()) / 60_000));
  const d = Math.floor(minutes / 1440);
  const h = Math.floor((minutes % 1440) / 60);
  const m = minutes % 60;
  if (d) return `${d} d ${h} h`;
  if (h) return `${h} h ${m} min`;
  return `${m} min`;
}

/** Admin and HR: is the system up, and how complete is the data in it? */
@Component({
  selector: 'app-reliability-section',
  host: { class: 'block' }, // custom elements are inline; block so the page spacing applies
  imports: [DatePipe, RouterLink, Badge, Icon, Meter],
  templateUrl: './reliability-section.html',
})
export class ReliabilitySection {
  readonly system = input.required<SystemStatus>();
  readonly quality = input.required<DataQualitySummary>();
  /** Measured in the browser: how long the dashboard call took, end to end. */
  readonly roundTripMs = input<number | null>(null);
  readonly canManageUsers = input(false);

  protected readonly health = HEALTH;
  protected readonly componentName = (name: string) => COMPONENT_NAMES[name] ?? name;
  protected readonly version = computed(() => shortVersion(this.system().version));
  protected readonly uptime = computed(() =>
    uptime(this.system().startedAt, new Date(this.system().checkedAt)),
  );

  protected readonly accounts = computed(() =>
    this.quality().accountsByRole.reduce((sum, r) => sum + r.count, 0),
  );

  /** Share of the four profile items (login, CV, skills, education) filled in across active employees. */
  protected readonly completeness = computed(() => {
    const q = this.quality();
    const total = q.activeEmployees * 4;
    const missing = q.withoutLogin + q.withoutCv + q.withoutSkills + q.withoutQualifications;
    return total ? Math.round(((total - missing) / total) * 100) : 100;
  });

  protected readonly meters = computed(() => {
    const q = this.quality();
    return [
      { label: 'Have a login', value: q.activeEmployees - q.withoutLogin },
      { label: 'Have a CV', value: q.activeEmployees - q.withoutCv },
      { label: 'Have skills listed', value: q.activeEmployees - q.withoutSkills },
      { label: 'Have education or experience', value: q.activeEmployees - q.withoutQualifications },
    ];
  });
}
