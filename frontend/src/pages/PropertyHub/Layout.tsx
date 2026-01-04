import React from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';

const PropertyHubLayout: React.FC = () => {
  const location = useLocation();
  const isAdmin = location.pathname.includes('/Admin');
  const isJournalLogs = location.pathname.includes('/Journal Logs');
  const isContactLogs = location.pathname.includes('/Contact Logs');

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <h1 className="text-3xl font-bold text-gray-900">Property Hub</h1>
            <div className="flex space-x-4">
              <Link
                to="/Property Hub/Home"
                className={`${location.pathname === '/Property Hub/Home' ? 'text-blue-600 font-semibold' : 'text-gray-600'} hover:text-gray-900`}
              >
                Home
              </Link>
              <Link
                to="/Property Hub/Admin"
                className={`${isAdmin ? 'text-blue-600 font-semibold' : 'text-gray-600'} hover:text-gray-900`}
              >
                Admin
              </Link>
              <Link
                to="/Property Hub/Journal Logs"
                className={`${isJournalLogs ? 'text-blue-600 font-semibold' : 'text-gray-600'} hover:text-gray-900`}
              >
                Journal Logs
              </Link>
              <Link
                to="/Property Hub/Contact Logs"
                className={`${isContactLogs ? 'text-blue-600 font-semibold' : 'text-gray-600'} hover:text-gray-900`}
              >
                Contact Logs
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
        <Outlet />
      </div>
    </div>
  );
};

export default PropertyHubLayout;
