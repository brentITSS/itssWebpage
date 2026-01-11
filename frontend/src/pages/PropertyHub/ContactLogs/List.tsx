import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { contactLogService, ContactLogResponseDto } from '../../../services/contactLogService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';

const ContactLogsList: React.FC = () => {
  const navigate = useNavigate();
  const [contactLogs, setContactLogs] = useState<ContactLogResponseDto[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<ContactLogResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>('');
  const [filterContactLogTypeId, setFilterContactLogTypeId] = useState<number | ''>('');
  const [filterDateFrom, setFilterDateFrom] = useState<string>('');
  const [filterDateTo, setFilterDateTo] = useState<string>('');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [logsData, propertiesData, typesData] = await Promise.all([
        contactLogService.getContactLogs(),
        propertyService.getProperties(),
        contactLogService.getContactLogTypes(),
      ]);
      setContactLogs(logsData);
      setFilteredLogs(logsData);
      setProperties(propertiesData);
      setContactLogTypes(typesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load contact logs');
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    let filtered = [...contactLogs];

    if (filterPropertyId) {
      filtered = filtered.filter(log => log.propertyId === filterPropertyId);
    }

    if (filterContactLogTypeId) {
      filtered = filtered.filter(log => log.contactLogTypeId === filterContactLogTypeId);
    }

    if (filterDateFrom) {
      filtered = filtered.filter(log => new Date(log.contactDate) >= new Date(filterDateFrom));
    }

    if (filterDateTo) {
      filtered = filtered.filter(log => new Date(log.contactDate) <= new Date(filterDateTo));
    }

    setFilteredLogs(filtered);
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this contact log?')) return;

    try {
      await contactLogService.deleteContactLog(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete contact log');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Contact Logs</h2>
        <button
          onClick={() => navigate('/Property Hub/Contact Logs/New')}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          New Contact Log
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4 mb-6">
        <h3 className="text-sm font-medium text-gray-700 mb-4">Filters</h3>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Property</label>
            <select
              value={filterPropertyId}
              onChange={(e) => setFilterPropertyId(e.target.value ? parseInt(e.target.value) : '')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">All Properties</option>
              {properties.map(p => (
                <option key={p.propertyId} value={p.propertyId}>{p.propertyName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Contact Log Type</label>
            <select
              value={filterContactLogTypeId}
              onChange={(e) => setFilterContactLogTypeId(e.target.value ? parseInt(e.target.value) : '')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">All Types</option>
              {contactLogTypes.map(t => (
                <option key={t.contactLogTypeId} value={t.contactLogTypeId}>{t.contactLogTypeName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Date From</label>
            <input
              type="date"
              value={filterDateFrom}
              onChange={(e) => setFilterDateFrom(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Date To</label>
            <input
              type="date"
              value={filterDateTo}
              onChange={(e) => setFilterDateTo(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>
        {(filterPropertyId || filterContactLogTypeId || filterDateFrom || filterDateTo) && (
          <button
            onClick={() => {
              setFilterPropertyId('');
              setFilterContactLogTypeId('');
              setFilterDateFrom('');
              setFilterDateTo('');
            }}
            className="mt-4 text-sm text-blue-600 hover:text-blue-800"
          >
            Clear Filters
          </button>
        )}
      </div>

      {/* Contact Logs Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tenant</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Subject</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Notes</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filteredLogs.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-4 text-center text-sm text-gray-500">
                  No contact logs found
                </td>
              </tr>
            ) : (
              filteredLogs.map((log) => (
                <tr key={log.contactLogId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {new Date(log.contactDate).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{log.propertyName}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{log.tenantName || '-'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{log.contactLogTypeName}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{log.subject}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <div className="max-w-xs truncate">{log.notes || '-'}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                    <button
                      onClick={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}`)}
                      className="text-blue-600 hover:text-blue-900"
                    >
                      View
                    </button>
                    <button
                      onClick={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}?edit=true`)}
                      className="text-green-600 hover:text-green-900"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(log.contactLogId)}
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

export default ContactLogsList;
