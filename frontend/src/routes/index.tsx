import React, { useEffect } from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { useAuthAccess } from '../context/AuthAccessContext';
import {
  getRootPathForAuthenticatedUser,
  hasPropertyHubWorkstreamAccess,
  isPropertyHubWorkstreamAdmin,
} from '../utils/access';
import Login from '../pages/Login';
import ForgotPassword from '../pages/ForgotPassword';
import ResetPassword from '../pages/ResetPassword';
import ChangePassword from '../pages/ChangePassword';
import Admin from '../pages/Admin';
import Users from '../pages/Admin/UsersLayoutAlt';
import Roles from '../pages/Admin/Roles';
import Workstreams from '../pages/Admin/Workstreams';
import Permissions from '../pages/Admin/Permissions';
import PermissionGuide from '../pages/Admin/PermissionGuide';
import PropertyHubLayout from '../pages/PropertyHub/Layout';
import PropertyHubHome from '../pages/PropertyHub/Home';
import PropertyHubAdmin from '../pages/PropertyHub/Admin';
import PropertyGroups from '../pages/PropertyHub/Admin/PropertyGroups';
import Properties from '../pages/PropertyHub/Admin/Properties';
import Tenancies from '../pages/PropertyHub/Admin/Tenancies';
import Lookups from '../pages/PropertyHub/Admin/Lookups';
import DocumentHub from '../pages/PropertyHub/Admin/DocumentHub';
import JournalLogsList from '../pages/PropertyHub/JournalLogs/List';
import JournalLogForm from '../pages/PropertyHub/JournalLogs/Form';
import JournalLogDetail from '../pages/PropertyHub/JournalLogs/Detail';
import ContactLogsList from '../pages/PropertyHub/ContactLogs/List';
import ContactLogForm from '../pages/PropertyHub/ContactLogs/Form';
import ContactLogDetail from '../pages/PropertyHub/ContactLogs/Detail';
import RemindersList from '../pages/PropertyHub/Reminders/List';
import ReminderForm from '../pages/PropertyHub/Reminders/Form';
import ReminderDetail from '../pages/PropertyHub/Reminders/Detail';
import RemindersCalendar from '../pages/PropertyHub/Reminders/Calendar';
import MaintenanceList from '../pages/PropertyHub/Maintenance/List';
import MaintenanceForm from '../pages/PropertyHub/Maintenance/Form';
import MaintenanceDetail from '../pages/PropertyHub/Maintenance/Detail';
import PropertyDashboard from '../pages/PropertyHub/PropertyDashboard';
import TenantsList from '../pages/PropertyHub/Tenants/List';

const AccessDenied: React.FC = () => {
  useEffect(() => {
    localStorage.removeItem('token');
    localStorage.removeItem('mustChangePassword');
    window.dispatchEvent(new Event('itss-auth-changed'));
  }, []);
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-slate-100 px-4">
      <div className="max-w-md rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        <h1 className="text-lg font-semibold text-slate-900">No workspace access</h1>
        <p className="mt-2 text-sm text-slate-600">
          Your account is signed in but is not assigned to Property Hub and is not a Global Admin. Contact an
          administrator.
        </p>
        <a href="/Login" className="mt-6 inline-block text-sm font-semibold text-indigo-600 hover:text-indigo-500">
          Back to sign in
        </a>
      </div>
    </div>
  );
};

const loadingScreen = (
  <div className="flex min-h-screen items-center justify-center bg-slate-50 text-sm font-medium text-slate-600">
    Loading…
  </div>
);

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const token = localStorage.getItem('token');
  const mustChangePassword = localStorage.getItem('mustChangePassword') === 'true';
  const location = useLocation();

  if (!token) {
    return <Navigate to="/Login" replace />;
  }

  if (mustChangePassword && location.pathname !== '/ChangePassword') {
    return <Navigate to="/ChangePassword" replace />;
  }

  return <>{children}</>;
};

/** Global Admin site: Global Admins only. */
const GlobalAdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useAuthAccess();

  if (loading) {
    return loadingScreen;
  }

  if (!user?.isGlobalAdmin) {
    return <Navigate to="/Property Hub/Home" replace />;
  }

  return <>{children}</>;
};

/** Property Hub shell: Global Admin or any Property Hub workstream assignment. */
const PropertyHubAccessGate: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useAuthAccess();

  if (loading) {
    return loadingScreen;
  }

  if (user && (user.isGlobalAdmin || hasPropertyHubWorkstreamAccess(user))) {
    return <>{children}</>;
  }

  return <Navigate to="/Login" replace />;
};

/** Property Hub Admin section: Global Admin or Property Hub workstream admin (permission type Admin). */
const PropertyHubAdminGate: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useAuthAccess();

  if (loading) {
    return loadingScreen;
  }

  if (user && isPropertyHubWorkstreamAdmin(user)) {
    return <>{children}</>;
  }

  return <Navigate to="/Property Hub/Home" replace />;
};

const RootRedirect: React.FC = () => {
  const token = localStorage.getItem('token');
  const { user, loading } = useAuthAccess();

  if (!token) {
    return <Navigate to="/Login" replace />;
  }

  if (loading) {
    return loadingScreen;
  }

  if (!user) {
    return <Navigate to="/Login" replace />;
  }

  return <Navigate to={getRootPathForAuthenticatedUser(user)} replace />;
};

const AppRoutes: React.FC = () => {
  return (
    <Routes>
      <Route path="/Login" element={<Login />} />
      <Route path="/AccessDenied" element={<AccessDenied />} />
      <Route path="/ForgotPassword" element={<ForgotPassword />} />
      <Route path="/ResetPassword" element={<ResetPassword />} />
      <Route
        path="/ChangePassword"
        element={
          <ProtectedRoute>
            <ChangePassword />
          </ProtectedRoute>
        }
      />

      <Route
        path="/Admin"
        element={
          <ProtectedRoute>
            <GlobalAdminRoute>
              <Admin />
            </GlobalAdminRoute>
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/Admin/Users" replace />} />
        <Route path="Users" element={<Users />} />
        <Route path="Roles" element={<Roles />} />
        <Route path="Workstreams" element={<Workstreams />} />
        <Route path="Permissions" element={<Permissions />} />
        <Route path="Permission Guide" element={<PermissionGuide />} />
      </Route>

      <Route
        path="/Property Hub"
        element={
          <ProtectedRoute>
            <PropertyHubAccessGate>
              <PropertyHubLayout />
            </PropertyHubAccessGate>
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/Property Hub/Home" replace />} />
        <Route path="Home" element={<PropertyHubHome />} />
        <Route path="Property/:propertyId" element={<PropertyDashboard />} />

        <Route
          path="Admin"
          element={
            <PropertyHubAdminGate>
              <PropertyHubAdmin />
            </PropertyHubAdminGate>
          }
        >
          <Route index element={<Navigate to="/Property Hub/Admin/Property Groups" replace />} />
          <Route path="Property Groups" element={<PropertyGroups />} />
          <Route path="Properties" element={<Properties />} />
          <Route path="Tenancies" element={<Tenancies />} />
          <Route path="Lookups" element={<Lookups />} />
          <Route path="Document Hub" element={<DocumentHub />} />
        </Route>

        <Route path="Journal Logs" element={<JournalLogsList />} />
        <Route path="Journal Logs/New" element={<JournalLogForm />} />
        <Route path="Journal Logs/:id" element={<JournalLogDetail />} />

        <Route path="Contact Logs" element={<ContactLogsList />} />
        <Route path="Contact Logs/New" element={<ContactLogForm />} />
        <Route path="Contact Logs/:id" element={<ContactLogDetail />} />

        <Route path="Reminders" element={<RemindersList />} />
        <Route path="Reminders/Calendar" element={<RemindersCalendar />} />
        <Route path="Reminders/New" element={<ReminderForm />} />
        <Route path="Reminders/:id" element={<ReminderDetail />} />

        <Route path="Maintenance" element={<MaintenanceList />} />
        <Route path="Maintenance/New" element={<MaintenanceForm />} />
        <Route path="Maintenance/:id" element={<MaintenanceDetail />} />

        <Route path="Tenants" element={<TenantsList />} />
      </Route>

      <Route path="/" element={<RootRedirect />} />
    </Routes>
  );
};

export default AppRoutes;
