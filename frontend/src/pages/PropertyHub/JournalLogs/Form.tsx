import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { journalService, CreateJournalLogRequest, UpdateJournalLogRequest, JournalLogResponseDto, JournalTypeDto, JournalSubTypeDto, AttachmentDto } from '../../../services/journalService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import { propertyAdminService, TenantResponseDto, TenancyResponseDto } from '../../../services/propertyAdminService';

const JournalLogForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const journalLogId = id ? parseInt(id) : null;

  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [journalTypes, setJournalTypes] = useState<JournalTypeDto[]>([]);
  const [journalLog, setJournalLog] = useState<JournalLogResponseDto | null>(null);
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [currentLogId, setCurrentLogId] = useState<number | null>(journalLogId);

  // Form state
  const [formData, setFormData] = useState<CreateJournalLogRequest>({
    propertyId: 0,
    tenancyId: undefined,
    tenantId: undefined,
    journalTypeId: 0,
    journalSubTypeId: undefined,
    amount: 0,
    description: '',
    transactionDate: new Date().toISOString().split('T')[0],
  });

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [propertiesData, tenantsData, tenanciesData, typesData] = await Promise.all([
        propertyService.getProperties(),
        propertyAdminService.getTenants(),
        propertyAdminService.getTenancies(),
        journalService.getJournalTypes(),
      ]);

      setProperties(propertiesData);
      setTenants(tenantsData);
      setTenancies(tenanciesData);
      setJournalTypes(typesData);

      if (isEdit && journalLogId) {
        const logData = await journalService.getJournalLog(journalLogId);
        setJournalLog(logData);
        setAttachments(logData.attachments || []);
        setCurrentLogId(journalLogId);
        setFormData({
          propertyId: logData.propertyId,
          tenancyId: logData.tenancyId || undefined,
          tenantId: logData.tenantId || undefined,
          journalTypeId: logData.journalTypeId,
          journalSubTypeId: logData.journalSubTypeId || undefined,
          amount: logData.amount,
          description: logData.description || '',
          transactionDate: new Date(logData.transactionDate).toISOString().split('T')[0],
        });
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  }, [isEdit, journalLogId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      let savedLogId: number;
      if (isEdit && journalLogId) {
        const updateRequest: UpdateJournalLogRequest = {
          propertyId: formData.propertyId,
          tenancyId: formData.tenancyId,
          tenantId: formData.tenantId,
          journalTypeId: formData.journalTypeId,
          journalSubTypeId: formData.journalSubTypeId,
          amount: formData.amount,
          description: formData.description || undefined,
          transactionDate: formData.transactionDate,
        };
        await journalService.updateJournalLog(journalLogId, updateRequest);
        savedLogId = journalLogId;
      } else {
        const createdLog = await journalService.createJournalLog(formData);
        savedLogId = createdLog.journalLogId;
        setCurrentLogId(savedLogId);
        // Load the created log to get attachments
        const logData = await journalService.getJournalLog(savedLogId);
        setJournalLog(logData);
        setAttachments(logData.attachments || []);
      }
      // Don't navigate away - stay on form to allow attachment uploads
    } catch (err: any) {
      setError(err.message || 'Failed to save journal log');
    } finally {
      setSaving(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !currentLogId) return;

    setUploading(true);
    setError(null);

    try {
      const attachment = await journalService.addAttachment(currentLogId, file);
      setAttachments([...attachments, attachment]);
      e.target.value = ''; // Reset file input
    } catch (err: any) {
      setError(err.message || 'Failed to upload attachment');
    } finally {
      setUploading(false);
    }
  };

  const handleDeleteAttachment = async (attachmentId: number) => {
    if (!window.confirm('Are you sure you want to delete this attachment?')) return;

    try {
      await journalService.deleteAttachment(attachmentId);
      setAttachments(attachments.filter(a => a.attachmentId !== attachmentId));
    } catch (err: any) {
      setError(err.message || 'Failed to delete attachment');
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  const selectedJournalType = journalTypes.find(t => t.journalTypeId === formData.journalTypeId);
  const availableSubTypes = selectedJournalType?.subTypes || [];
  
  // Filter tenancies by selected property
  const availableTenancies = formData.propertyId
    ? tenancies.filter(t => t.propertyId === formData.propertyId)
    : [];

  // Filter tenants by selected property through tenancies
  const availableTenants = formData.propertyId
    ? tenants.filter(t => availableTenancies.some(ten => ten.tenants.some(tenant => tenant.tenantId === t.tenantId)))
    : tenants;

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900">
          {isEdit ? 'Edit Journal Log' : 'New Journal Log'}
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
              onChange={(e) => {
                const newPropertyId = parseInt(e.target.value);
                setFormData({
                  ...formData,
                  propertyId: newPropertyId,
                  tenancyId: undefined, // Reset when property changes
                  tenantId: undefined,
                });
              }}
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
              Transaction Date *
            </label>
            <input
              type="date"
              value={formData.transactionDate}
              onChange={(e) => setFormData({ ...formData, transactionDate: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Tenancy (Optional)
            </label>
            <select
              value={formData.tenancyId || ''}
              onChange={(e) => {
                const tenancyId = e.target.value ? parseInt(e.target.value) : undefined;
                const selectedTenancy = availableTenancies.find(t => t.tenancyId === tenancyId);
                // Set first tenant if available, otherwise undefined
                const firstTenantId = selectedTenancy?.tenants && selectedTenancy.tenants.length > 0 ? selectedTenancy.tenants[0].tenantId : undefined;
                setFormData({
                  ...formData,
                  tenancyId,
                  tenantId: firstTenantId,
                });
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
              disabled={!formData.propertyId}
            >
              <option value="">None</option>
              {availableTenancies.map(t => {
                const tenantNames = t.tenants.length > 0 
                  ? t.tenants.map(tenant => `${tenant.firstName} ${tenant.lastName}`).join(', ')
                  : 'No tenants';
                return (
                  <option key={t.tenancyId} value={t.tenancyId}>
                    {t.propertyName} - {tenantNames}
                  </option>
                );
              })}
            </select>
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
              disabled={!formData.propertyId}
            >
              <option value="">None</option>
              {availableTenants.map(t => (
                <option key={t.tenantId} value={t.tenantId}>
                  {t.firstName} {t.lastName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Journal Type *
            </label>
            <select
              value={formData.journalTypeId}
              onChange={(e) => {
                const newTypeId = parseInt(e.target.value);
                setFormData({
                  ...formData,
                  journalTypeId: newTypeId,
                  journalSubTypeId: undefined, // Reset subtype when type changes
                });
              }}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="0">Select Journal Type</option>
              {journalTypes.map((t: JournalTypeDto) => (
                <option key={t.journalTypeId} value={t.journalTypeId}>{t.journalTypeName}</option>
              ))}
            </select>
          </div>

          {availableSubTypes.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Journal SubType (Optional)
              </label>
              <select
                value={formData.journalSubTypeId || ''}
                onChange={(e) => setFormData({
                  ...formData,
                  journalSubTypeId: e.target.value ? parseInt(e.target.value) : undefined,
                })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
              >
                <option value="">None</option>
                {availableSubTypes.map((st: JournalSubTypeDto) => (
                  <option key={st.journalSubTypeId} value={st.journalSubTypeId}>
                    {st.journalSubTypeName}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Amount *
            </label>
            <input
              type="number"
              step="0.01"
              value={formData.amount}
              onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>

        <div className="mt-6">
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Description
          </label>
          <textarea
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            rows={4}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>

        {/* Attachments Section - Only show if log exists */}
        {currentLogId && (
          <div className="mt-6 pt-6 border-t border-gray-200">
            <h3 className="text-lg font-semibold mb-4">Attachments</h3>
            
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Upload Attachment
              </label>
              <input
                type="file"
                onChange={handleFileUpload}
                disabled={uploading}
                className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
              />
              {uploading && <p className="mt-2 text-sm text-gray-500">Uploading...</p>}
            </div>

            {attachments.length === 0 ? (
              <p className="text-sm text-gray-500">No attachments</p>
            ) : (
              <div className="space-y-2">
                {attachments.map((attachment) => (
                  <div
                    key={attachment.attachmentId}
                    className="flex items-center justify-between p-3 border border-gray-200 rounded-md hover:bg-gray-50"
                  >
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">{attachment.fileName}</p>
                      <p className="text-xs text-gray-500">
                        {formatFileSize(attachment.fileSize)} • {new Date(attachment.createdDate).toLocaleDateString()}
                      </p>
                    </div>
                    <button
                      onClick={() => handleDeleteAttachment(attachment.attachmentId)}
                      className="text-sm text-red-600 hover:text-red-800"
                    >
                      Delete
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="mt-6 flex justify-end space-x-4">
          <button
            type="button"
            onClick={() => navigate('/Property Hub/Journal Logs')}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
          >
            {currentLogId ? 'Done' : 'Cancel'}
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

export default JournalLogForm;
