import React, { useEffect, useState } from 'react';
import {
  adminService,
  CreateUserRequest,
  ResetPasswordRequest,
  UpdateUserRequest,
  UserResponseDto,
} from '../../services/adminService';

const Users: React.FC = () => {
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingUser, setEditingUser] = useState<UserResponseDto | null>(null);
  const [showResetPassword, setShowResetPassword] = useState<number | null>(null);
  const [selectedUser, setSelectedUser] = useState<UserResponseDto | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const data = await adminService.getUsers();
      setUsers(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateUserRequest = {
      email: formData.get('email') as string,
      password: formData.get('password') as string,
      firstName: formData.get('firstName') as string || undefined,
      lastName: formData.get('lastName') as string || undefined,
      defaultLoginLandingPage: formData.get('defaultLoginLandingPage') as string || undefined,
      roleIds: [], // TODO: Add role selection UI
    };

    try {
      await adminService.createUser(request);
      setShowModal(false);
      loadUsers();
    } catch (err: any) {
      setError(err.message || 'Failed to create user');
    }
  };

  const handleUpdateUser = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!editingUser) return;

    const formData = new FormData(e.currentTarget);
    const defaultLandingPageValue = formData.get('defaultLoginLandingPage') as string | null;
    const request: UpdateUserRequest = {
      firstName: formData.get('firstName') as string || undefined,
      lastName: formData.get('lastName') as string || undefined,
      isActive: formData.get('isActive') === 'true',
      defaultLoginLandingPage: defaultLandingPageValue && defaultLandingPageValue.trim() !== '' 
        ? defaultLandingPageValue.trim() 
        : undefined,
      roleIds: [], // TODO: Add role selection UI
    };

    try {
      await adminService.updateUser(editingUser.userId, request);
      setEditingUser(null);
      loadUsers();
    } catch (err: any) {
      setError(err.message || 'Failed to update user');
    }
  };

  const handleDeleteUser = async (id: number) => {
    if (!window.confirm('Are you sure you want to deactivate this user?')) return;

    try {
      await adminService.updateUser(id, { isActive: false });
      loadUsers();
    } catch (err: any) {
      setError(err.message || 'Failed to deactivate user');
    }
  };

  const handleResetPassword = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: ResetPasswordRequest = {
      newPassword: formData.get('newPassword') as string,
    };

    try {
      await adminService.resetPassword(id, request);
      setShowResetPassword(null);
      alert('Password reset successfully');
    } catch (err: any) {
      setError(err.message || 'Failed to reset password');
    }
  };

  if (loading) {
    return (
      <div className="rounded-md border-2 border-black bg-white p-8 text-center text-sm font-bold text-black shadow-[4px_4px_0_#000]">
        Loading users...
      </div>
    );
  }

  const activeUsers = users.filter((user) => user.isActive).length;
  const inactiveUsers = users.length - activeUsers;
  const filteredUsers = users.filter((user) => {
    if (statusFilter === 'active' && !user.isActive) return false;
    if (statusFilter === 'inactive' && user.isActive) return false;
    const q = search.trim().toLowerCase();
    if (!q) return true;
    const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim().toLowerCase();
    return user.email.toLowerCase().includes(q) || fullName.includes(q);
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-3xl font-black tracking-tight text-black">Users</h2>
          <p className="mt-1 text-sm font-semibold text-black/70">
            Manage account access, default routes, and password resets.
          </p>
        </div>
        <button
          onClick={() => setShowModal(true)}
          className="inline-flex items-center justify-center rounded-md border-2 border-black bg-[#ffd60a] px-4 py-2.5 text-sm font-black uppercase tracking-wide text-black shadow-[3px_3px_0_#000] transition hover:translate-x-[1px] hover:translate-y-[1px] hover:shadow-none"
        >
          Create User
        </button>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <div className="rounded-md border-2 border-black bg-white p-4 shadow-[3px_3px_0_#000]">
          <p className="text-[11px] font-black uppercase tracking-wide text-black/70">Total Users</p>
          <p className="mt-2 text-2xl font-black text-black">{users.length}</p>
        </div>
        <div className="rounded-md border-2 border-black bg-[#caffbf] p-4 shadow-[3px_3px_0_#000]">
          <p className="text-[11px] font-black uppercase tracking-wide text-black/70">Active</p>
          <p className="mt-2 text-2xl font-black text-black">{activeUsers}</p>
        </div>
        <div className="rounded-md border-2 border-black bg-[#ffadad] p-4 shadow-[3px_3px_0_#000]">
          <p className="text-[11px] font-black uppercase tracking-wide text-black/70">Inactive</p>
          <p className="mt-2 text-2xl font-black text-black">{inactiveUsers}</p>
        </div>
        <div className="rounded-md border-2 border-black bg-[#9bf6ff] p-4 shadow-[3px_3px_0_#000]">
          <p className="text-[11px] font-black uppercase tracking-wide text-black/70">Shown</p>
          <p className="mt-2 text-2xl font-black text-black">{filteredUsers.length}</p>
        </div>
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
          {error}
        </div>
      )}

      <div className="rounded-md border-2 border-black bg-white p-4 shadow-[4px_4px_0_#000]">
        <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div className="relative w-full lg:max-w-md">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by email or name"
              className="w-full rounded-md border-2 border-black bg-white px-3 py-2.5 text-sm font-semibold text-black placeholder:text-black/40 focus:outline-none"
            />
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {[
              { id: 'all', label: 'All' },
              { id: 'active', label: 'Active' },
              { id: 'inactive', label: 'Inactive' },
            ].map((filter) => (
              <button
                key={filter.id}
                type="button"
                onClick={() => setStatusFilter(filter.id as 'all' | 'active' | 'inactive')}
                className={`rounded-md border-2 border-black px-3 py-2 text-xs font-black uppercase tracking-wide transition ${
                  statusFilter === filter.id
                    ? 'bg-black text-white'
                    : 'bg-white text-black hover:bg-[#f5f5f5]'
                }`}
              >
                {filter.label}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-hidden rounded-md border-2 border-black">
        <div className="overflow-x-auto">
        <table className="min-w-[980px] w-full divide-y divide-slate-200">
          <thead className="bg-[#ffe45e]">
            <tr>
              <th className="table-header-cell">
                Email
              </th>
              <th className="table-header-cell">
                Name
              </th>
              <th className="table-header-cell">
                Roles
              </th>
              <th className="table-header-cell">
                Status
              </th>
              <th className="table-header-cell">
                Default Landing Page
              </th>
              <th className="table-header-cell">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {filteredUsers.map((user) => (
              <tr key={user.userId} className="transition hover:bg-[#f7f7f7]">
                <td className="table-body-cell whitespace-nowrap font-medium text-slate-900">
                  {user.email}
                </td>
                <td className="table-body-cell whitespace-nowrap text-slate-600">
                  {user.firstName} {user.lastName}
                </td>
                <td className="table-body-cell whitespace-nowrap text-slate-600">
                  {user.roles && user.roles.length > 0
                    ? user.roles.map(role => role.roleTypeName || role.roleName).join(', ')
                    : 'None'}
                </td>
                <td className="table-body-cell whitespace-nowrap">
                  <span
                    className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${
                      user.isActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                    }`}
                  >
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-body-cell whitespace-nowrap text-slate-600">
                  {user.defaultLoginLandingPage || '-'}
                </td>
                <td className="table-body-cell min-w-[230px] whitespace-normal text-sm font-semibold">
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      onClick={() => setSelectedUser(user)}
                      className="rounded-md border border-slate-200 bg-white px-2 py-1 text-slate-700 transition hover:border-slate-300"
                    >
                      View
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingUser(user)}
                      className="rounded-md border border-slate-200 bg-white px-2 py-1 text-slate-700 transition hover:border-slate-300"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => setShowResetPassword(user.userId)}
                      className="rounded-md border border-slate-200 bg-white px-2 py-1 text-slate-700 transition hover:border-slate-300"
                    >
                      Reset Password
                    </button>
                    <button
                      onClick={() => handleDeleteUser(user.userId)}
                      className="rounded-md border border-rose-200 bg-rose-50 px-2 py-1 text-rose-700 transition hover:bg-rose-100"
                    >
                      Deactivate
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
        </div>
      </div>

      {selectedUser && (
        <div className="fixed inset-0 z-50 flex justify-end bg-slate-900/35 backdrop-blur-[1px]">
          <div className="h-full w-full max-w-md border-l border-slate-200 bg-white p-5 shadow-2xl">
            <div className="mb-4 flex items-start justify-between">
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">User Details</p>
                <h3 className="mt-1 text-lg font-semibold text-slate-900">
                  {selectedUser.firstName || selectedUser.lastName
                    ? `${selectedUser.firstName || ''} ${selectedUser.lastName || ''}`.trim()
                    : selectedUser.email}
                </h3>
              </div>
              <button
                type="button"
                onClick={() => setSelectedUser(null)}
                className="rounded-md border border-slate-200 px-2 py-1 text-sm text-slate-600 hover:bg-slate-50"
              >
                Close
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Email</p>
                <p className="mt-1 text-sm text-slate-700">{selectedUser.email}</p>
              </div>
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Default Route</p>
                <div className="mt-1 rounded-md bg-slate-50 px-3 py-2 text-sm text-slate-700">
                  {selectedUser.defaultLoginLandingPage || 'Not set'}
                </div>
              </div>
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Roles</p>
                <div className="mt-1 rounded-md bg-slate-50 px-3 py-2 text-sm text-slate-700">
                  {selectedUser.roles && selectedUser.roles.length > 0
                    ? selectedUser.roles.map((role) => role.roleTypeName || role.roleName).join(', ')
                    : 'None'}
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  setEditingUser(selectedUser);
                  setSelectedUser(null);
                }}
                className="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-semibold text-white transition hover:bg-black"
              >
                Edit User
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Create User Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Create User</h3>
            <form onSubmit={handleCreateUser}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Email *
                </label>
                <input
                  type="email"
                  name="email"
                  required
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Password *
                </label>
                <input
                  type="password"
                  name="password"
                  required
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  First Name
                </label>
                <input
                  type="text"
                  name="firstName"
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Last Name
                </label>
                <input
                  type="text"
                  name="lastName"
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Default Landing Page
                </label>
                <input
                  type="text"
                  name="defaultLoginLandingPage"
                  placeholder="e.g., /Admin, /Property Hub/Home"
                  className="field"
                />
                <p className="mt-1 text-xs text-slate-500">
                  Path to redirect user after login (e.g., /Admin, /Property Hub/Home)
                </p>
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="btn-secondary"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="btn-primary"
                >
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit User Modal */}
      {editingUser && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Edit User</h3>
            <form onSubmit={handleUpdateUser}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Email
                </label>
                <input
                  type="email"
                  value={editingUser.email}
                  disabled
                  className="field cursor-not-allowed bg-slate-100"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  First Name
                </label>
                <input
                  type="text"
                  name="firstName"
                  defaultValue={editingUser.firstName || ''}
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Last Name
                </label>
                <input
                  type="text"
                  name="lastName"
                  defaultValue={editingUser.lastName || ''}
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Status
                </label>
                <select
                  name="isActive"
                  defaultValue={editingUser.isActive ? 'true' : 'false'}
                  className="field"
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  Default Landing Page
                </label>
                <input
                  type="text"
                  name="defaultLoginLandingPage"
                  defaultValue={editingUser.defaultLoginLandingPage || ''}
                  placeholder="e.g., /Admin, /Property Hub/Home"
                  className="field"
                />
                <p className="mt-1 text-xs text-slate-500">
                  Path to redirect user after login (e.g., /Admin, /Property Hub/Home)
                </p>
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setEditingUser(null)}
                  className="btn-secondary"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="btn-primary"
                >
                  Update
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Reset Password Modal */}
      {showResetPassword && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Reset Password</h3>
            <form onSubmit={(e) => handleResetPassword(showResetPassword, e)}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">
                  New Password *
                </label>
                <input
                  type="password"
                  name="newPassword"
                  required
                  className="field"
                />
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setShowResetPassword(null)}
                  className="btn-secondary"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="btn-primary"
                >
                  Reset
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Users;
