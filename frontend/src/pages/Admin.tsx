import React, { useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';

const Admin: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);

  const iconClass = "h-5 w-5 flex-shrink-0";

  const UsersIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2" />
      <circle cx="9.5" cy="7.5" r="3.5" />
      <path d="M20 8v6M17 11h6" />
    </svg>
  );

  const RolesIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M12 3l8 4.5v9L12 21l-8-4.5v-9L12 3z" />
      <path d="M8.5 11.5l2.2 2.2 4.8-4.8" />
    </svg>
  );

  const WorkstreamsIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <rect x="3" y="4" width="8" height="7" rx="1.5" />
      <rect x="13" y="4" width="8" height="7" rx="1.5" />
      <rect x="8" y="13" width="8" height="7" rx="1.5" />
    </svg>
  );

  const PermissionsIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M12 2l7 4v6c0 5-3.5 8.6-7 10-3.5-1.4-7-5-7-10V6l7-4z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );

  const GuideIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 10v6" />
      <circle cx="12" cy="7.5" r="0.8" fill="currentColor" stroke="none" />
    </svg>
  );

  const navItemClass = ({ isActive }: { isActive: boolean }) =>
    [
      'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition',
      collapsed ? 'justify-center' : '',
      isActive
        ? 'bg-slate-900 text-white shadow-sm'
        : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
    ].join(' ');

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="mx-auto flex min-h-screen w-full max-w-[1440px]">
        <aside
          className={`border-r border-slate-200 bg-white px-3 py-4 transition-all duration-200 ${
            collapsed ? 'w-20' : 'w-72'
          }`}
        >
          <div className="mb-4 flex items-center justify-between">
            <div className={collapsed ? 'hidden' : 'block'}>
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">ITSS Platform</p>
              <h1 className="mt-1 text-lg font-semibold tracking-tight text-slate-900">Global Admin</h1>
            </div>
            <button
              type="button"
              onClick={() => setCollapsed((prev) => !prev)}
              className="rounded-lg border border-slate-200 p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
              aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
              title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4">
                {collapsed ? <path d="M9 6l6 6-6 6" /> : <path d="M15 6l-6 6 6 6" />}
              </svg>
            </button>
          </div>

          <nav className="space-y-1">
            <NavLink to="/Admin/Users" className={navItemClass} title="Users">
              <UsersIcon />
              {!collapsed && <span>Users</span>}
            </NavLink>
            <NavLink to="/Admin/Roles" className={navItemClass} title="Roles">
              <RolesIcon />
              {!collapsed && <span>Roles</span>}
            </NavLink>
            <NavLink to="/Admin/Workstreams" className={navItemClass} title="Workstreams">
              <WorkstreamsIcon />
              {!collapsed && <span>Workstreams</span>}
            </NavLink>
            <NavLink to="/Admin/Permissions" className={navItemClass} title="Permissions">
              <PermissionsIcon />
              {!collapsed && <span>Permissions</span>}
            </NavLink>
            <NavLink to="/Admin/Permission Guide" className={navItemClass} title="Permission Guide">
              <GuideIcon />
              {!collapsed && <span>Permission Guide</span>}
            </NavLink>
          </nav>

          {!collapsed && (
            <div className="mt-6 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
              Tip: collapse the sidebar for more content space.
            </div>
          )}
        </aside>

        <div className="flex min-w-0 flex-1 flex-col">
          <header className="border-b border-slate-200 bg-white px-6 py-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-xl font-semibold tracking-tight text-slate-900">Administration</h2>
                <p className="text-sm text-slate-500">Configure access and governance controls</p>
              </div>
              <div className="flex items-center gap-2">
                <Link
                  to="/Property Hub/Home"
                  className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-slate-400"
                >
                  Property Hub
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
          </header>

          <main className="p-6">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
};

export default Admin;
