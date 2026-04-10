import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  maintenanceService,
  CreateMaintenanceRequest,
  UpdateMaintenanceRequest,
} from '../../../services/maintenanceService';
import { propertyService, PropertyResponseDto, PropertyGroupResponseDto } from '../../../services/propertyService';

const MaintenanceForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const recordId = id ? parseInt(id, 10) : null;

  const [groups, setGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [types, setTypes] = useState<{ maintenanceTypeId: number; maintenanceTypeName: string }[]>([]);
  const [statuses, setStatuses] = useState<{ maintenanceStatusId: number; maintenanceStatusName: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [formData, setFormData] = useState<CreateMaintenanceRequest>({
    propertyGroupId: 0,
    propertyId: 0,
    maintenanceTypeId: 0,
    maintenanceStatusId: undefined,
    summary: '',
    detailNotes: '',
    workDate: undefined,
  });

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [g, p, t, s] = await Promise.all([
        propertyService.getPropertyGroups(),
        propertyService.getProperties(),
        maintenanceService.getMaintenanceTypes(),
        maintenanceService.getMaintenanceStatuses(),
      ]);
      setGroups(g);
      setProperties(p);
      setTypes(t);
      setStatuses(s.filter((x) => x.isActive !== false));

      if (isEdit && recordId) {
        const r = await maintenanceService.getMaintenanceRecord(recordId);
        setFormData({
          propertyGroupId: r.propertyGroupId,
          propertyId: r.propertyId,
          maintenanceTypeId: r.maintenanceTypeId,
          maintenanceStatusId: r.maintenanceStatusId,
          summary: r.summary || '',
          detailNotes: r.detailNotes || '',
          workDate: r.workDate ? new Date(r.workDate).toISOString().split('T')[0] : undefined,
        });
      } else if (g.length && p.length) {
        const firstG = g[0].propertyGroupId;
        const firstP = p.find((x) => x.propertyGroupId === firstG) || p[0];
        setFormData((prev) => ({
          ...prev,
          propertyGroupId: firstG,
          propertyId: firstP.propertyId,
          maintenanceTypeId: t[0]?.maintenanceTypeId ?? 0,
        }));
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load');
    } finally {
      setLoading(false);
    }
  }, [isEdit, recordId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const propertiesForGroup = properties.filter((p) => p.propertyGroupId === formData.propertyGroupId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.propertyGroupId || !formData.propertyId || !formData.maintenanceTypeId) {
      setError('Property group, property, and maintenance type are required.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      if (recordId) {
        const upd: UpdateMaintenanceRequest = {
          propertyGroupId: formData.propertyGroupId,
          propertyId: formData.propertyId,
          maintenanceTypeId: formData.maintenanceTypeId,
          maintenanceStatusId: formData.maintenanceStatusId,
          summary: formData.summary,
          detailNotes: formData.detailNotes,
          workDate: formData.workDate || undefined,
        };
        await maintenanceService.updateMaintenanceRecord(recordId, upd);
        navigate(`/Property Hub/Maintenance/${recordId}`);
      } else {
        const created = await maintenanceService.createMaintenanceRecord({
          ...formData,
          workDate: formData.workDate || undefined,
        });
        navigate(`/Property Hub/Maintenance/${created.maintenanceId}`);
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
        <h2 className="text-2xl font-bold text-gray-900">{recordId ? 'Edit maintenance' : 'New maintenance'}</h2>
        <button
          type="button"
          onClick={() => navigate('/Property Hub/Maintenance')}
          className="text-gray-600 hover:text-gray-900"
        >
          Back to list
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">{error}</div>
      )}

      <form onSubmit={handleSubmit} className="rounded-lg bg-white p-6 shadow">
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Property group *</label>
            <select
              required
              value={formData.propertyGroupId || ''}
              onChange={(e) => {
                const gid = parseInt(e.target.value, 10);
                const firstP = properties.find((p) => p.propertyGroupId === gid);
                setFormData({
                  ...formData,
                  propertyGroupId: gid,
                  propertyId: firstP?.propertyId ?? 0,
                });
              }}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            >
              <option value="">Select…</option>
              {groups.map((g) => (
                <option key={g.propertyGroupId} value={g.propertyGroupId}>
                  {g.propertyGroupName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Property *</label>
            <select
              required
              value={formData.propertyId || ''}
              onChange={(e) => setFormData({ ...formData, propertyId: parseInt(e.target.value, 10) })}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            >
              <option value="">Select…</option>
              {propertiesForGroup.map((p) => (
                <option key={p.propertyId} value={p.propertyId}>
                  {p.propertyName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Maintenance type *</label>
            <select
              required
              value={formData.maintenanceTypeId || ''}
              onChange={(e) =>
                setFormData({ ...formData, maintenanceTypeId: parseInt(e.target.value, 10) })
              }
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            >
              <option value="">Select…</option>
              {types.map((t) => (
                <option key={t.maintenanceTypeId} value={t.maintenanceTypeId}>
                  {t.maintenanceTypeName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Status</label>
            <select
              value={formData.maintenanceStatusId ?? ''}
              onChange={(e) =>
                setFormData({
                  ...formData,
                  maintenanceStatusId: e.target.value ? parseInt(e.target.value, 10) : undefined,
                })
              }
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            >
              <option value="">— None —</option>
              {statuses.map((s) => (
                <option key={s.maintenanceStatusId} value={s.maintenanceStatusId}>
                  {s.maintenanceStatusName}
                </option>
              ))}
            </select>
          </div>
          <div className="md:col-span-2">
            <label className="mb-1 block text-sm font-medium text-gray-700">Summary</label>
            <input
              type="text"
              value={formData.summary}
              onChange={(e) => setFormData({ ...formData, summary: e.target.value })}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div className="md:col-span-2">
            <label className="mb-1 block text-sm font-medium text-gray-700">Details</label>
            <textarea
              rows={5}
              value={formData.detailNotes}
              onChange={(e) => setFormData({ ...formData, detailNotes: e.target.value })}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Work date</label>
            <input
              type="date"
              value={formData.workDate || ''}
              onChange={(e) => setFormData({ ...formData, workDate: e.target.value || undefined })}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div className="flex items-end gap-2 pb-1 md:col-span-2">
            <button
              type="submit"
              disabled={saving}
              className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {saving ? 'Saving…' : 'Save'}
            </button>
            <button
              type="button"
              onClick={() => navigate('/Property Hub/Maintenance')}
              className="rounded-md border border-gray-300 px-4 py-2"
            >
              Cancel
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};

export default MaintenanceForm;
