import React, { useState, useEffect } from 'react';
import { propertyAdminService, PropertyResponseDto, CreatePropertyRequest, UpdatePropertyRequest, PropertyGroupResponseDto } from '../../../services/propertyAdminService';

const Properties: React.FC = () => {
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingProperty, setEditingProperty] = useState<PropertyResponseDto | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [propertiesData, groupsData] = await Promise.all([
        propertyAdminService.getProperties(),
        propertyAdminService.getPropertyGroups(),
      ]);
      setProperties(propertiesData);
      setPropertyGroups(groupsData);
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreatePropertyRequest = {
      propertyGroupId: parseInt(formData.get('propertyGroupId') as string),
      propertyName: formData.get('propertyName') as string,
      address: formData.get('address') as string || undefined,
      postCode: formData.get('postCode') as string || undefined,
    };

    try {
      await propertyAdminService.createProperty(request);
      setShowModal(false);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create property');
    }
  };

  const handleUpdate = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdatePropertyRequest = {
      propertyGroupId: formData.get('propertyGroupId') ? parseInt(formData.get('propertyGroupId') as string) : undefined,
      propertyName: formData.get('propertyName') as string || undefined,
      address: formData.get('address') as string || undefined,
      postCode: formData.get('postCode') as string || undefined,
    };

    try {
      await propertyAdminService.updateProperty(id, request);
      setEditingProperty(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update property');
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this property?')) return;

    try {
      await propertyAdminService.deleteProperty(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete property');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Properties</h2>
        <button
          onClick={() => setShowModal(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          Create Property
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
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Group</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Address</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Post Code</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {properties.map((property) => (
              <tr key={property.propertyId}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{property.propertyName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{property.propertyGroupName}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{property.address || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{property.postCode || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  <button onClick={() => setEditingProperty(property)} className="text-blue-600 hover:text-blue-900">Edit</button>
                  <button onClick={() => handleDelete(property.propertyId)} className="text-red-600 hover:text-red-900">Delete</button>
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
            <h3 className="text-lg font-bold mb-4">Create Property</h3>
            <form onSubmit={handleCreate}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Property Group *</label>
                <select name="propertyGroupId" required className="w-full px-3 py-2 border border-gray-300 rounded-md">
                  <option value="">Select Group</option>
                  {propertyGroups.map(g => (
                    <option key={g.propertyGroupId} value={g.propertyGroupId}>{g.propertyGroupName}</option>
                  ))}
                </select>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Property Name *</label>
                <input type="text" name="propertyName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                <input type="text" name="address" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Post Code</label>
                <input type="text" name="postCode" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Modal */}
      {editingProperty && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Edit Property</h3>
            <form onSubmit={(e) => handleUpdate(editingProperty.propertyId, e)}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Property Group</label>
                <select name="propertyGroupId" defaultValue={editingProperty.propertyGroupId} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                  {propertyGroups.map(g => (
                    <option key={g.propertyGroupId} value={g.propertyGroupId}>{g.propertyGroupName}</option>
                  ))}
                </select>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Property Name</label>
                <input type="text" name="propertyName" defaultValue={editingProperty.propertyName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                <input type="text" name="address" defaultValue={editingProperty.address || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Post Code</label>
                <input type="text" name="postCode" defaultValue={editingProperty.postCode || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => setEditingProperty(null)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Update</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Properties;
