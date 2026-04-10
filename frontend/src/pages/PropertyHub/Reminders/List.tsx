import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { reminderService, ReminderResponseDto } from '../../../services/reminderService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';

const RemindersList: React.FC = () => {
  const navigate = useNavigate();
  const [reminders, setReminders] = useState<ReminderResponseDto[]>([]);
  const [filtered, setFiltered] = useState<ReminderResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>('');
  const [filterCompleted, setFilterCompleted] = useState<'all' | 'open' | 'done'>('all');
  const [filterDateFrom, setFilterDateFrom] = useState('');
  const [filterDateTo, setFilterDateTo] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [r, p] = await Promise.all([reminderService.getReminders(), propertyService.getProperties()]);
      setReminders(r);
      setFiltered(r);
      setProperties(p);
    } catch (err: any) {
      setError(err.message || 'Failed to load reminders');
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = useCallback(() => {
    let list = [...reminders];
    if (filterPropertyId) {
      list = list.filter((x) => x.propertyId === filterPropertyId);
    }
    if (filterCompleted === 'open') list = list.filter((x) => !x.isCompleted);
    if (filterCompleted === 'done') list = list.filter((x) => x.isCompleted);
    if (filterDateFrom) {
      list = list.filter((x) => new Date(x.dueDate) >= new Date(filterDateFrom));
    }
    if (filterDateTo) {
      list = list.filter((x) => new Date(x.dueDate) <= new Date(filterDateTo));
    }
    setFiltered(list);
  }, [reminders, filterPropertyId, filterCompleted, filterDateFrom, filterDateTo]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete this reminder?')) return;
    try {
      await reminderService.deleteReminder(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Reminders</h2>
        <button
          type="button"
          onClick={() => navigate('/Property Hub/Reminders/New')}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          New reminder
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">{error}</div>
      )}

      <div className="bg-white rounded-lg shadow p-4 mb-6">
        <h3 className="text-sm font-medium text-gray-700 mb-4">Filters</h3>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Property</label>
            <select
              value={filterPropertyId === '' ? '' : String(filterPropertyId)}
              onChange={(e) => setFilterPropertyId(e.target.value ? parseInt(e.target.value, 10) : '')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">All</option>
              {properties.map((p) => (
                <option key={p.propertyId} value={p.propertyId}>
                  {p.propertyName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
            <select
              value={filterCompleted}
              onChange={(e) => setFilterCompleted(e.target.value as 'all' | 'open' | 'done')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="all">All</option>
              <option value="open">Open</option>
              <option value="done">Completed</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Due from</label>
            <input
              type="date"
              value={filterDateFrom}
              onChange={(e) => setFilterDateFrom(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Due to</label>
            <input
              type="date"
              value={filterDateTo}
              onChange={(e) => setFilterDateTo(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Due</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Title</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Group</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Done</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-4 text-center text-sm text-gray-500">
                  No reminders found
                </td>
              </tr>
            ) : (
              filtered.map((r) => (
                <tr key={r.reminderId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {new Date(r.dueDate).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{r.title}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{r.propertyName || '—'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{r.propertyGroupName || '—'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">{r.isCompleted ? 'Yes' : 'No'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                    <button
                      type="button"
                      onClick={() => navigate(`/Property Hub/Reminders/${r.reminderId}`)}
                      className="text-blue-600 hover:text-blue-900"
                    >
                      View
                    </button>
                    <button
                      type="button"
                      onClick={() => navigate(`/Property Hub/Reminders/${r.reminderId}?edit=true`)}
                      className="text-green-600 hover:text-green-900"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(r.reminderId)}
                      className="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default RemindersList;
