import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { journalService, JournalLogResponseDto } from '../../../services/journalService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';

const JournalLogsList: React.FC = () => {
  const navigate = useNavigate();
  const [journalLogs, setJournalLogs] = useState<JournalLogResponseDto[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<JournalLogResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [journalTypes, setJournalTypes] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>('');
  const [filterJournalTypeId, setFilterJournalTypeId] = useState<number | ''>('');
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
        journalService.getJournalLogs(),
        propertyService.getProperties(),
        journalService.getJournalTypes(),
      ]);
      setJournalLogs(logsData);
      setFilteredLogs(logsData);
      setProperties(propertiesData);
      setJournalTypes(typesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load journal logs');
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    let filtered = [...journalLogs];

    if (filterPropertyId) {
      filtered = filtered.filter(log => log.propertyId === filterPropertyId);
    }

    if (filterJournalTypeId) {
      filtered = filtered.filter(log => log.journalTypeId === filterJournalTypeId);
    }

    if (filterDateFrom) {
      filtered = filtered.filter(log => new Date(log.transactionDate) >= new Date(filterDateFrom));
    }

    if (filterDateTo) {
      filtered = filtered.filter(log => new Date(log.transactionDate) <= new Date(filterDateTo));
    }

    setFilteredLogs(filtered);
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this journal log?')) return;

    try {
      await journalService.deleteJournalLog(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete journal log');
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
    }).format(amount);
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Journal Logs</h2>
        <button
          onClick={() => navigate('/Property Hub/Journal Logs/New')}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          New Journal Log
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
            <label className="block text-sm font-medium text-gray-700 mb-1">Journal Type</label>
            <select
              value={filterJournalTypeId}
              onChange={(e) => setFilterJournalTypeId(e.target.value ? parseInt(e.target.value) : '')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">All Types</option>
              {journalTypes.map(t => (
                <option key={t.journalTypeId} value={t.journalTypeId}>{t.journalTypeName}</option>
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
        {(filterPropertyId || filterJournalTypeId || filterDateFrom || filterDateTo) && (
          <button
            onClick={() => {
              setFilterPropertyId('');
              setFilterJournalTypeId('');
              setFilterDateFrom('');
              setFilterDateTo('');
            }}
            className="mt-4 text-sm text-blue-600 hover:text-blue-800"
          >
            Clear Filters
          </button>
        )}
      </div>

      {/* Journal Logs Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tenant</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Amount</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filteredLogs.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-4 text-center text-sm text-gray-500">
                  No journal logs found
                </td>
              </tr>
            ) : (
              filteredLogs.map((log) => (
                <tr key={log.journalLogId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {new Date(log.transactionDate).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{log.propertyName}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{log.tenantName || '-'}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {log.journalTypeName}
                    {log.journalSubTypeName && <span className="text-gray-400"> - {log.journalSubTypeName}</span>}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {formatCurrency(log.amount)}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <div className="max-w-xs truncate">{log.description || '-'}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                    <button
                      onClick={() => navigate(`/Property Hub/Journal Logs/${log.journalLogId}`)}
                      className="text-blue-600 hover:text-blue-900"
                    >
                      View
                    </button>
                    <button
                      onClick={() => navigate(`/Property Hub/Journal Logs/${log.journalLogId}?edit=true`)}
                      className="text-green-600 hover:text-green-900"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(log.journalLogId)}
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

export default JournalLogsList;
