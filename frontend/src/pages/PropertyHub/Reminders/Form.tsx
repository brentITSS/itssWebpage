import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { reminderService, CreateReminderRequest, UpdateReminderRequest } from '../../../services/reminderService';
import { propertyService, PropertyResponseDto, PropertyGroupResponseDto } from '../../../services/propertyService';

const ReminderForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const reminderId = id ? parseInt(id, 10) : null;

  const [groups, setGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [formData, setFormData] = useState<CreateReminderRequest>({
    propertyGroupId: undefined,
    propertyId: undefined,
    title: '',
    notes: '',
    dueDate: new Date().toISOString().split('T')[0],
    isCompleted: false,
  });

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [g, p] = await Promise.all([propertyService.getPropertyGroups(), propertyService.getProperties()]);
      setGroups(g);
      setProperties(p);

      if (isEdit && reminderId) {
        const r = await reminderService.getReminder(reminderId);
        setFormData({
          propertyGroupId: r.propertyGroupId,
          propertyId: r.propertyId,
          title: r.title,
          notes: r.notes || '',
          dueDate: new Date(r.dueDate).toISOString().split('T')[0],
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      if (reminderId) {
        const upd: UpdateReminderRequest = {
          propertyGroupId: formData.propertyGroupId,
          propertyId: formData.propertyId,
          title: formData.title,
          notes: formData.notes,
          dueDate: formData.dueDate,
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
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Property group (optional)</label>
          <select
            value={formData.propertyGroupId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                propertyGroupId: e.target.value ? parseInt(e.target.value, 10) : undefined,
                propertyId: undefined,
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
          <label className="block text-sm font-medium text-gray-700 mb-1">Property (optional)</label>
          <select
            value={formData.propertyId ?? ''}
            onChange={(e) =>
              setFormData({
                ...formData,
                propertyId: e.target.value ? parseInt(e.target.value, 10) : undefined,
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
          <label className="block text-sm font-medium text-gray-700 mb-1">Title *</label>
          <input
            type="text"
            required
            value={formData.title}
            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
          <textarea
            rows={4}
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Due date *</label>
          <input
            type="date"
            required
            value={formData.dueDate}
            onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
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
            Completed
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
