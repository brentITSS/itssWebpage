import React, { useEffect, useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { authService } from '../../services/authService';

const PropertyHubAdmin: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const [canAccessUserManagement, setCanAccessUserManagement] = useState<boolean | null>(null);
  const [accessToast, setAccessToast] = useState<string | null>(null);
  const iconClass = 'h-5 w-5 flex-shrink-0';

  const GroupsIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <rect x="3" y="4" width="8" height="7" rx="1.5" />
      <rect x="13" y="4" width="8" height="7" rx="1.5" />
      <rect x="8" y="13" width="8" height="7" rx="1.5" />
    </svg>
  );

  const PropertiesIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M3 10.5L12 3l9 7.5" />
      <path d="M5.5 9.5V21h13V9.5" />
      <rect x="9.5" y="14" width="5" height="7" rx="0.8" />
    </svg>
  );

  const TenanciesIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M7 4h10l3 3v13H4V4h3z" />
      <path d="M14 4v3h3" />
      <path d="M8 12h8M8 16h6" />
    </svg>
  );

  const LookupsIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <circle cx="11" cy="11" r="7" />
      <path d="M21 21l-4.35-4.35" />
    </svg>
  );

  const DocumentHubIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M7 3h7l5 5v13H7z" />
      <path d="M14 3v5h5" />
      <path d="M10 13h6M10 17h6" />
      <path d="M4 7v13h11" />
    </svg>
  );

  const FlowsIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M4 7h8" />
      <path d="M12 7l-2-2m2 2l-2 2" />
      <path d="M20 17h-8" />
      <path d="M12 17l2-2m-2 2l2 2" />
      <circle cx="6" cy="17" r="2.2" />
      <circle cx="18" cy="7" r="2.2" />
    </svg>
  );

  const UsersIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2" />
      <circle cx="9.5" cy="7.5" r="3.5" />
      <path d="M20 8v6M17 11h6" />
    </svg>
  );

  const LockIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4 flex-shrink-0">
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" />
    </svg>
  );

  useEffect(() => {
    let active = true;

    const loadUser = async () => {
      try {
        const user = await authService.getCurrentUser();
        if (active) {
          setCanAccessUserManagement(Boolean(user.isGlobalAdmin));
        }
      } catch {
        if (active) {
          setCanAccessUserManagement(false);
        }
      }
    };

    loadUser();
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!accessToast) return;
    const timer = window.setTimeout(() => setAccessToast(null), 2800);
    return () => window.clearTimeout(timer);
  }, [accessToast]);

  const navItemClass = ({ isActive }: { isActive: boolean }) =>
    [
      'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition',
      collapsed ? 'xl:justify-center' : '',
      isActive ? 'bg-slate-900 text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
    ].join(' ');

  return (
    <div className="space-y-6">
      {accessToast && (
        <div className="fixed right-4 top-4 z-50 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-900 shadow-lg">
          {accessToast}
        </div>
      )}

      <section className="rounded-xl border border-slate-200 bg-white p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Property Hub Admin</h2>
            <p className="mt-1 text-sm text-slate-500">Manage groups, properties, tenancies, and lookups.</p>
          </div>
          <div className="flex items-center gap-2">
            <Link
              to="/Property Hub/Home"
              className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-slate-400"
            >
              Home
            </Link>
            <Link
              to="/Login"
              className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-slate-400"
              onClick={() => {
                localStorage.removeItem('token');
              }}
            >
              Logout
            </Link>
          </div>
        </div>
      </section>

      <div className="flex flex-col gap-4 xl:flex-row xl:gap-6">
        <aside
          className={`w-full rounded-xl border border-slate-200 bg-white px-3 py-4 transition-all duration-200 xl:sticky xl:top-6 xl:self-start ${
            collapsed ? 'xl:w-20' : 'xl:w-72'
          }`}
        >
          <div className="mb-4 flex items-center justify-between">
            <div className={collapsed ? 'xl:hidden' : 'block'}>
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Property Hub</p>
              <h3 className="mt-1 text-lg font-semibold tracking-tight text-slate-900">Admin Tools</h3>
            </div>
            <button
              type="button"
              onClick={() => setCollapsed((prev) => !prev)}
              className="hidden rounded-lg border border-slate-200 p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700 xl:inline-flex"
              aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
              title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4">
                {collapsed ? <path d="M9 6l6 6-6 6" /> : <path d="M15 6l-6 6 6 6" />}
              </svg>
            </button>
          </div>

          <nav className="grid grid-cols-1 gap-1 sm:grid-cols-2 xl:grid-cols-1">
            <NavLink to="/Property Hub/Admin/Property Groups" className={navItemClass} title="Property Groups">
              <GroupsIcon />
              <span className={collapsed ? 'xl:hidden' : ''}>Property Groups</span>
            </NavLink>
            <NavLink to="/Property Hub/Admin/Properties" className={navItemClass} title="Properties">
              <PropertiesIcon />
              <span className={collapsed ? 'xl:hidden' : ''}>Properties</span>
            </NavLink>
            <NavLink to="/Property Hub/Admin/Tenancies" className={navItemClass} title="Tenancies">
              <TenanciesIcon />
              <span className={collapsed ? 'xl:hidden' : ''}>Tenancies</span>
            </NavLink>
            <NavLink to="/Property Hub/Admin/Lookups" className={navItemClass} title="Lookups">
              <LookupsIcon />
              <span className={collapsed ? 'xl:hidden' : ''}>Lookups</span>
            </NavLink>
            <NavLink to="/Property Hub/Admin/Document Hub" className={navItemClass} title="Document Hub">
              <DocumentHubIcon />
              <span className={collapsed ? 'xl:hidden' : ''}>Document Hub</span>
            </NavLink>
            <NavLink to="/Property Hub/Admin/Document Flows" className={navItemClass} title="Document Flows">
              <FlowsIcon />
              <span className={collapsed ? 'xl:hidden' : ''}>Document Flows</span>
            </NavLink>
            {canAccessUserManagement ? (
              <NavLink to="/Admin/Users" className={navItemClass} title="User Management">
                <UsersIcon />
                <span className={collapsed ? 'xl:hidden' : ''}>User Management</span>
              </NavLink>
            ) : (
              <button
                type="button"
                onClick={() =>
                  setAccessToast('User Management is locked. You need Global Admin access to open it.')
                }
                className={`flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-400 transition ${
                  collapsed ? 'xl:justify-center' : ''
                } hover:bg-slate-100`}
                title="User Management (locked)"
                aria-label="User Management is locked"
              >
                <UsersIcon />
                <span className={collapsed ? 'xl:hidden' : ''}>User Management</span>
                <span
                  className={`ml-auto inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide text-slate-500 ${
                    collapsed ? 'xl:hidden' : ''
                  }`}
                >
                  <LockIcon />
                  Locked
                </span>
              </button>
            )}
          </nav>

          <div
            className={`mt-6 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500 ${
              collapsed ? 'xl:hidden' : ''
            }`}
          >
            Tip: collapse the sidebar for more workspace.
          </div>
        </aside>

        <main className="min-w-0 flex-1 overflow-x-hidden">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default PropertyHubAdmin;
