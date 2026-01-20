import React, { useState, useEffect } from 'react';
import { adminService, WorkstreamResponseDto, UserResponseDto, PermissionTypeDto, AssignWorkstreamUserRequest } from '../../services/adminService';
import { propertyService, PropertyGroupResponseDto } from '../../services/propertyService';

const Permissions: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'workstream' | 'propertyGroup'>('workstream');
  
  // Workstream state
  const [workstreams, setWorkstreams] = useState<WorkstreamResponseDto[]>([]);
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [permissionTypes, setPermissionTypes] = useState<PermissionTypeDto[]>([]);
  const [selectedWorkstream, setSelectedWorkstream] = useState<number | null>(null);
  const [workstreamUsers, setWorkstreamUsers] = useState<UserResponseDto[]>([]);
  const [showAssignModal, setShowAssignModal] = useState(false);
  
  // Property Group state
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [selectedPropertyGroup, setSelectedPropertyGroup] = useState<number | null>(null);
  const [propertyGroupUsers, setPropertyGroupUsers] = useState<UserResponseDto[]>([]);
  const [showPropertyGroupAssignModal, setShowPropertyGroupAssignModal] = useState(false);
  
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

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  const selectedWorkstreamData = workstreams.find(w => w.workstreamId === selectedWorkstream);
  const selectedPropertyGroupData = propertyGroups.find(pg => pg.propertyGroupId === selectedPropertyGroup);

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">Permissions</h2>
        
        {/* Tabs */}
        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab('workstream')}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'workstream'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Workstream Permissions
            </button>
            <button
              onClick={() => setActiveTab('propertyGroup')}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'propertyGroup'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Property Group Access
            </button>
          </nav>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* Workstream Permissions Tab */}
      {activeTab === 'workstream' && (
        <div>
          <div className="flex justify-between items-center mb-6">
            <h3 className="text-xl font-semibold text-gray-900">Workstream Permissions</h3>
        {selectedWorkstream && (
          <button
            onClick={() => setShowAssignModal(true)}
            className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
          >
            Assign User
          </button>
        )}
      </div>

          {/* Workstream Selector */}
      <div className="mb-6">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Select Workstream
        </label>
        <select
          value={selectedWorkstream || ''}
          onChange={(e) => setSelectedWorkstream(parseInt(e.target.value))}
          className="px-4 py-2 border border-gray-300 rounded-md"
        >
          <option value="">Select a workstream</option>
          {workstreams.map((ws) => (
            <option key={ws.workstreamId} value={ws.workstreamId}>
              {ws.workstreamName}
            </option>
          ))}
        </select>
      </div>

      {selectedWorkstream && (
        <div className="bg-white shadow rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">
              Users assigned to: {selectedWorkstreamData?.workstreamName}
            </h3>
          </div>
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Email
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Permission Type
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {workstreamUsers.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-4 text-center text-sm text-gray-500">
                    No users assigned to this workstream
                  </td>
                </tr>
              ) : (
                workstreamUsers.map((user) => {
                  const access = user.workstreamAccess.find(
                    wa => wa.workstreamId === selectedWorkstream
                  );
                  return (
                    <tr key={user.userId}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {user.email}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {user.firstName} {user.lastName}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {access?.permissionTypeName || '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <button
                          onClick={() => handleRemoveUser(user.userId)}
                          className="text-red-600 hover:text-red-900"
                        >
                          Remove
                        </button>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Assign User Modal */}
      {showAssignModal && selectedWorkstream && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Assign User to Workstream</h3>
            <form onSubmit={handleAssignUser}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  User *
                </label>
                <select
                  name="userId"
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                >
                  <option value="">Select User</option>
                  {users
                    .filter(u => !workstreamUsers.some(wu => wu.userId === u.userId))
                    .map((user) => (
                      <option key={user.userId} value={user.userId}>
                        {user.email} - {user.firstName} {user.lastName}
                      </option>
                    ))}
                </select>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Permission Type *
                </label>
                <select
                  name="permissionTypeId"
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                >
                  <option value="">Select Permission Type</option>
                  {permissionTypes.map((pt) => (
                    <option key={pt.permissionTypeId} value={pt.permissionTypeId}>
                      {pt.permissionTypeName}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowAssignModal(false)}
                  className="px-4 py-2 border border-gray-300 rounded-md"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                >
                  Assign
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Property Group Access Tab */}
      {activeTab === 'propertyGroup' && (
        <div>
          <div className="flex justify-between items-center mb-6">
            <h3 className="text-xl font-semibold text-gray-900">Property Group Access</h3>
            {selectedPropertyGroup && (
              <button
                onClick={() => setShowPropertyGroupAssignModal(true)}
                className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
              >
                Assign User
              </button>
            )}
          </div>

          {/* Property Group Selector */}
          <div className="mb-6">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Property Group
            </label>
            <select
              value={selectedPropertyGroup || ''}
              onChange={(e) => setSelectedPropertyGroup(parseInt(e.target.value))}
              className="px-4 py-2 border border-gray-300 rounded-md"
            >
              <option value="">Select a property group</option>
              {propertyGroups.map((pg) => (
                <option key={pg.propertyGroupId} value={pg.propertyGroupId}>
                  {pg.propertyGroupName}
                </option>
              ))}
            </select>
          </div>

          {selectedPropertyGroup && (
            <div className="bg-white shadow rounded-lg overflow-hidden">
              <div className="px-6 py-4 border-b border-gray-200">
                <h3 className="text-lg font-medium text-gray-900">
                  Users with access to: {selectedPropertyGroupData?.propertyGroupName}
                </h3>
              </div>
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Email
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Name
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {propertyGroupUsers.length === 0 ? (
                    <tr>
                      <td colSpan={3} className="px-6 py-4 text-center text-sm text-gray-500">
                        No users assigned to this property group
                      </td>
                    </tr>
                  ) : (
                    propertyGroupUsers.map((user) => (
                      <tr key={user.userId}>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {user.email}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {user.firstName} {user.lastName}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <button
                            onClick={() => handleRemovePropertyGroupUser(user.userId)}
                            className="text-red-600 hover:text-red-900"
                          >
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}

          {/* Assign User to Property Group Modal */}
          {showPropertyGroupAssignModal && selectedPropertyGroup && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Assign User to Property Group</h3>
                <form onSubmit={handleAssignPropertyGroupUser}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      User *
                    </label>
                    <select
                      name="userId"
                      required
                      className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    >
                      <option value="">Select User</option>
                      {users
                        .filter(u => !propertyGroupUsers.some(pgu => pgu.userId === u.userId))
                        .map((user) => (
                          <option key={user.userId} value={user.userId}>
                            {user.email} - {user.firstName} {user.lastName}
                          </option>
                        ))}
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button
                      type="button"
                      onClick={() => setShowPropertyGroupAssignModal(false)}
                      className="px-4 py-2 border border-gray-300 rounded-md"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                    >
                      Assign
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default Permissions;
