import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { contactLogService, CreateContactLogRequest, UpdateContactLogRequest, ContactLogResponseDto } from '../../../services/contactLogService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import { propertyAdminService, TenantResponseDto } from '../../../services/propertyAdminService';

const ContactLogForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const contactLogId = id ? parseInt(id) : null;

  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<any[]>([]);
  const [_contactLog, setContactLog] = useState<ContactLogResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Form state
  const [formData, setFormData] = useState<CreateContactLogRequest>({
    propertyId: 0,
    tenantId: undefined,
    contactLogTypeId: 0,
    subject: '',
    notes: '',
    contactDate: new Date().toISOString().split('T')[0],
  });

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [propertiesData, tenantsData, typesData] = await Promise.all([
        propertyService.getProperties(),
        propertyAdminService.getTenants(),
        contactLogService.getContactLogTypes(),
      ]);

      setProperties(propertiesData);
      setTenants(tenantsData);
      setContactLogTypes(typesData);

      if (isEdit && contactLogId) {
        const logData = await contactLogService.getContactLog(contactLogId);
        setContactLog(logData);
        setFormData({
          propertyId: logData.propertyId,
          tenantId: logData.tenantId || undefined,
          contactLogTypeId: logData.contactLogTypeId,
          subject: logData.subject,
          notes: logData.notes,
          contactDate: new Date(logData.contactDate).toISOString().split('T')[0],
        });
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  }, [id, isEdit, contactLogId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (isEdit && contactLogId) {
        const updateRequest: UpdateContactLogRequest = {
          propertyId: formData.propertyId,
          tenantId: formData.tenantId,
          contactLogTypeId: formData.contactLogTypeId,
          subject: formData.subject,
          notes: formData.notes,
          contactDate: formData.contactDate,
        };
        await contactLogService.updateContactLog(contactLogId, updateRequest);
      } else {
        await contactLogService.createContactLog(formData);
      }
      navigate('/Property Hub/Contact Logs');
    } catch (err: any) {
      setError(err.message || 'Failed to save contact log');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900">
          {isEdit ? 'Edit Contact Log' : 'New Contact Log'}
        </h2>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Property *
            </label>
            <select
              value={formData.propertyId}
              onChange={(e) => setFormData({
                ...formData,
                propertyId: parseInt(e.target.value),
                tenantId: undefined, // Reset tenant when property changes
              })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="0">Select Property</option>
              {properties.map(p => (
                <option key={p.propertyId} value={p.propertyId}>{p.propertyName}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Contact Date *
            </label>
            <input
              type="date"
              value={formData.contactDate}
              onChange={(e) => setFormData({ ...formData, contactDate: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Tenant (Optional)
            </label>
            <select
              value={formData.tenantId || ''}
              onChange={(e) => setFormData({
                ...formData,
                tenantId: e.target.value ? parseInt(e.target.value) : undefined,
              })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">None</option>
              {tenants.map(t => (
                <option key={t.tenantId} value={t.tenantId}>
                  {t.firstName} {t.lastName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Contact Log Type *
            </label>
            <select
              value={formData.contactLogTypeId}
              onChange={(e) => setFormData({ ...formData, contactLogTypeId: parseInt(e.target.value) })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="0">Select Contact Log Type</option>
              {contactLogTypes.map(t => (
                <option key={t.contactLogTypeId} value={t.contactLogTypeId}>{t.contactLogTypeName}</option>
              ))}
            </select>
          </div>

          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Subject *
            </label>
            <input
              type="text"
              value={formData.subject}
              onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>

        <div className="mt-6">
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Notes *
          </label>
          <textarea
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            rows={6}
            required
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>

        <div className="mt-6 flex justify-end space-x-4">
          <button
            type="button"
            onClick={() => navigate('/Property Hub/Contact Logs')}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={saving}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Saving...' : isEdit ? 'Update' : 'Create'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default ContactLogForm;
