import {
  DailyAttendance,
  DashboardSummary,
  MonthHeadcount,
  MonthLeave,
  PersonalSummary,
  TeamSummary,
  WeekHours,
} from '../../core/models/dashboard';
import { LeaveRequest } from '../../core/models/leave';
import type { IconName } from './widgets/widgets';

/**
 * Data storytelling: turns the dashboard numbers into sentences. Chart titles state the takeaway
 * ("Attendance averaged 92%…") instead of naming the metric, and the banner at the top says what
 * matters today. Pure functions of the API data, so every sentence is unit-tested.
 */

export type Tone = 'good' | 'warning' | 'neutral';

export interface StoryPoint {
  icon: IconName;
  tone: Tone;
  text: string;
  link?: string;
}

export interface Story {
  headline: string;
  hero: { value: number; suffix: string; label: string };
  points: StoryPoint[];
}

export interface Insight {
  title: string;
  subtitle: string;
}

// ---- Formatting ---------------------------------------------------------------------------

const plural = (n: number, one: string, many = `${one}s`) => `${n} ${n === 1 ? one : many}`;
const round1 = (n: number) => Math.round(n * 10) / 10;
export const pct = (v: number) => `${Math.round(v)}%`;

/** "2026-11-17" as a local date (not UTC, which can shift it a day). */
export const localDate = (iso: string) => {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
};
export const dayLabel = (iso: string) =>
  localDate(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
export const monthLabel = (iso: string) =>
  localDate(iso).toLocaleDateString('en-US', { month: 'short' });
export const monthName = (iso: string) =>
  localDate(iso).toLocaleDateString('en-US', { month: 'long' });
const timeLabel = (iso: string) =>
  new Date(iso).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });

export const attendanceRate = (d: DailyAttendance) =>
  d.expected ? (d.present / d.expected) * 100 : 0;
const mean = (values: number[]) => values.reduce((a, b) => a + b, 0) / Math.max(1, values.length);
const daysSince = (iso: string) => Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);

/** Full days only: today is still in progress, so it would drag the average down every morning. */
const pastDays = (trend: DailyAttendance[], today: string) =>
  trend.filter((d) => d.date < today && d.expected > 0);

// ---- Chart takeaways --------------------------------------------------------------------------

export function attendanceInsight(trend: DailyAttendance[], today: string): Insight {
  const past = pastDays(trend, today);
  if (!past.length) {
    return {
      title: 'Attendance will appear here once people check in',
      subtitle: 'Share of employed people who checked in, per working day',
    };
  }
  const avg = mean(past.map(attendanceRate));
  const lowest = past.reduce((a, b) => (attendanceRate(b) < attendanceRate(a) ? b : a));
  return {
    title: `Attendance averaged ${pct(avg)} over the last ${plural(past.length, 'working day')}`,
    subtitle: `Lowest on ${dayLabel(lowest.date)} at ${pct(attendanceRate(lowest))} · share of employed people who checked in`,
  };
}

export function headcountInsight(trend: MonthHeadcount[]): Insight {
  if (!trend.length) return { title: 'Headcount', subtitle: '' };
  const first = trend[0].headcount;
  const last = trend[trend.length - 1].headcount;
  const hires = trend.reduce((s, m) => s + m.hires, 0);
  const leavers = trend.reduce((s, m) => s + m.leavers, 0);
  const title =
    last > first
      ? `Headcount grew from ${first} to ${last} in 12 months`
      : last < first
        ? `Headcount fell from ${first} to ${last} in 12 months`
        : `Headcount held steady at ${last} over 12 months`;
  const busiest = trend.reduce((a, b) => (b.hires > a.hires ? b : a));
  const busiestText = busiest.hires ? ` · most hires in ${monthName(busiest.month)}` : '';
  return {
    title,
    subtitle: `${plural(hires, 'hire')}, ${plural(leavers, 'leaver')}${busiestText} · people employed at each month's end`,
  };
}

export const leaveTotal = (m: MonthLeave) => m.annual + m.sick + m.unpaid;

export function leaveInsight(months: MonthLeave[], today: string): Insight {
  const current = today.slice(0, 7);
  const done = months.filter((m) => m.month.slice(0, 7) <= current);
  const ahead = months.filter((m) => m.month.slice(0, 7) > current);
  const peak = done.reduce<MonthLeave | null>(
    (a, b) => (!a || leaveTotal(b) > leaveTotal(a) ? b : a),
    null,
  );
  const upcoming = ahead.reduce((s, m) => s + leaveTotal(m), 0);
  const all = months.reduce((s, m) => s + leaveTotal(m), 0);
  const annualShare = all ? (months.reduce((s, m) => s + m.annual, 0) / all) * 100 : 0;

  const title =
    peak && leaveTotal(peak)
      ? `Leave peaked in ${monthName(peak.month)} with ${plural(round1(leaveTotal(peak)), 'day')}`
      : 'No leave approved in the last six months';
  const aheadText = upcoming
    ? `${plural(round1(upcoming), 'day')} already approved for the next two months`
    : 'Nothing approved for the next two months yet';
  return {
    title,
    subtitle: all ? `${aheadText} · annual leave is ${pct(annualShare)} of all days` : aheadText,
  };
}

export function hoursInsight(weeks: WeekHours[]): Insight {
  const done = weeks.slice(0, -1).filter((w) => w.daysPresent > 0);
  const thisWeek = weeks.at(-1);
  const sofar = thisWeek
    ? `This week so far: ${round1(thisWeek.hours)} h over ${plural(thisWeek.daysPresent, 'day')}`
    : '';
  if (!done.length)
    return { title: 'Your hours will appear here as you check in and out', subtitle: sofar };
  return {
    title: `You worked ${round1(mean(done.map((w) => w.hours)))} h a week on average`,
    subtitle: `${sofar} · completed days only, last ${weeks.length} weeks`,
  };
}

// ---- Banners --------------------------------------------------------------------------------

function waitingSentence(count: number, oldest: LeaveRequest | undefined, forYou: boolean): string {
  if (!count) return forYou ? 'Nothing is waiting for you.' : 'No leave requests are waiting.';
  const who = forYou ? 'wait for you' : 'wait for review';
  const age = oldest ? daysSince(oldest.createdAt) : 0;
  return `${plural(count, 'leave request')} ${who}${age >= 2 ? `, the oldest for ${age} days` : ''}.`;
}

function attendancePoint(
  trend: DailyAttendance[],
  today: string,
  isWorkingDay: boolean,
): StoryPoint | null {
  const past = pastDays(trend, today);
  const now = trend.find((d) => d.date === today);
  if (!past.length || !isWorkingDay || !now) return null;
  const avg = mean(past.map(attendanceRate));
  return {
    icon: 'clock',
    tone: 'neutral',
    text: `So far today ${pct(attendanceRate(now))} have checked in; the 30-day average is ${pct(avg)}`,
    link: '/attendance/team',
  };
}

export function organizationStory(s: DashboardSummary): Story | null {
  const org = s.organization;
  if (!org) return null;
  const present = org.attendanceToday.working + org.attendanceToday.checkedOut;

  const first = s.isWorkingDay
    ? `${present} of ${org.headcount} people are in today${org.onLeaveToday ? `, ${org.onLeaveToday} on leave` : ''}.`
    : `It's the weekend; ${org.headcount} people are on the team.`;

  const points: StoryPoint[] = [];
  const attendance = attendancePoint(org.attendanceTrend, s.today, s.isWorkingDay);
  if (attendance) points.push(attendance);

  const head = headcountInsight(org.headcountTrend);
  points.push({ icon: 'users', tone: 'neutral', text: head.title, link: '/employees' });

  if (org.upcomingLeave.length) {
    points.push({
      icon: 'calendar',
      tone: 'neutral',
      text: `${plural(org.upcomingLeave.length, 'person', 'people')} away in the next 14 days`,
      link: '/leave/approvals',
    });
  }

  const q = s.dataQuality;
  if (q && q.activeEmployees) {
    points.push(
      q.withoutCv
        ? {
            icon: 'file',
            tone: 'warning',
            text: `${q.withoutCv} of ${q.activeEmployees} employees have no CV on file`,
            link: '/employees',
          }
        : { icon: 'file', tone: 'good', text: 'Every active employee has a CV on file' },
    );
  }

  if (s.system) {
    points.push(
      s.system.status === 'Healthy'
        ? { icon: 'pulse', tone: 'good', text: 'All systems operational' }
        : {
            icon: 'pulse',
            tone: 'warning',
            text: `System ${s.system.status.toLowerCase()}: check the status panel`,
          },
    );
  }

  return {
    headline: `${first} ${waitingSentence(org.pendingApprovals, org.pendingRequests[0], false)}`,
    hero: s.isWorkingDay
      ? {
          value: org.headcount ? Math.round((present / org.headcount) * 100) : 0,
          suffix: '%',
          label: 'checked in today',
        }
      : { value: org.headcount, suffix: '', label: 'active employees' },
    points,
  };
}

export function teamStory(team: TeamSummary, today: string, isWorkingDay: boolean): Story {
  const first = isWorkingDay
    ? `${team.presentToday} of ${team.teamSize} people in ${team.departmentName} are in today.`
    : `It's the weekend; ${team.teamSize} people are in ${team.departmentName}.`;

  const points: StoryPoint[] = [];
  const attendance = attendancePoint(team.attendanceTrend, today, isWorkingDay);
  if (attendance) points.push(attendance);

  const missing = team.members.filter((m) => m.status === 'NotCheckedIn');
  if (isWorkingDay && missing.length) {
    const names = missing.slice(0, 2).map((m) => m.name.split(' ')[0]);
    const rest = missing.length > 2 ? ` and ${missing.length - 2} more` : '';
    points.push({
      icon: 'user',
      tone: 'neutral',
      text: `Not checked in yet: ${names.join(', ')}${rest}`,
      link: '/attendance/team',
    });
  }
  points.push(
    team.upcomingLeave.length
      ? {
          icon: 'calendar',
          tone: 'neutral',
          text: `${plural(team.upcomingLeave.length, 'person', 'people')} away in the next 14 days`,
        }
      : { icon: 'calendar', tone: 'good', text: 'The whole team is here for the next two weeks' },
  );

  return {
    headline: `${first} ${waitingSentence(team.pendingApprovals, team.pendingRequests[0], true)}`,
    hero: {
      value: team.presentToday,
      suffix: ` / ${team.teamSize}`,
      label: 'team members in today',
    },
    points,
  };
}

export function personalStory(me: PersonalSummary, today: string, isWorkingDay: boolean): Story {
  const available = me.annualLeaveBalance - me.pendingAnnualDays;
  const checkIn = me.checkedOutAt
    ? `You're done for today (${timeLabel(me.checkedInAt!)} – ${timeLabel(me.checkedOutAt)}).`
    : me.checkedInAt
      ? `You're checked in since ${timeLabel(me.checkedInAt)}.`
      : isWorkingDay
        ? "You haven't checked in yet today."
        : 'Enjoy your weekend.';
  const next = me.nextLeave
    ? ` Your next leave starts ${me.nextLeave.startDate <= today ? 'today' : dayLabel(me.nextLeave.startDate)}.`
    : '';

  const points: StoryPoint[] = [];
  const hours = hoursInsight(me.hoursByWeek);
  if (me.hoursByWeek.slice(0, -1).some((w) => w.daysPresent)) {
    points.push({ icon: 'clock', tone: 'neutral', text: hours.title, link: '/attendance' });
  }
  const m = me.attendanceThisMonth;
  if (m.workingDaysSoFar) {
    points.push({
      icon: 'calendar',
      tone: 'neutral',
      text: `Present ${m.daysPresent} of ${plural(m.workingDaysSoFar, 'working day')} this month`,
      link: '/attendance',
    });
  }
  const missing = [
    !me.profile.skillsCount && 'skills',
    !me.profile.qualificationsCount && 'education',
    !me.profile.hasCv && 'CV',
  ].filter(Boolean) as string[];
  points.push(
    missing.length
      ? {
          icon: 'user',
          tone: 'warning',
          text: `Your profile has no ${missing.join(', ')} yet: ask HR to add it`,
        }
      : { icon: 'user', tone: 'good', text: 'Your profile is complete' },
  );
  if (me.pendingRequests) {
    points.push({
      icon: 'inbox',
      tone: 'neutral',
      text: `${plural(me.pendingRequests, 'leave request')} waiting for approval`,
      link: '/leave',
    });
  }

  return {
    headline: `${checkIn}${next}`,
    hero: { value: Math.max(0, round1(available)), suffix: '', label: 'annual leave days left' },
    points,
  };
}
