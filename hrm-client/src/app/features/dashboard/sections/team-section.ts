import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TeamSummary, TodayStatus } from '../../../core/models/dashboard';
import { Badge, BadgeTone } from '../../../shared/ui/badge/badge';
import { AttendanceBar, LeaveList, StatTile } from '../widgets/widgets';

/** Same colours as the attendance bar legend. */
const STATUS: Record<TodayStatus, { label: string; tone: BadgeTone }> = {
  Working: { label: 'Working', tone: 'success' },
  CheckedOut: { label: 'Checked out', tone: 'brand' },
  OnLeave: { label: 'On leave', tone: 'warning' },
  NotCheckedIn: { label: 'Not checked in', tone: 'neutral' },
};

/** Manager: their department today. */
@Component({
  selector: 'app-team-section',
  host: { class: 'block' }, // custom elements are inline; block so the page spacing applies
  imports: [DatePipe, RouterLink, Badge, StatTile, AttendanceBar, LeaveList],
  template: `
    @let t = team();
    <section class="space-y-4" aria-labelledby="team-heading">
      <h2 id="team-heading" class="text-sm font-semibold tracking-wide text-muted uppercase">
        My team · {{ t.departmentName }}
      </h2>

      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <app-stat-tile label="Team members" icon="users" link="/employees" [value]="t.teamSize" />
        <app-stat-tile
          label="Checked in today"
          icon="clock"
          link="/attendance/team"
          [value]="t.presentToday"
          [hint]="isWorkingDay() ? 'of ' + t.teamSize + ' team members' : 'Weekend today'"
        />
        <app-stat-tile
          label="On leave today"
          icon="calendar"
          link="/attendance/team"
          [value]="t.onLeaveToday"
          [hint]="t.upcomingLeave.length + ' away in the next 14 days'"
        />
        <app-stat-tile
          label="Requests to review"
          icon="inbox"
          link="/leave/approvals"
          [value]="t.pendingApprovals"
          [hint]="t.pendingApprovals ? 'Oldest first below' : 'Inbox is clear'"
        />
      </div>

      <div class="grid gap-4 lg:grid-cols-5">
        <div class="card lg:col-span-3">
          <div class="mb-4 flex items-baseline justify-between gap-2">
            <h3 class="font-semibold">Team today</h3>
            <a routerLink="/attendance/team" class="text-sm text-brand-700 hover:underline"
              >Attendance →</a
            >
          </div>
          <app-attendance-bar [data]="t.attendanceToday" />
          <ul class="mt-4 divide-y divide-border">
            @for (m of t.members; track m.employeeId) {
              <li class="flex items-center justify-between gap-3 py-2.5 text-sm">
                <span class="min-w-0">
                  <a
                    class="block truncate font-medium text-ink hover:text-brand-700"
                    [routerLink]="['/employees', m.employeeId]"
                    >{{ m.name }}</a
                  >
                  <span class="block truncate text-xs text-muted">{{ m.position }}</span>
                </span>
                <span class="flex shrink-0 items-center gap-3">
                  @if (m.checkInAt) {
                    <span class="text-xs text-muted tabular-nums">
                      {{ m.checkInAt | date: 'shortTime' }}
                      @if (m.checkOutAt) {
                        – {{ m.checkOutAt | date: 'shortTime' }}
                      }
                    </span>
                  }
                  <app-badge [tone]="status[m.status].tone">{{ status[m.status].label }}</app-badge>
                </span>
              </li>
            }
          </ul>
        </div>

        <div class="space-y-4 lg:col-span-2">
          <div class="card">
            <h3 class="mb-1 font-semibold">Waiting for you</h3>
            <p class="text-xs text-muted">Oldest requests first.</p>
            <app-leave-list
              mode="pending"
              [requests]="t.pendingRequests"
              [today]="today()"
              emptyText="No requests are waiting."
            />
          </div>
          <div class="card">
            <h3 class="mb-1 font-semibold">Who's away</h3>
            <p class="text-xs text-muted">Today and the next 14 days.</p>
            <app-leave-list
              [requests]="t.upcomingLeave"
              [today]="today()"
              emptyText="Nobody in the team is away."
            />
          </div>
        </div>
      </div>
    </section>
  `,
})
export class TeamSection {
  readonly team = input.required<TeamSummary>();
  readonly today = input.required<string>();
  readonly isWorkingDay = input.required<boolean>();
  protected readonly status = STATUS;
}
