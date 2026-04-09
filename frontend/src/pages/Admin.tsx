import React from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';

const Admin: React.FC = () => {
  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    [
      'flex items-center rounded-lg px-3 py-2 text-sm font-medium transition',
      isActive
        ? 'bg-[#111827] text-white'
        : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
    ].join(' ');

  return (
    <div className="min-h-screen bg-[#f8fafc]">
      <div className="mx-auto flex min-h-screen w-full max-w-[1440px]">
        <aside className="w-64 border-r border-slate-200 bg-white px-4 py-6">
          <div className="mb-8 rounded-xl border border-slate-200 bg-slate-50 px-3 py-3">
            <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">ITSS Platform</p>
            <h1 className="mt-2 text-lg font-semibold text-slate-900">Global Admin</h1>
            <p className="mt-1 text-xs text-slate-500">User, role, and permission management</p>
          </div>

          <nav>
            <ul className="space-y-2">
              <li>
                <NavLink to="/Admin/Users" className={navLinkClass}>
                  Users
                </NavLink>
              </li>
              <li>
                <NavLink to="/Admin/Roles" className={navLinkClass}>
                  Roles
                </NavLink>
              </li>
              <li>
                <NavLink to="/Admin/Workstreams" className={navLinkClass}>
                  Workstreams
                </NavLink>
              </li>
              <li>
                <NavLink to="/Admin/Permissions" className={navLinkClass}>
                  Permissions
                </NavLink>
              </li>
            </ul>
          </nav>
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
          <main className="min-w-0 flex-1 p-6">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
};

export default Admin;
