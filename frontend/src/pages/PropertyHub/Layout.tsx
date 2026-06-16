import React from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { useAuthAccess } from '../../context/AuthAccessContext';
import { authService } from '../../services/authService';
import { formatRoleWithFirstName, getPropertyHubRoleLabel } from '../../utils/access';

const PropertyHubLayout: React.FC = () => {
  const location = useLocation();
  const { user, isPropertyHubUserOnly } = useAuthAccess();
  const roleLine = (() => {
    const roleLabel = getPropertyHubRoleLabel(user);
    return roleLabel ? formatRoleWithFirstName(roleLabel, user) : null;
  })();
  /** Workstream users (non-admin on Property Hub) only see Home + Logout in the top bar. */
  const showFullTopNav = !isPropertyHubUserOnly;
  // Normalize pathname for matching (handle URL encoding)
  const pathname = decodeURIComponent(location.pathname);
  
  // Check active routes using startsWith for consistent matching
  const isHome = pathname === '/Property Hub/Home' || pathname === '/Property Hub' || pathname === '/Property Hub/';
  const isAdminActive = pathname.startsWith('/Property Hub/Admin');
  const isJournalLogs = pathname.startsWith('/Property Hub/Journal Logs');
  const isContactLogs = pathname.startsWith('/Property Hub/Contact Logs');
  const isReminders = pathname.startsWith('/Property Hub/Reminders');
  const isMaintenance = pathname.startsWith('/Property Hub/Maintenance');

  const tabClass = (active: boolean) =>
    [
      'inline-flex items-center rounded-lg px-3 py-2 text-sm font-medium transition',
      active
        ? 'bg-[#111827] text-white'
        : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
    ].join(' ');

  return (
    <div className="min-h-screen bg-[#f8fafc]">
      <div className="border-b border-slate-200 bg-white">
        <div className="mx-auto w-full max-w-[1440px] px-6 py-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Workspace</p>
              <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Property Hub</h1>
              {roleLine && (
                <p className="mt-1 text-lg font-semibold tracking-tight text-slate-900">{roleLine}</p>
              )}
              <p className="text-sm text-slate-500">Operations, admin, and activity tracking</p>
            </div>
            <div className="flex flex-wrap items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 p-1">
              <Link
                to="/Property Hub/Home"
                className={tabClass(isHome)}
              >
                Home
              </Link>
              {showFullTopNav && (
                <>
                  <Link
                    to="/Property Hub/Admin"
                    className={tabClass(isAdminActive)}
                  >
                    Admin
                  </Link>
                  <Link
                    to="/Property Hub/Journal Logs"
                    className={tabClass(isJournalLogs)}
                  >
                    Journal Logs
                  </Link>
                  <Link
                    to="/Property Hub/Contact Logs"
                    className={tabClass(isContactLogs)}
                  >
                    Contact Logs
                  </Link>
                  <Link
                    to="/Property Hub/Reminders"
                    className={tabClass(isReminders)}
                  >
                    Reminders
                  </Link>
                  <Link
                    to="/Property Hub/Maintenance"
                    className={tabClass(isMaintenance)}
                  >
                    Maintenance
                  </Link>
                </>
              )}
              <button
                type="button"
                className="inline-flex items-center rounded-lg px-3 py-2 text-sm font-medium text-slate-600 transition hover:bg-slate-100 hover:text-slate-900"
                onClick={() => authService.logout()}
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto w-full max-w-[1440px] px-6 py-6">
        <Outlet />
      </div>
    </div>
  );
};

export default PropertyHubLayout;
