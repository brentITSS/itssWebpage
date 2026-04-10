import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Login from '../pages/Login';
import ForgotPassword from '../pages/ForgotPassword';
import ResetPassword from '../pages/ResetPassword';
import Admin from '../pages/Admin';
import Users from '../pages/Admin/UsersLayoutAlt';
import Roles from '../pages/Admin/Roles';
import Workstreams from '../pages/Admin/Workstreams';
import Permissions from '../pages/Admin/Permissions';
import PropertyHubLayout from '../pages/PropertyHub/Layout';
import PropertyHubHome from '../pages/PropertyHub/Home';
import PropertyHubAdmin from '../pages/PropertyHub/Admin';
import PropertyGroups from '../pages/PropertyHub/Admin/PropertyGroups';
import Properties from '../pages/PropertyHub/Admin/Properties';
import Tenancies from '../pages/PropertyHub/Admin/Tenancies';
import Lookups from '../pages/PropertyHub/Admin/Lookups';
import JournalLogsList from '../pages/PropertyHub/JournalLogs/List';
import JournalLogForm from '../pages/PropertyHub/JournalLogs/Form';
import JournalLogDetail from '../pages/PropertyHub/JournalLogs/Detail';
import ContactLogsList from '../pages/PropertyHub/ContactLogs/List';
import ContactLogForm from '../pages/PropertyHub/ContactLogs/Form';
import ContactLogDetail from '../pages/PropertyHub/ContactLogs/Detail';
import RemindersList from '../pages/PropertyHub/Reminders/List';
import ReminderForm from '../pages/PropertyHub/Reminders/Form';
import ReminderDetail from '../pages/PropertyHub/Reminders/Detail';
import MaintenanceList from '../pages/PropertyHub/Maintenance/List';
import MaintenanceForm from '../pages/PropertyHub/Maintenance/Form';
import MaintenanceDetail from '../pages/PropertyHub/Maintenance/Detail';

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
      {/* Login Route */}
      <Route path="/Login" element={<Login />} />
      <Route path="/ForgotPassword" element={<ForgotPassword />} />
      <Route path="/ResetPassword" element={<ResetPassword />} />

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
          <Route path="Tenancies" element={<Tenancies />} />
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

        <Route path="Reminders" element={<RemindersList />} />
        <Route path="Reminders/New" element={<ReminderForm />} />
        <Route path="Reminders/:id" element={<ReminderDetail />} />

        <Route path="Maintenance" element={<MaintenanceList />} />
        <Route path="Maintenance/New" element={<MaintenanceForm />} />
        <Route path="Maintenance/:id" element={<MaintenanceDetail />} />
      </Route>

      {/* Default redirect */}
      <Route path="/" element={<Navigate to="/Admin" replace />} />
    </Routes>
  );
};

export default AppRoutes;
