import React, { useState, useEffect } from 'react';
import { propertyAdminService, PropertyGroupResponseDto, CreatePropertyGroupRequest, UpdatePropertyGroupRequest } from '../../../services/propertyAdminService';

const PropertyGroups: React.FC = () => {
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingGroup, setEditingGroup] = useState<PropertyGroupResponseDto | null>(null);

  useEffect(() => {
    loadPropertyGroups();
  }, []);

  const loadPropertyGroups = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await propertyAdminService.getPropertyGroups();
      setPropertyGroups(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load property groups');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreatePropertyGroupRequest = {
      propertyGroupName: formData.get('propertyGroupName') as string,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.createPropertyGroup(request);
      setShowModal(false);
      loadPropertyGroups();
    } catch (err: any) {
      setError(err.message || 'Failed to create property group');
    }
  };

  const handleUpdate = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdatePropertyGroupRequest = {
      propertyGroupName: formData.get('propertyGroupName') as string || undefined,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.updatePropertyGroup(id, request);
      setEditingGroup(null);
      loadPropertyGroups();
    } catch (err: any) {
      setError(err.message || 'Failed to update property group');
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this property group?')) return;

    try {
      await propertyAdminService.deletePropertyGroup(id);
      loadPropertyGroups();
    } catch (err: any) {
      setError(err.message || 'Failed to delete property group');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Property Groups</h2>
        <button
          onClick={() => setShowModal(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          Create Property Group
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Properties</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {propertyGroups.map((group) => (
              <tr key={group.propertyGroupId}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {group.propertyGroupName}
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">{group.description || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{group.propertyCount}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  <button onClick={() => setEditingGroup(group)} className="text-blue-600 hover:text-blue-900">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(group.propertyGroupId)} className="text-red-600 hover:text-red-900">
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Create Property Group</h3>
            <form onSubmit={handleCreate}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
                <input type="text" name="propertyGroupName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <textarea name="description" rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                <select name="isActive" className="w-full px-3 py-2 border border-gray-300 rounded-md" defaultValue="true">
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">
                  Cancel
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Modal */}
      {editingGroup && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Edit Property Group</h3>
            <form onSubmit={(e) => handleUpdate(editingGroup.propertyGroupId, e)}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
                <input type="text" name="propertyGroupName" defaultValue={editingGroup.propertyGroupName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <textarea name="description" rows={3} defaultValue={editingGroup.description || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                <select name="isActive" defaultValue={editingGroup.isActive === true ? 'true' : editingGroup.isActive === false ? 'false' : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                  <option value="">Not Set</option>
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => setEditingGroup(null)} className="px-4 py-2 border border-gray-300 rounded-md">
                  Cancel
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
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

export default PropertyGroups;
