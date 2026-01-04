import React from 'react';
import { Link, Outlet } from 'react-router-dom';

const Admin: React.FC = () => {
  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <h1 className="text-3xl font-bold text-gray-900">Global Admin</h1>
            <Link
              to="/Login"
              className="text-gray-600 hover:text-gray-900"
              onClick={() => {
                localStorage.removeItem('token');
              }}
            >
              Logout
            </Link>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex">
          {/* Sidebar Navigation */}
          <nav className="w-64 pr-8">
            <div className="bg-white rounded-lg shadow p-4">
              <ul className="space-y-2">
                <li>
                  <Link
                    to="/Admin/Users"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Users
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Admin/Roles"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Roles
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Admin/Workstreams"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Workstreams
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Admin/Permissions"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Permissions
                  </Link>
                </li>
              </ul>
            </div>
          </nav>

          {/* Main Content */}
          <main className="flex-1">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
};

export default Admin;
