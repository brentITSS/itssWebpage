import React, { useState, useEffect } from 'react';
import { propertyAdminService, TenantResponseDto, CreateTenantRequest, UpdateTenantRequest, TenancyResponseDto, CreateTenancyRequest, UpdateTenancyRequest, PropertyResponseDto } from '../../../services/propertyAdminService';

const Tenants: React.FC = () => {
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showTenantModal, setShowTenantModal] = useState(false);
  const [showTenancyModal, setShowTenancyModal] = useState(false);
  const [editingTenant, setEditingTenant] = useState<TenantResponseDto | null>(null);
  const [editingTenancy, setEditingTenancy] = useState<TenancyResponseDto | null>(null);
  const [selectedTenantId, setSelectedTenantId] = useState<number | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [tenantsData, tenanciesData, propertiesData] = await Promise.all([
        propertyAdminService.getTenants(),
        propertyAdminService.getTenancies(),
        propertyAdminService.getProperties(),
      ]);
      setTenants(tenantsData);
      setTenancies(tenanciesData);
      setProperties(propertiesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTenant = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateTenantRequest = {
      firstName: formData.get('firstName') as string,
      lastName: formData.get('lastName') as string,
      email: formData.get('email') as string || undefined,
      phone: formData.get('phone') as string || undefined,
    };

    try {
      await propertyAdminService.createTenant(request);
      setShowTenantModal(false);
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

  const handleCreateTenancy = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateTenancyRequest = {
      propertyId: parseInt(formData.get('propertyId') as string),
      tenantId: selectedTenantId || parseInt(formData.get('tenantId') as string),
      startDate: new Date(formData.get('startDate') as string),
      endDate: formData.get('endDate') ? new Date(formData.get('endDate') as string) : undefined,
      monthlyRent: formData.get('monthlyRent') ? parseFloat(formData.get('monthlyRent') as string) : undefined,
    };

    try {
      await propertyAdminService.createTenancy(request);
      setShowTenancyModal(false);
      setSelectedTenantId(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create tenancy');
    }
  };

  const handleUpdateTenancy = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateTenancyRequest = {
      propertyId: formData.get('propertyId') ? parseInt(formData.get('propertyId') as string) : undefined,
      tenantId: formData.get('tenantId') ? parseInt(formData.get('tenantId') as string) : undefined,
      startDate: formData.get('startDate') ? new Date(formData.get('startDate') as string) : undefined,
      endDate: formData.get('endDate') ? new Date(formData.get('endDate') as string) : undefined,
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

  const handleDeleteTenancy = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this tenancy?')) return;

    try {
      await propertyAdminService.deleteTenancy(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete tenancy');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Tenants</h2>
        <div className="space-x-2">
          <button
            onClick={() => setShowTenancyModal(true)}
            className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700"
          >
            Create Tenancy
          </button>
          <button
            onClick={() => setShowTenantModal(true)}
            className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
          >
            Create Tenant
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* Tenants Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden mb-6">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Phone</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {tenants.map((tenant) => (
              <tr key={tenant.tenantId}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {tenant.firstName} {tenant.lastName}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{tenant.email || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{tenant.phone || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  <button onClick={() => setEditingTenant(tenant)} className="text-blue-600 hover:text-blue-900">Edit</button>
                  <button onClick={() => { setSelectedTenantId(tenant.tenantId); setShowTenancyModal(true); }} className="text-green-600 hover:text-green-900">Add Tenancy</button>
                  <button onClick={() => handleDeleteTenant(tenant.tenantId)} className="text-red-600 hover:text-red-900">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Tenancies Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <h3 className="text-xl font-semibold px-6 py-4 border-b border-gray-200">Tenancies</h3>
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tenant</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Start Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">End Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Monthly Rent</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {tenancies.map((tenancy) => (
              <tr key={tenancy.tenancyId}>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{tenancy.propertyName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{tenancy.tenantName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{new Date(tenancy.startDate).toLocaleDateString()}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{tenancy.endDate ? new Date(tenancy.endDate).toLocaleDateString() : '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">£{tenancy.monthlyRent.toFixed(2)}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  <button onClick={() => setEditingTenancy(tenancy)} className="text-blue-600 hover:text-blue-900">Edit</button>
                  <button onClick={() => handleDeleteTenancy(tenancy.tenancyId)} className="text-red-600 hover:text-red-900">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create Tenant Modal */}
      {showTenantModal && (
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
                <input type="text" name="phone" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="flex justify-end space-x-2">
                <button type="button" onClick={() => setShowTenantModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create/Edit Tenancy Modal */}
      {(showTenancyModal || editingTenancy) && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">{editingTenancy ? 'Edit' : 'Create'} Tenancy</h3>
            <form onSubmit={editingTenancy ? (e) => handleUpdateTenancy(editingTenancy.tenancyId, e) : handleCreateTenancy}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Property *</label>
                <select name="propertyId" required defaultValue={editingTenancy?.propertyId} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                  <option value="">Select Property</option>
                  {properties.map(p => (
                    <option key={p.propertyId} value={p.propertyId}>{p.propertyName}</option>
                  ))}
                </select>
              </div>
              {!selectedTenantId && (
                <div className="mb-4">
                  <label className="block text-sm font-medium text-gray-700 mb-1">Tenant *</label>
                  <select name="tenantId" required defaultValue={editingTenancy?.tenantId} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                    <option value="">Select Tenant</option>
                    {tenants.map(t => (
                      <option key={t.tenantId} value={t.tenantId}>{t.firstName} {t.lastName}</option>
                    ))}
                  </select>
                </div>
              )}
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
                <button type="button" onClick={() => { setShowTenancyModal(false); setEditingTenancy(null); setSelectedTenantId(null); }} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">{editingTenancy ? 'Update' : 'Create'}</button>
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
            <form onSubmit={(e) => handleUpdateTenant(editingTenant.tenantId, e)}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                <input type="text" name="firstName" defaultValue={editingTenant.firstName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                <input type="text" name="lastName" defaultValue={editingTenant.lastName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <input type="email" name="email" defaultValue={editingTenant.email || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                <input type="text" name="phone" defaultValue={editingTenant.phone || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
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

export default Tenants;
