import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { journalService, JournalLogResponseDto } from '../../../services/journalService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import { formatDateUk } from '../../../dateFormat';
import EntityActionButtons from '../../../components/EntityActionButtons';

const JournalLogsList: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [journalLogs, setJournalLogs] = useState<JournalLogResponseDto[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<JournalLogResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [journalTypes, setJournalTypes] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters - initialize from URL query parameter if present
  const propertyIdFromUrl = searchParams.get('propertyId');
  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>(propertyIdFromUrl ? parseInt(propertyIdFromUrl) : '');
  const [filterJournalTypeId, setFilterJournalTypeId] = useState<number | ''>('');
  const [filterDateFrom, setFilterDateFrom] = useState<string>('');
  const [filterDateTo, setFilterDateTo] = useState<string>('');
  const [showFilters, setShowFilters] = useState(false);

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

  const applyFilters = useCallback(() => {
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
  }, [journalLogs, filterPropertyId, filterJournalTypeId, filterDateFrom, filterDateTo]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

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
    return new Intl.NumberFormat('en-ZA', {
      style: 'currency',
      currency: 'ZAR',
    }).format(amount);
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Journal Logs</h2>
        <button
          onClick={() => navigate('/Property Hub/Journal Logs/New')}
          className="w-full rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 sm:w-auto"
        >
          New Journal Log
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <div className="mb-6 rounded-lg bg-white p-4 shadow">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium text-gray-700">Filters</h3>
          <button
            type="button"
            onClick={() => setShowFilters((prev) => !prev)}
            className="text-sm font-medium text-blue-600 hover:text-blue-800"
            aria-expanded={showFilters}
          >
            {showFilters ? 'Hide filters' : 'Show filters'}
          </button>
        </div>
        {showFilters && (
          <>
            <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-4">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Property</label>
                <select
                  value={filterPropertyId === '' ? '' : filterPropertyId.toString()}
                  onChange={(e) => setFilterPropertyId(e.target.value ? parseInt(e.target.value) : '')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  <option value="">All Properties</option>
                  {properties.map(p => (
                    <option key={p.propertyId} value={p.propertyId}>{p.propertyName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Journal Type</label>
                <select
                  value={filterJournalTypeId}
                  onChange={(e) => setFilterJournalTypeId(e.target.value ? parseInt(e.target.value) : '')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  <option value="">All Types</option>
                  {journalTypes.map(t => (
                    <option key={t.journalTypeId} value={t.journalTypeId}>{t.journalTypeName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Date From</label>
                <input
                  type="date"
                  value={filterDateFrom}
                  onChange={(e) => setFilterDateFrom(e.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Date To</label>
                <input
                  type="date"
                  value={filterDateTo}
                  onChange={(e) => setFilterDateTo(e.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
            </div>
            {(filterPropertyId || filterJournalTypeId || filterDateFrom || filterDateTo) && (
              <button
                type="button"
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
          </>
        )}
      </div>

      {/* Journal Logs Table */}
      <div className="rounded-lg bg-white shadow">
        <div className="overflow-x-auto">
          <table className="min-w-[1040px] w-full divide-y divide-gray-200">
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
                <tr
                  key={log.journalLogId}
                  className="cursor-pointer hover:bg-gray-50"
                  onClick={() => navigate(`/Property Hub/Journal Logs/${log.journalLogId}`)}
                >
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {formatDateUk(log.transactionDate)}
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
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <EntityActionButtons
                      compact
                      onView={() => navigate(`/Property Hub/Journal Logs/${log.journalLogId}`)}
                      onEdit={() => navigate(`/Property Hub/Journal Logs/${log.journalLogId}?edit=true`)}
                      onDelete={() => handleDelete(log.journalLogId)}
                    />
                  </td>
                </tr>
              ))
            )}
          </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default JournalLogsList;
