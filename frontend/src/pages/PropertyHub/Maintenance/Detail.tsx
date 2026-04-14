import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { maintenanceService, MaintenanceResponseDto } from '../../../services/maintenanceService';
import MaintenanceForm from './Form';
import { formatDateUk } from '../../../dateFormat';

const MaintenanceDetail: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = searchParams.get('edit') === 'true';

  const [row, setRow] = useState<MaintenanceResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const r = await maintenanceService.getMaintenanceRecord(parseInt(id, 10));
      setRow(r);
    } catch (err: any) {
      setError(err.message || 'Failed to load');
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
    return <MaintenanceForm />;
  }

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  if (error || !row) {
    return (
      <div className="text-center py-8 text-red-600">
        {error || 'Not found'}
        <div className="mt-4">
          <button type="button" onClick={() => navigate('/Property Hub/Maintenance')} className="text-blue-600">
            Back to list
          </button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">{row.summary || 'Maintenance record'}</h2>
        <div className="space-x-2">
          <button
            type="button"
            onClick={() => navigate(`/Property Hub/Maintenance/${row.maintenanceId}?edit=true`)}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Edit
          </button>
          <button
            type="button"
            onClick={() => navigate('/Property Hub/Maintenance')}
            className="px-4 py-2 border border-gray-300 rounded-md"
          >
            Back
          </button>
        </div>
      </div>

      <div className="bg-white shadow rounded-lg p-6 max-w-2xl space-y-3 text-sm">
        <p>
          <span className="font-medium text-gray-700">Type: </span>
          {row.maintenanceTypeName}
        </p>
        {row.maintenanceStatusName && (
          <p>
            <span className="font-medium text-gray-700">Status: </span>
            {row.maintenanceStatusName}
          </p>
        )}
        <p>
          <span className="font-medium text-gray-700">Property group: </span>
          {row.propertyGroupName}
        </p>
        <p>
          <span className="font-medium text-gray-700">Property: </span>
          {row.propertyName || 'Group-wide'}
        </p>
        <p>
          <span className="font-medium text-gray-700">Work date: </span>
          {row.workDate ? formatDateUk(row.workDate) : '—'}
        </p>
        {row.detailNotes && (
          <div>
            <p className="font-medium text-gray-700 mb-1">Details</p>
            <p className="text-gray-600 whitespace-pre-wrap">{row.detailNotes}</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default MaintenanceDetail;
