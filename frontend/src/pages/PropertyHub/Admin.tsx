import React, { useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';

const PropertyHubAdmin: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
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

  const UsersIcon = () => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2" />
      <circle cx="9.5" cy="7.5" r="3.5" />
      <path d="M20 8v6M17 11h6" />
    </svg>
  );

  const navItemClass = ({ isActive }: { isActive: boolean }) =>
    [
      'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition',
      collapsed ? 'justify-center' : '',
      isActive ? 'bg-slate-900 text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
    ].join(' ');

  return (
    <div className="space-y-6">
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

      <div className="flex gap-6">
        <aside
          className={`rounded-xl border border-slate-200 bg-white px-3 py-4 transition-all duration-200 ${
            collapsed ? 'w-20' : 'w-72'
          }`}
        >
          <div className="mb-4 flex items-center justify-between">
            <div className={collapsed ? 'hidden' : 'block'}>
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Property Hub</p>
              <h3 className="mt-1 text-lg font-semibold tracking-tight text-slate-900">Admin Tools</h3>
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
            <NavLink to="/Property Hub/Admin/Property Groups" className={navItemClass} title="Property Groups">
              <GroupsIcon />
              {!collapsed && <span>Property Groups</span>}
            </NavLink>
            <NavLink to="/Property Hub/Admin/Properties" className={navItemClass} title="Properties">
              <PropertiesIcon />
              {!collapsed && <span>Properties</span>}
            </NavLink>
            <NavLink to="/Property Hub/Admin/Tenancies" className={navItemClass} title="Tenancies">
              <TenanciesIcon />
              {!collapsed && <span>Tenancies</span>}
            </NavLink>
            <NavLink to="/Property Hub/Admin/Lookups" className={navItemClass} title="Lookups">
              <LookupsIcon />
              {!collapsed && <span>Lookups</span>}
            </NavLink>
            <NavLink to="/Admin/Users" className={navItemClass} title="User Management">
              <UsersIcon />
              {!collapsed && <span>User Management</span>}
            </NavLink>
          </nav>

          {!collapsed && (
            <div className="mt-6 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
              Tip: collapse the sidebar for more workspace.
            </div>
          )}
        </aside>

        <main className="min-w-0 flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default PropertyHubAdmin;
