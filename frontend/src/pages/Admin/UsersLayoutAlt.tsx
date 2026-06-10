import React, { useEffect, useState } from 'react';
import {
  adminService,
  CreateUserRequest,
  ResetPasswordRequest,
  RoleResponseDto,
  UpdateUserRequest,
  UserResponseDto,
} from '../../services/adminService';

const UsersLayoutAlt: React.FC = () => {
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingUser, setEditingUser] = useState<UserResponseDto | null>(null);
  const [showResetPassword, setShowResetPassword] = useState<number | null>(null);
  const [generatedTempPassword, setGeneratedTempPassword] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [roles, setRoles] = useState<RoleResponseDto[]>([]);

  useEffect(() => {
    loadUsers();
    loadRoles();
  }, []);

  const loadRoles = async () => {
    try {
      const data = await adminService.getRoles();
      setRoles(data);
    } catch (err) {
      console.error('Failed to load roles', err);
    }
  };

  const parseRoleTypeIds = (formData: FormData): number[] => {
    const selectedRoleIds = formData
      .getAll('roleIds')
      .map((value) => parseInt(value as string, 10));
    const roleTypeIds = selectedRoleIds
      .map((roleId) => roles.find((role) => role.roleId === roleId)?.roleTypeId)
      .filter((id): id is number => id !== undefined);
    return [...new Set(roleTypeIds)];
  };

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
      firstName: (formData.get('firstName') as string) || undefined,
      lastName: (formData.get('lastName') as string) || undefined,
      defaultLoginLandingPage: (formData.get('defaultLoginLandingPage') as string) || undefined,
      roleIds: parseRoleTypeIds(formData),
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
      firstName: (formData.get('firstName') as string) || undefined,
      lastName: (formData.get('lastName') as string) || undefined,
      isActive: formData.get('isActive') === 'true',
      defaultLoginLandingPage:
        defaultLandingPageValue && defaultLandingPageValue.trim() !== ''
          ? defaultLandingPageValue.trim()
          : undefined,
      roleIds: parseRoleTypeIds(formData),
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

  const handleResetPassword = async (id: number) => {
    const request: ResetPasswordRequest = {
      generateTemporaryPassword: true,
      requirePasswordChange: true,
    };

    try {
      const response = await adminService.resetPassword(id, request);
      setGeneratedTempPassword(response.temporaryPassword || null);
      loadUsers();
    } catch (err: any) {
      setError(err.message || 'Failed to reset password');
    }
  };

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

  if (loading) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm font-medium text-slate-600">
        Loading users...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <section className="rounded-xl border border-slate-200 bg-white p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">User Directory</h2>
            <p className="mt-1 text-sm text-slate-500">
              Explore and manage users in cards rather than a table layout.
            </p>
          </div>
          <button
            onClick={() => setShowModal(true)}
            className="inline-flex items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-black"
          >
            Create User
          </button>
        </div>
      </section>

      <div className="grid gap-6 xl:grid-cols-[280px_minmax(0,1fr)]">
        <aside className="space-y-4">
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Search</p>
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Email or name"
              className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:border-slate-500 focus:outline-none"
            />
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Status</p>
            <div className="mt-2 flex flex-wrap gap-2">
              {[
                { id: 'all', label: 'All' },
                { id: 'active', label: 'Active' },
                { id: 'inactive', label: 'Inactive' },
              ].map((filter) => (
                <button
                  key={filter.id}
                  type="button"
                  onClick={() => setStatusFilter(filter.id as 'all' | 'active' | 'inactive')}
                  className={`rounded-md px-3 py-2 text-xs font-semibold uppercase tracking-wide transition ${
                    statusFilter === filter.id
                      ? 'bg-slate-900 text-white'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {filter.label}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Total</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{users.length}</p>
            </div>
            <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-emerald-700">Active</p>
              <p className="mt-1 text-xl font-semibold text-emerald-800">{activeUsers}</p>
            </div>
            <div className="rounded-xl border border-rose-200 bg-rose-50 p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-rose-700">Inactive</p>
              <p className="mt-1 text-xl font-semibold text-rose-800">{inactiveUsers}</p>
            </div>
            <div className="rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Shown</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{filteredUsers.length}</p>
            </div>
          </div>
        </aside>

        <section className="space-y-3">
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
              {error}
            </div>
          )}

          {filteredUsers.length === 0 ? (
            <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
              No users match this filter.
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              {filteredUsers.map((user) => {
                const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
                return (
                  <article
                    key={user.userId}
                    className="rounded-xl border border-slate-200 bg-white p-4 transition hover:border-slate-300 hover:shadow-sm"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <h3 className="text-base font-semibold text-slate-900">
                          {fullName || user.email}
                        </h3>
                        <p className="text-sm text-slate-500">{user.email}</p>
                      </div>
                      <span
                        className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${
                          user.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                        }`}
                      >
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </div>

                    <div className="mt-3 space-y-2 text-sm text-slate-600">
                      <div>
                        <span className="mr-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                          Roles
                        </span>
                        {user.roles && user.roles.length > 0
                          ? user.roles.map((role) => role.roleTypeName || role.roleName).join(', ')
                          : 'None'}
                      </div>
                      <div>
                        <span className="mr-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                          Default Route
                        </span>
                        {user.defaultLoginLandingPage || 'Not set'}
                      </div>
                    </div>

                    <div className="mt-4 flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => setEditingUser(user)}
                        className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 transition hover:border-slate-300"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          setShowResetPassword(user.userId);
                          setGeneratedTempPassword(null);
                        }}
                        className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 transition hover:border-slate-300"
                      >
                        Reset Password
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDeleteUser(user.userId)}
                        className="rounded-md border border-rose-200 bg-rose-50 px-2.5 py-1.5 text-xs font-semibold text-rose-700 transition hover:bg-rose-100"
                      >
                        Deactivate
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </div>

      {/* Create User Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Create User</h3>
            <form onSubmit={handleCreateUser}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Email *</label>
                <input type="email" name="email" required className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Password *</label>
                <input type="password" name="password" required className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">First Name</label>
                <input type="text" name="firstName" className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Last Name</label>
                <input type="text" name="lastName" className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Default Landing Page</label>
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
              <div className="mb-4">
                <label className="mb-2 block text-sm font-medium text-slate-700">Roles</label>
                {roles.length === 0 ? (
                  <p className="text-sm text-slate-500">No roles available.</p>
                ) : (
                  <div className="space-y-2 rounded-lg border border-slate-200 p-3">
                    {roles.map((role) => (
                      <label key={role.roleId} className="flex items-start gap-2 text-sm text-slate-700">
                        <input
                          type="checkbox"
                          name="roleIds"
                          value={role.roleId}
                          className="mt-0.5 rounded border-slate-300"
                        />
                        <span>
                          <span className="font-medium">{role.roleName}</span>
                          {role.roleTypeName && role.roleTypeName !== role.roleName ? (
                            <span className="mt-0.5 block text-xs text-slate-500">{role.roleTypeName}</span>
                          ) : null}
                        </span>
                      </label>
                    ))}
                  </div>
                )}
              </div>
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
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
                <label className="mb-1 block text-sm font-medium text-slate-700">Email</label>
                <input
                  type="email"
                  value={editingUser.email}
                  disabled
                  className="field cursor-not-allowed bg-slate-100"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">First Name</label>
                <input type="text" name="firstName" defaultValue={editingUser.firstName || ''} className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Last Name</label>
                <input type="text" name="lastName" defaultValue={editingUser.lastName || ''} className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Status</label>
                <select name="isActive" defaultValue={editingUser.isActive ? 'true' : 'false'} className="field">
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Must Change Password</label>
                <div className="field flex items-center bg-slate-100 text-slate-700">
                  {editingUser.mustChangePassword ? 'Yes' : 'No'}
                </div>
                <p className="mt-1 text-xs text-slate-500">
                  This flag is set automatically when a temporary password is generated.
                </p>
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Default Landing Page</label>
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
              <div className="mb-4">
                <label className="mb-2 block text-sm font-medium text-slate-700">Roles</label>
                {roles.length === 0 ? (
                  <p className="text-sm text-slate-500">No roles available.</p>
                ) : (
                  <div className="space-y-2 rounded-lg border border-slate-200 p-3">
                    {roles.map((role) => {
                      const assignedRoleTypeIds = editingUser.roles.map((userRole) => userRole.roleId);
                      return (
                        <label key={role.roleId} className="flex items-start gap-2 text-sm text-slate-700">
                          <input
                            type="checkbox"
                            name="roleIds"
                            value={role.roleId}
                            defaultChecked={assignedRoleTypeIds.includes(role.roleTypeId)}
                            className="mt-0.5 rounded border-slate-300"
                          />
                          <span>
                            <span className="font-medium">{role.roleName}</span>
                            {role.roleTypeName && role.roleTypeName !== role.roleName ? (
                              <span className="mt-0.5 block text-xs text-slate-500">{role.roleTypeName}</span>
                            ) : null}
                          </span>
                        </label>
                      );
                    })}
                  </div>
                )}
              </div>
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => setEditingUser(null)} className="btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
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
            <h3 className="mb-2 text-lg font-semibold text-slate-900">Generate Temporary Password</h3>
            <p className="mb-4 text-sm text-slate-600">
              This creates a one-time temporary password and forces the user to set a new password on next login.
            </p>
            {generatedTempPassword && (
              <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 p-3">
                <p className="text-xs font-semibold uppercase tracking-wide text-amber-700">Temporary password</p>
                <p className="mt-1 break-all font-mono text-sm font-semibold text-amber-900">
                  {generatedTempPassword}
                </p>
                <p className="mt-2 text-xs text-amber-800">
                  Copy and share this securely. It is only shown once.
                </p>
              </div>
            )}
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => {
                  setShowResetPassword(null);
                  setGeneratedTempPassword(null);
                }}
                className="btn-secondary"
              >
                Close
              </button>
              <button
                type="button"
                onClick={() => handleResetPassword(showResetPassword)}
                className="btn-primary"
              >
                Generate temp password
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UsersLayoutAlt;
