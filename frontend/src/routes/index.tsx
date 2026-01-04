import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Admin from '../pages/Admin';
import Users from '../pages/Admin/Users';
import Roles from '../pages/Admin/Roles';
import Workstreams from '../pages/Admin/Workstreams';
import Permissions from '../pages/Admin/Permissions';
import PropertyHubLayout from '../pages/PropertyHub/Layout';
import PropertyHubHome from '../pages/PropertyHub/Home';
import PropertyHubAdmin from '../pages/PropertyHub/Admin';
import PropertyGroups from '../pages/PropertyHub/Admin/PropertyGroups';
import Properties from '../pages/PropertyHub/Admin/Properties';
import Tenants from '../pages/PropertyHub/Admin/Tenants';
import Lookups from '../pages/PropertyHub/Admin/Lookups';
import JournalLogsList from '../pages/PropertyHub/JournalLogs/List';
import JournalLogForm from '../pages/PropertyHub/JournalLogs/Form';
import JournalLogDetail from '../pages/PropertyHub/JournalLogs/Detail';
import ContactLogsList from '../pages/PropertyHub/ContactLogs/List';
import ContactLogForm from '../pages/PropertyHub/ContactLogs/Form';
import ContactLogDetail from '../pages/PropertyHub/ContactLogs/Detail';

// Protected route wrapper - checks for Global Admin role
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const token = localStorage.getItem('token');
  
  if (!token) {
    return <Navigate to="/Login" replace />;
  }

  // TODO: Add additional check for Global Admin role from token/JWT
  // For now, just check if token exists
  
  return <>{children}</>;
};

const AppRoutes: React.FC = () => {
  return (
    <Routes>
      {/* Global Admin Routes */}
      <Route
        path="/Admin"
        element={
          <ProtectedRoute>
            <Admin />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/Admin/Users" replace />} />
        <Route path="Users" element={<Users />} />
        <Route path="Roles" element={<Roles />} />
        <Route path="Workstreams" element={<Workstreams />} />
        <Route path="Permissions" element={<Permissions />} />
      </Route>

      {/* Property Hub Routes */}
      <Route
        path="/Property Hub"
        element={
          <ProtectedRoute>
            <PropertyHubLayout />
          </ProtectedRoute>
        }
      >
        <Route path="Home" element={<PropertyHubHome />} />

        {/* Property Hub Admin Routes */}
        <Route path="Admin" element={<PropertyHubAdmin />}>
          <Route index element={<Navigate to="/Property Hub/Admin/Property Groups" replace />} />
          <Route path="Property Groups" element={<PropertyGroups />} />
          <Route path="Properties" element={<Properties />} />
          <Route path="Tenants" element={<Tenants />} />
          <Route path="Lookups" element={<Lookups />} />
          {/* User Management route will be added later */}
        </Route>

        {/* Journal Logs Routes */}
        <Route path="Journal Logs" element={<JournalLogsList />} />
        <Route path="Journal Logs/New" element={<JournalLogForm />} />
        <Route path="Journal Logs/:id" element={<JournalLogDetail />} />

        {/* Contact Logs Routes */}
        <Route path="Contact Logs" element={<ContactLogsList />} />
        <Route path="Contact Logs/New" element={<ContactLogForm />} />
        <Route path="Contact Logs/:id" element={<ContactLogDetail />} />
      </Route>

      {/* Default redirect */}
      <Route path="/" element={<Navigate to="/Admin" replace />} />
    </Routes>
  );
};

export default AppRoutes;
