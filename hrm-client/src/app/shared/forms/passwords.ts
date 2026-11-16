import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Same rules as the API's Identity password policy, so most weak passwords never reach the server. */
export const PASSWORD_HINT =
  'At least 8 characters, with an upper-case letter, a lower-case letter, a digit and a symbol.';

export const passwordPolicy: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = String(control.value ?? '');
  if (!value) return null; // "required" reports the empty case
  const ok =
    value.length >= 8 &&
    /[A-Z]/.test(value) &&
    /[a-z]/.test(value) &&
    /\d/.test(value) &&
    /[^A-Za-z0-9]/.test(value);
  return ok ? null : { message: PASSWORD_HINT };
};

/**
 * A random temporary password that satisfies the policy, from the browser's cryptographic RNG
 * (never Math.random). Shown once to the admin, who passes it on; the user should change it.
 */
export function generatePassword(length = 14): string {
  const sets = [
    'ABCDEFGHJKLMNPQRSTUVWXYZ',
    'abcdefghijkmnpqrstuvwxyz',
    '23456789',
    '!@#$%^&*-_+=?',
  ];
  const all = sets.join('');
  const random = (max: number) => crypto.getRandomValues(new Uint32Array(1))[0] % max;
  const chars = sets.map((set) => set[random(set.length)]); // one from each set
  while (chars.length < length) chars.push(all[random(all.length)]);
  // Shuffle so the guaranteed characters are not always first.
  for (let i = chars.length - 1; i > 0; i--) {
    const j = random(i + 1);
    [chars[i], chars[j]] = [chars[j], chars[i]];
  }
  return chars.join('');
}
