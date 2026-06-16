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

function getPropertyHubPermissionRank(permissionTypeName: string | undefined): number {
  const p = permissionTypeName?.trim().toLowerCase();
  if (!p) return 0;
  if (p === 'admin') return 3;
  if (p === 'edit' || p === 'editor') return 2;
  if (p === 'view' || p === 'read' || p === 'readonly') return 1;
  return 1;
}

function formatPermissionTypeLabel(permissionTypeName: string): string {
  const trimmed = permissionTypeName.trim();
  if (!trimmed) return 'User';
  return trimmed.charAt(0).toUpperCase() + trimmed.slice(1).toLowerCase();
}

/** Display label for the user's Property Hub access level (Global Admin, Admin, Edit, View, etc.). */
export function getPropertyHubRoleLabel(user: UserDto | null | undefined): string | null {
  if (!user) return null;
  if (user.isGlobalAdmin) return 'Global Admin';

  const entries = getPropertyHubWorkstreamEntries(user);
  if (entries.length === 0) return null;

  if (entries.some((wa) => wa.permissionTypeName?.trim().toLowerCase() === 'admin')) {
    return 'Admin';
  }

  let bestPermission = '';
  let bestRank = 0;
  for (const entry of entries) {
    const rank = getPropertyHubPermissionRank(entry.permissionTypeName);
    if (rank > bestRank) {
      bestRank = rank;
      bestPermission = entry.permissionTypeName?.trim() ?? '';
    }
  }

  return bestPermission ? formatPermissionTypeLabel(bestPermission) : 'User';
}

/** e.g. "Admin (Leezil)" when first name is set, otherwise "Admin". */
export function formatRoleWithFirstName(roleLabel: string, user: UserDto | null | undefined): string {
  const first = user?.firstName?.trim();
  return first ? `${roleLabel} (${first})` : roleLabel;
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
