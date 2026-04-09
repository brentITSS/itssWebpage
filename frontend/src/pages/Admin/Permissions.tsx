import React, { useState, useEffect, useMemo } from 'react';
import { adminService, WorkstreamResponseDto, UserResponseDto, PermissionTypeDto, AssignWorkstreamUserRequest } from '../../services/adminService';
import { propertyService, PropertyGroupResponseDto } from '../../services/propertyService';

const selectClass =
  'mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 focus:border-slate-500 focus:outline-none';

const Permissions: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'workstream' | 'propertyGroup'>('workstream');

  const [workstreams, setWorkstreams] = useState<WorkstreamResponseDto[]>([]);
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [permissionTypes, setPermissionTypes] = useState<PermissionTypeDto[]>([]);
  const [selectedWorkstream, setSelectedWorkstream] = useState<number | null>(null);
  const [workstreamUsers, setWorkstreamUsers] = useState<UserResponseDto[]>([]);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [wsUserSearch, setWsUserSearch] = useState('');

  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [selectedPropertyGroup, setSelectedPropertyGroup] = useState<number | null>(null);
  const [propertyGroupUsers, setPropertyGroupUsers] = useState<UserResponseDto[]>([]);
  const [showPropertyGroupAssignModal, setShowPropertyGroupAssignModal] = useState(false);
  const [pgUserSearch, setPgUserSearch] = useState('');

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    if (selectedWorkstream) {
      loadWorkstreamUsers(selectedWorkstream);
    }
  }, [selectedWorkstream]);

  useEffect(() => {
    if (selectedPropertyGroup) {
      loadPropertyGroupUsers(selectedPropertyGroup);
    }
  }, [selectedPropertyGroup]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [workstreamsData, usersData, permissionTypesData, propertyGroupsData] = await Promise.all([
        adminService.getWorkstreams(),
        adminService.getUsers(),
        adminService.getPermissionTypes(),
        propertyService.getPropertyGroups(),
      ]);
      setWorkstreams(workstreamsData);
      setUsers(usersData);
      setPermissionTypes(permissionTypesData);
      setPropertyGroups(propertyGroupsData);
      if (workstreamsData.length > 0) {
        setSelectedWorkstream(workstreamsData[0].workstreamId);
      }
      if (propertyGroupsData.length > 0) {
        setSelectedPropertyGroup(propertyGroupsData[0].propertyGroupId);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const loadWorkstreamUsers = async (workstreamId: number) => {
    try {
      const data = await adminService.getWorkstreamUsers(workstreamId);
      setWorkstreamUsers(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load workstream users');
    }
  };

  const handleAssignUser = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedWorkstream) return;

    const formData = new FormData(e.currentTarget);
    const request: AssignWorkstreamUserRequest = {
      userId: parseInt(formData.get('userId') as string),
      permissionTypeId: parseInt(formData.get('permissionTypeId') as string),
    };

    try {
      await adminService.assignUserToWorkstream(selectedWorkstream, request);
      setShowAssignModal(false);
      loadWorkstreamUsers(selectedWorkstream);
    } catch (err: any) {
      setError(err.message || 'Failed to assign user to workstream');
    }
  };

  const handleRemoveUser = async (userId: number) => {
    if (!selectedWorkstream) return;
    if (!window.confirm('Are you sure you want to remove this user from the workstream?')) return;

    try {
      await adminService.removeUserFromWorkstream(selectedWorkstream, userId);
      loadWorkstreamUsers(selectedWorkstream);
    } catch (err: any) {
      setError(err.message || 'Failed to remove user from workstream');
    }
  };

  const loadPropertyGroupUsers = async (propertyGroupId: number) => {
    try {
      const data = await propertyService.getPropertyGroupUsers(propertyGroupId);
      setPropertyGroupUsers(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load property group users');
    }
  };

  const handleAssignPropertyGroupUser = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedPropertyGroup) return;

    const formData = new FormData(e.currentTarget);
    const userId = parseInt(formData.get('userId') as string);

    try {
      await propertyService.assignUserToPropertyGroup(selectedPropertyGroup, userId);
      setShowPropertyGroupAssignModal(false);
      loadPropertyGroupUsers(selectedPropertyGroup);
    } catch (err: any) {
      setError(err.message || 'Failed to assign user to property group');
    }
  };

  const handleRemovePropertyGroupUser = async (userId: number) => {
    if (!selectedPropertyGroup) return;
    if (!window.confirm('Are you sure you want to remove this user from the property group?')) return;

    try {
      await propertyService.removeUserFromPropertyGroup(selectedPropertyGroup, userId);
      loadPropertyGroupUsers(selectedPropertyGroup);
    } catch (err: any) {
      setError(err.message || 'Failed to remove user from property group');
    }
  };

  const selectedWorkstreamData = workstreams.find((w) => w.workstreamId === selectedWorkstream);
  const selectedPropertyGroupData = propertyGroups.find((pg) => pg.propertyGroupId === selectedPropertyGroup);

  const filteredWorkstreamUsers = useMemo(() => {
    const q = wsUserSearch.trim().toLowerCase();
    if (!q) return workstreamUsers;
    return workstreamUsers.filter((user) => {
      const name = `${user.firstName || ''} ${user.lastName || ''}`.trim().toLowerCase();
      return user.email.toLowerCase().includes(q) || name.includes(q);
    });
  }, [workstreamUsers, wsUserSearch]);

  const filteredPropertyGroupUsers = useMemo(() => {
    const q = pgUserSearch.trim().toLowerCase();
    if (!q) return propertyGroupUsers;
    return propertyGroupUsers.filter((user) => {
      const name = `${user.firstName || ''} ${user.lastName || ''}`.trim().toLowerCase();
      return user.email.toLowerCase().includes(q) || name.includes(q);
    });
  }, [propertyGroupUsers, pgUserSearch]);

  if (loading) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm font-medium text-slate-600">
        Loading permissions...
      </div>
    );
  }

  const tabBtn = (id: 'workstream' | 'propertyGroup', label: string) => (
    <button
      type="button"
      onClick={() => setActiveTab(id)}
      className={`rounded-md px-3 py-2 text-xs font-semibold uppercase tracking-wide transition ${
        activeTab === id ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
      }`}
    >
      {label}
    </button>
  );

  return (
    <div className="space-y-6">
      <section className="rounded-xl border border-slate-200 bg-white p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Permissions</h2>
            <p className="mt-1 text-sm text-slate-500">
              Assign users to workstreams with permission types, or grant property group access.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            {tabBtn('workstream', 'Workstream')}
            {tabBtn('propertyGroup', 'Property groups')}
          </div>
        </div>
      </section>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
          {error}
        </div>
      )}

      {activeTab === 'workstream' && (
        <div className="grid gap-6 xl:grid-cols-[280px_minmax(0,1fr)]">
          <aside className="space-y-4">
            <div className="rounded-xl border border-slate-200 bg-white p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Workstream</p>
              <select
                value={selectedWorkstream || ''}
                onChange={(e) => setSelectedWorkstream(parseInt(e.target.value, 10) || null)}
                className={selectClass}
              >
                <option value="">Select a workstream</option>
                {workstreams.map((ws) => (
                  <option key={ws.workstreamId} value={ws.workstreamId}>
                    {ws.workstreamName}
                  </option>
                ))}
              </select>
            </div>

            <div className="rounded-xl border border-slate-200 bg-white p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Search assignees</p>
              <input
                type="text"
                value={wsUserSearch}
                onChange={(e) => setWsUserSearch(e.target.value)}
                placeholder="Email or name"
                className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:border-slate-500 focus:outline-none"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-xl border border-slate-200 bg-white p-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Workstreams</p>
                <p className="mt-1 text-xl font-semibold text-slate-900">{workstreams.length}</p>
              </div>
              <div className="rounded-xl border border-slate-200 bg-white p-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">On this WS</p>
                <p className="mt-1 text-xl font-semibold text-slate-900">{workstreamUsers.length}</p>
              </div>
              <div className="col-span-2 rounded-xl border border-slate-200 bg-white p-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Shown</p>
                <p className="mt-1 text-xl font-semibold text-slate-900">{filteredWorkstreamUsers.length}</p>
              </div>
            </div>

            {selectedWorkstream && (
              <button
                type="button"
                onClick={() => setShowAssignModal(true)}
                className="w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-black"
              >
                Assign user
              </button>
            )}
          </aside>

          <section className="space-y-3">
            {!selectedWorkstream ? (
              <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
                Select a workstream to view assignments.
              </div>
            ) : (
              <>
                <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Current workstream</p>
                  <p className="mt-1 text-base font-semibold text-slate-900">{selectedWorkstreamData?.workstreamName}</p>
                </div>

                {filteredWorkstreamUsers.length === 0 ? (
                  <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
                    {workstreamUsers.length === 0
                      ? 'No users assigned to this workstream.'
                      : 'No assignees match this search.'}
                  </div>
                ) : (
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                    {filteredWorkstreamUsers.map((user) => {
                      const access = user.workstreamAccess.find((wa) => wa.workstreamId === selectedWorkstream);
                      return (
                        <article
                          key={user.userId}
                          className="rounded-xl border border-slate-200 bg-white p-4 transition hover:border-slate-300 hover:shadow-sm"
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div className="min-w-0">
                              <h3 className="truncate text-base font-semibold text-slate-900">{user.email}</h3>
                              <p className="text-sm text-slate-500">
                                {[user.firstName, user.lastName].filter(Boolean).join(' ') || '—'}
                              </p>
                            </div>
                          </div>
                          <div className="mt-3 text-sm text-slate-600">
                            <span className="mr-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                              Permission
                            </span>
                            {access?.permissionTypeName || '—'}
                          </div>
                          <div className="mt-4 flex flex-wrap gap-2">
                            <button
                              type="button"
                              onClick={() => handleRemoveUser(user.userId)}
                              className="rounded-md border border-rose-200 bg-rose-50 px-2.5 py-1.5 text-xs font-semibold text-rose-700 transition hover:bg-rose-100"
                            >
                              Remove
                            </button>
                          </div>
                        </article>
                      );
                    })}
                  </div>
                )}
              </>
            )}

            {showAssignModal && selectedWorkstream && (
              <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
                <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
                  <h3 className="mb-4 text-lg font-semibold text-slate-900">Assign user to workstream</h3>
                  <form onSubmit={handleAssignUser}>
                    <div className="mb-4">
                      <label className="mb-1 block text-sm font-medium text-slate-700">User *</label>
                      <select name="userId" required className="field">
                        <option value="">Select user</option>
                        {users
                          .filter((u) => !workstreamUsers.some((wu) => wu.userId === u.userId))
                          .map((user) => (
                            <option key={user.userId} value={user.userId}>
                              {user.email} — {user.firstName} {user.lastName}
                            </option>
                          ))}
                      </select>
                    </div>
                    <div className="mb-4">
                      <label className="mb-1 block text-sm font-medium text-slate-700">Permission type *</label>
                      <select name="permissionTypeId" required className="field">
                        <option value="">Select permission type</option>
                        {permissionTypes.map((pt) => (
                          <option key={pt.permissionTypeId} value={pt.permissionTypeId}>
                            {pt.permissionTypeName}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="flex justify-end gap-2">
                      <button type="button" onClick={() => setShowAssignModal(false)} className="btn-secondary">
                        Cancel
                      </button>
                      <button type="submit" className="btn-primary">
                        Assign
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            )}
          </section>
        </div>
      )}

      {activeTab === 'propertyGroup' && (
        <div className="grid gap-6 xl:grid-cols-[280px_minmax(0,1fr)]">
          <aside className="space-y-4">
            <div className="rounded-xl border border-slate-200 bg-white p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Property group</p>
              <select
                value={selectedPropertyGroup || ''}
                onChange={(e) => setSelectedPropertyGroup(parseInt(e.target.value, 10) || null)}
                className={selectClass}
              >
                <option value="">Select a property group</option>
                {propertyGroups.map((pg) => (
                  <option key={pg.propertyGroupId} value={pg.propertyGroupId}>
                    {pg.propertyGroupName}
                  </option>
                ))}
              </select>
            </div>

            <div className="rounded-xl border border-slate-200 bg-white p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Search users</p>
              <input
                type="text"
                value={pgUserSearch}
                onChange={(e) => setPgUserSearch(e.target.value)}
                placeholder="Email or name"
                className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:border-slate-500 focus:outline-none"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-xl border border-slate-200 bg-white p-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Groups</p>
                <p className="mt-1 text-xl font-semibold text-slate-900">{propertyGroups.length}</p>
              </div>
              <div className="rounded-xl border border-slate-200 bg-white p-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">In group</p>
                <p className="mt-1 text-xl font-semibold text-slate-900">{propertyGroupUsers.length}</p>
              </div>
              <div className="col-span-2 rounded-xl border border-slate-200 bg-white p-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Shown</p>
                <p className="mt-1 text-xl font-semibold text-slate-900">{filteredPropertyGroupUsers.length}</p>
              </div>
            </div>

            {selectedPropertyGroup && (
              <button
                type="button"
                onClick={() => setShowPropertyGroupAssignModal(true)}
                className="w-full rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-black"
              >
                Assign user
              </button>
            )}
          </aside>

          <section className="space-y-3">
            {!selectedPropertyGroup ? (
              <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
                Select a property group to view access.
              </div>
            ) : (
              <>
                <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Current group</p>
                  <p className="mt-1 text-base font-semibold text-slate-900">{selectedPropertyGroupData?.propertyGroupName}</p>
                </div>

                {filteredPropertyGroupUsers.length === 0 ? (
                  <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
                    {propertyGroupUsers.length === 0
                      ? 'No users in this property group.'
                      : 'No users match this search.'}
                  </div>
                ) : (
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                    {filteredPropertyGroupUsers.map((user) => (
                      <article
                        key={user.userId}
                        className="rounded-xl border border-slate-200 bg-white p-4 transition hover:border-slate-300 hover:shadow-sm"
                      >
                        <div className="min-w-0">
                          <h3 className="truncate text-base font-semibold text-slate-900">{user.email}</h3>
                          <p className="text-sm text-slate-500">
                            {[user.firstName, user.lastName].filter(Boolean).join(' ') || '—'}
                          </p>
                        </div>
                        <div className="mt-4 flex flex-wrap gap-2">
                          <button
                            type="button"
                            onClick={() => handleRemovePropertyGroupUser(user.userId)}
                            className="rounded-md border border-rose-200 bg-rose-50 px-2.5 py-1.5 text-xs font-semibold text-rose-700 transition hover:bg-rose-100"
                          >
                            Remove
                          </button>
                        </div>
                      </article>
                    ))}
                  </div>
                )}
              </>
            )}

            {showPropertyGroupAssignModal && selectedPropertyGroup && (
              <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
                <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
                  <h3 className="mb-4 text-lg font-semibold text-slate-900">Assign user to property group</h3>
                  <form onSubmit={handleAssignPropertyGroupUser}>
                    <div className="mb-4">
                      <label className="mb-1 block text-sm font-medium text-slate-700">User *</label>
                      <select name="userId" required className="field">
                        <option value="">Select user</option>
                        {users
                          .filter((u) => !propertyGroupUsers.some((pgu) => pgu.userId === u.userId))
                          .map((user) => (
                            <option key={user.userId} value={user.userId}>
                              {user.email} — {user.firstName} {user.lastName}
                            </option>
                          ))}
                      </select>
                    </div>
                    <div className="flex justify-end gap-2">
                      <button type="button" onClick={() => setShowPropertyGroupAssignModal(false)} className="btn-secondary">
                        Cancel
                      </button>
                      <button type="submit" className="btn-primary">
                        Assign
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
};

export default Permissions;
