import React from 'react';
import { Link, Outlet } from 'react-router-dom';

const PropertyHubAdmin: React.FC = () => {
  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <h1 className="text-3xl font-bold text-gray-900">Property Hub Admin</h1>
            <div className="flex space-x-4">
              <Link
                to="/Property Hub/Home"
                className="text-gray-600 hover:text-gray-900"
              >
                Home
              </Link>
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
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex">
          {/* Sidebar Navigation */}
          <nav className="w-64 pr-8">
            <div className="bg-white rounded-lg shadow p-4">
              <ul className="space-y-2">
                <li>
                  <Link
                    to="/Property Hub/Admin/Property Groups"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Property Groups
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Property Hub/Admin/Properties"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Properties
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Property Hub/Admin/Tenants"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Tenants
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Property Hub/Admin/Lookups"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    Lookups
                  </Link>
                </li>
                <li>
                  <Link
                    to="/Property Hub/Admin/User Management"
                    className="block px-4 py-2 rounded-md hover:bg-gray-100 text-gray-700 hover:text-gray-900"
                  >
                    User Management
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

export default PropertyHubAdmin;
