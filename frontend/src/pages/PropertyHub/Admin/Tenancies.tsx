import React, { useState, useEffect } from 'react';
import { propertyAdminService, TenancyResponseDto, CreateTenancyRequest, UpdateTenancyRequest, PropertyResponseDto, TenantResponseDto, CreateTenantRequest, UpdateTenantRequest } from '../../../services/propertyAdminService';

const Tenancies: React.FC = () => {
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingTenancy, setEditingTenancy] = useState<TenancyResponseDto | null>(null);
  const [showTenantModal, setShowTenantModal] = useState(false);
  const [selectedTenancyForTenant, setSelectedTenancyForTenant] = useState<number | null>(null);
  const [editingTenant, setEditingTenant] = useState<{ tenant: TenantResponseDto; tenancyId: number } | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [tenanciesData, propertiesData] = await Promise.all([
        propertyAdminService.getTenancies(),
        propertyAdminService.getProperties(),
      ]);
      setTenancies(tenanciesData);
      setProperties(propertiesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const tenantIdValue = formData.get('tenantId') as string;
    const request: CreateTenancyRequest = {
      propertyId: parseInt(formData.get('propertyId') as string),
      tenantId: tenantIdValue ? parseInt(tenantIdValue) : undefined,
      startDate: new Date(formData.get('startDate') as string).toISOString(),
      endDate: formData.get('endDate') ? new Date(formData.get('endDate') as string).toISOString() : undefined,
      monthlyRent: formData.get('monthlyRent') ? parseFloat(formData.get('monthlyRent') as string) : undefined,
    };

    try {
      await propertyAdminService.createTenancy(request);
      setShowModal(false);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create tenancy');
    }
  };

  const handleUpdate = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateTenancyRequest = {
      propertyId: formData.get('propertyId') ? parseInt(formData.get('propertyId') as string) : undefined,
      tenantId: formData.get('tenantId') ? parseInt(formData.get('tenantId') as string) : undefined,
      startDate: formData.get('startDate') ? new Date(formData.get('startDate') as string).toISOString() : undefined,
      endDate: formData.get('endDate') ? new Date(formData.get('endDate') as string).toISOString() : undefined,
      monthlyRent: formData.get('monthlyRent') ? parseFloat(formData.get('monthlyRent') as string) : undefined,
    };

    try {
      await propertyAdminService.updateTenancy(id, request);
      setEditingTenancy(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update tenancy');
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this tenancy?')) return;

    try {
      await propertyAdminService.deleteTenancy(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete tenancy');
    }
  };

  const handleCreateTenant = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedTenancyForTenant) return;

    const formData = new FormData(e.currentTarget);
    const request: CreateTenantRequest = {
      tenancyId: selectedTenancyForTenant,
      firstName: formData.get('firstName') as string,
      lastName: formData.get('lastName') as string,
      email: formData.get('email') as string || undefined,
      phone: formData.get('phone') as string || undefined,
    };

    try {
      await propertyAdminService.createTenant(request);
      setShowTenantModal(false);
      setSelectedTenancyForTenant(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create tenant');
    }
  };

  const handleUpdateTenant = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateTenantRequest = {
      firstName: formData.get('firstName') as string || undefined,
      lastName: formData.get('lastName') as string || undefined,
      email: formData.get('email') as string || undefined,
      phone: formData.get('phone') as string || undefined,
    };

    try {
      await propertyAdminService.updateTenant(id, request);
      setEditingTenant(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update tenant');
    }
  };

  const handleDeleteTenant = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this tenant?')) return;

    try {
      await propertyAdminService.deleteTenant(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete tenant');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Tenancies</h2>
        <button
          onClick={() => setShowModal(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          Create Tenancy
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
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tenants</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Start Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">End Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Monthly Rent</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {tenancies.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-4 text-center text-sm text-gray-500">
                  No tenancies found
                </td>
              </tr>
            ) : (
              tenancies.map((tenancy) => (
                <tr key={tenancy.tenancyId}>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{tenancy.propertyName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <div className="space-y-2">
                      {tenancy.tenants.length > 0 ? (
                        tenancy.tenants.map((tenant) => (
                          <div key={tenant.tenantId} className="flex items-center justify-between bg-gray-50 px-2 py-1 rounded">
                            <span>{tenant.firstName} {tenant.lastName}</span>
                            <div className="space-x-2">
                              <button
                                onClick={() => setEditingTenant({ tenant, tenancyId: tenancy.tenancyId })}
                                className="text-blue-600 hover:text-blue-900 text-xs"
                              >
                                Edit
                              </button>
                              <button
                                onClick={() => handleDeleteTenant(tenant.tenantId)}
                                className="text-red-600 hover:text-red-900 text-xs"
                              >
                                Delete
                              </button>
                            </div>
                          </div>
                        ))
                      ) : (
                        <span className="text-gray-400">-</span>
                      )}
                      <button
                        onClick={() => {
                          setSelectedTenancyForTenant(tenancy.tenancyId);
                          setShowTenantModal(true);
                        }}
                        className="text-blue-600 hover:text-blue-900 text-xs font-medium"
                      >
                        + Add Tenant
                      </button>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{new Date(tenancy.startDate).toLocaleDateString()}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{tenancy.endDate ? new Date(tenancy.endDate).toLocaleDateString() : '-'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">£{tenancy.monthlyRent?.toFixed(2) || '-'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                    <button onClick={() => setEditingTenancy(tenancy)} className="text-blue-600 hover:text-blue-900">Edit</button>
                    <button onClick={() => handleDelete(tenancy.tenancyId)} className="text-red-600 hover:text-red-900">Delete</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Create/Edit Modal */}
      {(showModal || editingTenancy) && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">{editingTenancy ? 'Edit' : 'Create'} Tenancy</h3>
            <form onSubmit={editingTenancy ? (e) => handleUpdate(editingTenancy.tenancyId, e) : handleCreate}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Property *</label>
                <select name="propertyId" required defaultValue={editingTenancy?.propertyId} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                  <option value="">Select Property</option>
                  {properties.map(p => (
                    <option key={p.propertyId} value={p.propertyId}>{p.propertyName}</option>
                  ))}
                </select>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Start Date *</label>
                <input type="date" name="startDate" required defaultValue={editingTenancy ? new Date(editingTenancy.startDate).toISOString().split('T')[0] : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
                <input type="date" name="endDate" defaultValue={editingTenancy?.endDate ? new Date(editingTenancy.endDate).toISOString().split('T')[0] : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Monthly Rent</label>
                <input type="number" step="0.01" name="monthlyRent" defaultValue={editingTenancy?.monthlyRent || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => { setShowModal(false); setEditingTenancy(null); }} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">{editingTenancy ? 'Update' : 'Create'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Tenant Modal */}
      {showTenantModal && selectedTenancyForTenant && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Create Tenant</h3>
            <form onSubmit={handleCreateTenant}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">First Name *</label>
                <input type="text" name="firstName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Last Name *</label>
                <input type="text" name="lastName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <input type="email" name="email" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                <input type="tel" name="phone" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => {
                    setShowTenantModal(false);
                    setSelectedTenancyForTenant(null);
                  }}
                  className="px-4 py-2 border border-gray-300 rounded-md"
                >
                  Cancel
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Tenant Modal */}
      {editingTenant && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Edit Tenant</h3>
            <form onSubmit={(e) => handleUpdateTenant(editingTenant.tenant.tenantId, e)}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                <input type="text" name="firstName" defaultValue={editingTenant.tenant.firstName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                <input type="text" name="lastName" defaultValue={editingTenant.tenant.lastName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <input type="email" name="email" defaultValue={editingTenant.tenant.email || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                <input type="tel" name="phone" defaultValue={editingTenant.tenant.phone || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => setEditingTenant(null)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Update</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Tenancies;
