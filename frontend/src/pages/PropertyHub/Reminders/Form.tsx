import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  reminderService,
  CreateReminderRequest,
  UpdateReminderRequest,
  ReminderPriorityDto,
} from '../../../services/reminderService';
import { propertyService, PropertyResponseDto, PropertyGroupResponseDto } from '../../../services/propertyService';
import { propertyAdminService, TenantResponseDto, TenancyResponseDto } from '../../../services/propertyAdminService';

const ReminderForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const reminderId = id ? parseInt(id, 10) : null;

  const [groups, setGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [priorities, setPriorities] = useState<ReminderPriorityDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [formData, setFormData] = useState<CreateReminderRequest>({
    tenantId: undefined,
    tenancyId: undefined,
    propertyGroupId: undefined,
    propertyId: undefined,
    title: '',
    reminderPriorityId: undefined,
    notes: '',
    isCompleted: false,
  });

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [g, p, tnt, tnc, pri] = await Promise.all([
        propertyService.getPropertyGroups(),
        propertyService.getProperties(),
        propertyAdminService.getTenants(),
        propertyAdminService.getTenancies(),
        reminderService.getReminderPriorities(),
      ]);
      setGroups(g);
      setProperties(p);
      setTenants(tnt);
      setTenancies(tnc);
      setPriorities(pri.filter((x) => x.isActive !== false));

      if (isEdit && reminderId) {
        const r = await reminderService.getReminder(reminderId);
        setFormData({
          tenantId: r.tenantId,
          tenancyId: r.tenancyId,
          propertyGroupId: r.propertyGroupId,
          propertyId: r.propertyId,
          title: r.title,
          reminderPriorityId: r.reminderPriorityId,
          notes: r.notes || '',
          isCompleted: r.isCompleted,
        });
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load');
    } finally {
      setLoading(false);
    }
  }, [isEdit, reminderId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const propertiesForGroup = formData.propertyGroupId
    ? properties.filter((p) => p.propertyGroupId === formData.propertyGroupId)
    : properties;

  const tenanciesForProperty = formData.propertyId
    ? tenancies.filter((t) => t.propertyId === formData.propertyId)
    : [];

  const tenantIdsForProperty = new Set<number>();
  tenanciesForProperty.forEach((tenancy) => {
    if (tenancy.tenants && Array.isArray(tenancy.tenants)) {
      tenancy.tenants.forEach((t) => tenantIdsForProperty.add(t.tenantId));
    }
  });

  const tenantsForProperty =
    formData.propertyId && formData.propertyId > 0
      ? tenants.filter((t) => tenantIdsForProperty.has(t.tenantId))
      : [];

  const hasScopeLink =
    !!formData.propertyGroupId ||
    !!formData.propertyId ||
    !!formData.tenancyId ||
    !!formData.tenantId;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    if (!hasScopeLink) {
      setError('Choose a property group, property, tenancy, or tenant.');
      setSaving(false);
      return;
    }
    try {
      if (reminderId) {
        const upd: UpdateReminderRequest = {
          tenantId: formData.tenantId,
          tenancyId: formData.tenancyId,
          propertyGroupId: formData.propertyGroupId,
          propertyId: formData.propertyId,
          reminderPriorityId: formData.reminderPriorityId ?? null,
          title: formData.title,
          notes: formData.notes,
          isCompleted: formData.isCompleted,
        };
        await reminderService.updateReminder(reminderId, upd);
        navigate(`/Property Hub/Reminders/${reminderId}`);
      } else {
        const created = await reminderService.createReminder(formData);
        navigate(`/Property Hub/Reminders/${created.reminderId}`);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">{reminderId ? 'Edit reminder' : 'New reminder'}</h2>
        <button
          type="button"
          onClick={() => navigate('/Property Hub/Reminders')}
          className="text-gray-600 hover:text-gray-900"
        >
          Back to list
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">{error}</div>
      )}

      <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg p-6 max-w-2xl space-y-4">
        <p className="text-sm text-gray-500">
          You must choose at least one: property group, property, tenancy, or tenant. The API fills property and group
          from tenancy or tenant when possible.
        </p>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Property group</label>
          <select
            value={formData.propertyGroupId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                propertyGroupId: e.target.value ? parseInt(e.target.value, 10) : undefined,
                propertyId: undefined,
                tenancyId: undefined,
                tenantId: undefined,
              })
            }
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          >
            <option value="">—</option>
            {groups.map((g) => (
              <option key={g.propertyGroupId} value={g.propertyGroupId}>
                {g.propertyGroupName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Property</label>
          <select
            value={formData.propertyId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                propertyId: e.target.value ? parseInt(e.target.value, 10) : undefined,
                tenancyId: undefined,
                tenantId: undefined,
              })
            }
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          >
            <option value="">—</option>
            {propertiesForGroup.map((p) => (
              <option key={p.propertyId} value={p.propertyId}>
                {p.propertyName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Tenancy</label>
          <select
            value={formData.tenancyId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                tenancyId: e.target.value ? parseInt(e.target.value, 10) : undefined,
                tenantId: undefined,
              })
            }
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            disabled={!formData.propertyId}
          >
            <option value="">—</option>
            {tenanciesForProperty.map((t) => (
              <option key={t.tenancyId} value={t.tenancyId}>
                {t.description
                  ? t.description.length > 60
                    ? `${t.description.slice(0, 60)}…`
                    : t.description
                  : `Tenancy #${t.tenancyId}`}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Tenant</label>
          <select
            value={formData.tenantId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                tenantId: e.target.value ? parseInt(e.target.value, 10) : undefined,
              })
            }
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            disabled={!formData.propertyId}
          >
            <option value="">—</option>
            {tenantsForProperty.map((t) => (
              <option key={t.tenantId} value={t.tenantId}>
                {t.firstName} {t.lastName} ({t.email || t.tenantId})
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Reminder *</label>
          <input
            type="text"
            required
            value={formData.title}
            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            placeholder="Short summary (maps to tblReminder.reminder)"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
          <select
            value={formData.reminderPriorityId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                reminderPriorityId: e.target.value ? parseInt(e.target.value, 10) : undefined,
              })
            }
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          >
            <option value="">— None —</option>
            {priorities.map((pr) => (
              <option key={pr.reminderPriorityId} value={pr.reminderPriorityId}>
                {pr.reminderPriorityName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Detail</label>
          <textarea
            rows={4}
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            placeholder="Optional notes (tblReminder.reminderDetail)"
          />
        </div>
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="done"
            checked={formData.isCompleted}
            onChange={(e) => setFormData({ ...formData, isCompleted: e.target.checked })}
          />
          <label htmlFor="done" className="text-sm text-gray-700">
            Completed (sets reminderActive = 0 in the database)
          </label>
        </div>
        <div className="flex gap-2 pt-4">
          <button
            type="submit"
            disabled={saving}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/Property Hub/Reminders')}
            className="px-4 py-2 border border-gray-300 rounded-md"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
};

export default ReminderForm;
