import type { UserDto, WorkstreamAccessDto } from '../services/authService';

/** Mirrors backend Property Hub workstream matching (see AuthService.HasPropertyHubAdminAccess). */
export function isPropertyHubWorkstreamName(workstreamName: string): boolean {
  const n = workstreamName.trim();
  if (!n) return false;
  const lower = n.toLowerCase();
  return lower.includes('property hub') || lower.includes('property');
}

export function getPropertyHubWorkstreamEntries(user: UserDto): WorkstreamAccessDto[] {
  return (user.workstreamAccess ?? []).filter((wa) => isPropertyHubWorkstreamName(wa.workstreamName));
}

export function hasPropertyHubWorkstreamAccess(user: UserDto | null | undefined): boolean {
  if (!user) return false;
  if (user.isGlobalAdmin) return true;
  return getPropertyHubWorkstreamEntries(user).length > 0;
}

/** Workstream admin for Property Hub: Admin permission on a Property Hub workstream (or Global Admin). */
export function isPropertyHubWorkstreamAdmin(user: UserDto | null | undefined): boolean {
  if (!user) return false;
  if (user.isGlobalAdmin) return true;
  return getPropertyHubWorkstreamEntries(user).some(
    (wa) => wa.permissionTypeName?.trim().toLowerCase() === 'admin'
  );
}

/** Has Property Hub access but not admin on any Property Hub workstream (non–Global Admin). */
export function isPropertyHubWorkstreamUserOnly(user: UserDto | null | undefined): boolean {
  if (!user || user.isGlobalAdmin) return false;
  const entries = getPropertyHubWorkstreamEntries(user);
  if (entries.length === 0) return false;
  return !entries.some((wa) => wa.permissionTypeName?.trim().toLowerCase() === 'admin');
}

export function getPostLoginPath(user: UserDto): string {
  if (user.mustChangePassword) return '/ChangePassword';
  const landing = user.defaultLoginLandingPage?.trim();
  if (landing) return landing;
  if (user.isGlobalAdmin) return '/Admin';
  if (hasPropertyHubWorkstreamAccess(user)) return '/Property Hub/Home';
  return '/AccessDenied';
}

export function getRootPathForAuthenticatedUser(user: UserDto): string {
  return getPostLoginPath(user);
}
