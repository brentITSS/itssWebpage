import React from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';

const Admin: React.FC = () => {
  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    [
      'flex items-center rounded-xl px-3 py-2.5 text-sm font-medium transition',
      isActive
        ? 'bg-indigo-500/20 text-white ring-1 ring-indigo-400/40'
        : 'text-slate-300 hover:bg-white/10 hover:text-white',
    ].join(' ');

  return (
    <div className="min-h-screen bg-slate-950">
      <div className="mx-auto flex min-h-screen w-full max-w-[1400px]">
        <aside className="w-72 border-r border-white/10 bg-slate-900 px-5 py-6">
          <div className="mb-8">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-300">ITSS Platform</p>
            <h1 className="mt-2 text-xl font-semibold text-white">Global Admin</h1>
            <p className="mt-1 text-sm text-slate-400">
              User, role, and permission management
            </p>
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

        <div className="flex min-w-0 flex-1 flex-col bg-slate-100">
          <header className="border-b border-slate-200 bg-white/80 px-6 py-4 backdrop-blur">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-xl font-semibold tracking-tight text-slate-900">Administration</h2>
                <p className="text-sm text-slate-500">Configure access and governance controls</p>
              </div>
              <div className="flex items-center gap-2">
                <Link
                  to="/Property Hub/Home"
                  className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                >
                  Property Hub
                </Link>
                <Link
                  to="/Login"
                  className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
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
