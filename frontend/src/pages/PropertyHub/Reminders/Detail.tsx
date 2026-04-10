import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { reminderService, ReminderResponseDto } from '../../../services/reminderService';
import ReminderForm from './Form';

const ReminderDetail: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = searchParams.get('edit') === 'true';

  const [reminder, setReminder] = useState<ReminderResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const r = await reminderService.getReminder(parseInt(id, 10));
      setReminder(r);
    } catch (err: any) {
      setError(err.message || 'Failed to load reminder');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    if (id && !isEdit) {
      load();
    }
  }, [id, isEdit, load]);

  if (isEdit) {
    return <ReminderForm />;
  }

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  if (error || !reminder) {
    return (
      <div className="text-center py-8 text-red-600">
        {error || 'Not found'}
        <div className="mt-4">
          <button type="button" onClick={() => navigate('/Property Hub/Reminders')} className="text-blue-600">
            Back to list
          </button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">{reminder.title}</h2>
        <div className="space-x-2">
          <button
            type="button"
            onClick={() => navigate(`/Property Hub/Reminders/${reminder.reminderId}?edit=true`)}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Edit
          </button>
          <button
            type="button"
            onClick={() => navigate('/Property Hub/Reminders')}
            className="px-4 py-2 border border-gray-300 rounded-md"
          >
            Back
          </button>
        </div>
      </div>

      <div className="bg-white shadow rounded-lg p-6 max-w-2xl space-y-3 text-sm">
        {reminder.reminderPriorityName && (
          <p className="flex flex-wrap items-center gap-2">
            <span className="font-medium text-gray-700">Priority: </span>
            <span
              className="rounded-full px-2 py-0.5 text-xs font-medium text-white"
              style={{
                backgroundColor: reminder.reminderPriorityColor || '#64748b',
              }}
            >
              {reminder.reminderPriorityName}
            </span>
          </p>
        )}
        <p>
          <span className="font-medium text-gray-700">Completed: </span>
          {reminder.isCompleted ? 'Yes' : 'No'}
        </p>
        <p>
          <span className="font-medium text-gray-700">Created: </span>
          {reminder.createdDate ? new Date(reminder.createdDate).toLocaleString() : '—'}
        </p>
        {reminder.createdBy && (
          <p>
            <span className="font-medium text-gray-700">Created by: </span>
            {reminder.createdBy}
          </p>
        )}
        <p>
          <span className="font-medium text-gray-700">Property group: </span>
          {reminder.propertyGroupName || '—'}
        </p>
        <p>
          <span className="font-medium text-gray-700">Property: </span>
          {reminder.propertyName || '—'}
        </p>
        <p>
          <span className="font-medium text-gray-700">Tenancy: </span>
          {reminder.tenancySummary || '—'}
        </p>
        <p>
          <span className="font-medium text-gray-700">Tenant: </span>
          {reminder.tenantName || '—'}
        </p>
        {reminder.notes && (
          <div>
            <p className="font-medium text-gray-700 mb-1">Detail</p>
            <p className="text-gray-600 whitespace-pre-wrap">{reminder.notes}</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default ReminderDetail;
