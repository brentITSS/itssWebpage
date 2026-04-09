import React, { useState, useEffect, useMemo } from 'react';
import { adminService, RoleResponseDto, CreateRoleRequest, UpdateRoleRequest, RoleTypeDto } from '../../services/adminService';

const Roles: React.FC = () => {
  const [roles, setRoles] = useState<RoleResponseDto[]>([]);
  const [roleTypes, setRoleTypes] = useState<RoleTypeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleResponseDto | null>(null);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<number | 'all'>('all');

  useEffect(() => {
    loadRoles();
    loadRoleTypes();
  }, []);

  const loadRoles = async () => {
    try {
      setLoading(true);
      const data = await adminService.getRoles();
      setRoles(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load roles');
    } finally {
      setLoading(false);
    }
  };

  const loadRoleTypes = async () => {
    try {
      const data = await adminService.getRoleTypes();
      setRoleTypes(data);
    } catch (err) {
      console.error('Failed to load role types', err);
    }
  };

  const handleCreateRole = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateRoleRequest = {
      roleName: formData.get('roleName') as string,
      roleTypeId: parseInt(formData.get('roleTypeId') as string),
    };

    try {
      await adminService.createRole(request);
      setShowModal(false);
      loadRoles();
    } catch (err: any) {
      setError(err.message || 'Failed to create role');
    }
  };

  const handleUpdateRole = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateRoleRequest = {
      roleName: (formData.get('roleName') as string) || undefined,
      roleTypeId: formData.get('roleTypeId') ? parseInt(formData.get('roleTypeId') as string) : undefined,
    };

    try {
      await adminService.updateRole(id, request);
      setEditingRole(null);
      loadRoles();
    } catch (err: any) {
      setError(err.message || 'Failed to update role');
    }
  };

  const filteredRoles = useMemo(() => {
    return roles.filter((role) => {
      if (typeFilter !== 'all' && role.roleTypeId !== typeFilter) return false;
      const q = search.trim().toLowerCase();
      if (!q) return true;
      const typeName = (role.roleTypeName || '').toLowerCase();
      return role.roleName.toLowerCase().includes(q) || typeName.includes(q);
    });
  }, [roles, search, typeFilter]);

  if (loading) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm font-medium text-slate-600">
        Loading roles...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <section className="rounded-xl border border-slate-200 bg-white p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Role Directory</h2>
            <p className="mt-1 text-sm text-slate-500">Define role names and map them to role types.</p>
          </div>
          <button
            type="button"
            onClick={() => setShowModal(true)}
            className="inline-flex items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-black"
          >
            Create Role
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
              placeholder="Role or type name"
              className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:border-slate-500 focus:outline-none"
            />
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Role type</p>
            <div className="mt-2 flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setTypeFilter('all')}
                className={`rounded-md px-3 py-2 text-xs font-semibold uppercase tracking-wide transition ${
                  typeFilter === 'all' ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                }`}
              >
                All
              </button>
              {roleTypes.map((rt) => (
                <button
                  key={rt.roleTypeId}
                  type="button"
                  onClick={() => setTypeFilter(rt.roleTypeId)}
                  className={`rounded-md px-3 py-2 text-xs font-semibold uppercase tracking-wide transition ${
                    typeFilter === rt.roleTypeId ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {rt.roleTypeName}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Total</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{roles.length}</p>
            </div>
            <div className="rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Types</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{roleTypes.length}</p>
            </div>
            <div className="col-span-2 rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Shown</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{filteredRoles.length}</p>
            </div>
          </div>
        </aside>

        <section className="space-y-3">
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
              {error}
            </div>
          )}

          {filteredRoles.length === 0 ? (
            <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
              No roles match this filter.
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              {filteredRoles.map((role) => (
                <article
                  key={role.roleId}
                  className="rounded-xl border border-slate-200 bg-white p-4 transition hover:border-slate-300 hover:shadow-sm"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-base font-semibold text-slate-900">{role.roleName}</h3>
                      <p className="text-sm text-slate-500">{role.roleTypeName || 'Unknown type'}</p>
                    </div>
                  </div>
                  <div className="mt-3 text-sm text-slate-600">
                    <span className="mr-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Created</span>
                    {new Date(role.createdDate).toLocaleDateString()}
                  </div>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <button
                      type="button"
                      onClick={() => setEditingRole(role)}
                      className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 transition hover:border-slate-300"
                    >
                      Edit
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </div>

      {showModal && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Create Role</h3>
            <form onSubmit={handleCreateRole}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Role Name *</label>
                <input type="text" name="roleName" required className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Role Type *</label>
                <select name="roleTypeId" required className="field">
                  <option value="">Select Role Type</option>
                  {roleTypes.map((rt) => (
                    <option key={rt.roleTypeId} value={rt.roleTypeId}>
                      {rt.roleTypeName}
                    </option>
                  ))}
                </select>
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

      {editingRole && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Edit Role</h3>
            <form onSubmit={(e) => handleUpdateRole(editingRole.roleId, e)}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Role Name</label>
                <input type="text" name="roleName" defaultValue={editingRole.roleName} className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Role Type</label>
                <select name="roleTypeId" defaultValue={editingRole.roleTypeId} className="field">
                  {roleTypes.map((rt) => (
                    <option key={rt.roleTypeId} value={rt.roleTypeId}>
                      {rt.roleTypeName}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => setEditingRole(null)} className="btn-secondary">
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
    </div>
  );
};

export default Roles;
