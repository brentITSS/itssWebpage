import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { journalService, CreateJournalLogRequest, UpdateJournalLogRequest, JournalTypeDto, JournalSubTypeDto, AttachmentDto } from '../../../services/journalService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import { propertyAdminService, TenantResponseDto, TenancyResponseDto } from '../../../services/propertyAdminService';
import { tagService, TagDto } from '../../../services/tagService';
import Tag from '../../../components/Tag';
import TagAssignmentModal from '../../../components/TagAssignmentModal';
import { formatDateUk } from '../../../dateFormat';

const JournalLogForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const journalLogId = id ? parseInt(id) : null;
  const contextPropertyIdParam = searchParams.get('propertyId');
  const contextReturnPropertyId = (() => {
    if (!contextPropertyIdParam) return null;
    const n = parseInt(contextPropertyIdParam, 10);
    return Number.isFinite(n) && n > 0 ? n : null;
  })();

  const exitFormNavigate = () => {
    if (contextReturnPropertyId != null) {
      navigate(`/Property Hub/Property/${contextReturnPropertyId}`);
      return;
    }
    navigate('/Property Hub/Journal Logs');
  };

  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [journalTypes, setJournalTypes] = useState<JournalTypeDto[]>([]);
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [tags, setTags] = useState<TagDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [showTagModal, setShowTagModal] = useState(false);
  const [currentLogId, setCurrentLogId] = useState<number | null>(journalLogId);
  const [propertyGroupFilterId, setPropertyGroupFilterId] = useState<number | undefined>(undefined);

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
    addToCalendar: false,
    calendarDate: undefined,
    trackingDataOnly: false,
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
        setAttachments(logData.attachments || []);
        setCurrentLogId(journalLogId);
        // Load tags for this journal log
        const tagData = await tagService.getTagLogsByEntity('JournalLog', journalLogId);
        setTags(tagData);
        setFormData({
          propertyId: logData.propertyId,
          tenancyId: logData.tenancyId || undefined,
          tenantId: logData.tenantId || undefined,
          journalTypeId: logData.journalTypeId,
          journalSubTypeId: logData.journalSubTypeId || undefined,
          amount: logData.amount,
          description: logData.description || '',
          transactionDate: new Date(logData.transactionDate).toISOString().split('T')[0],
          addToCalendar: !!logData.hasCalendarAppointment,
          calendarDate: logData.calendarDate ? new Date(logData.calendarDate).toISOString().split('T')[0] : undefined,
          trackingDataOnly: !!logData.trackingDataOnly,
        });
      } else if (contextReturnPropertyId != null) {
        setFormData((prev) => ({
          ...prev,
          propertyId: contextReturnPropertyId,
        }));
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  }, [isEdit, journalLogId, contextReturnPropertyId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    if (!formData.propertyId) return;
    const selectedProperty = properties.find((p) => p.propertyId === formData.propertyId);
    if (selectedProperty?.propertyGroupId && selectedProperty.propertyGroupId !== propertyGroupFilterId) {
      setPropertyGroupFilterId(selectedProperty.propertyGroupId);
    }
  }, [formData.propertyId, properties, propertyGroupFilterId]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      let savedLogId: number;
      // If we have a currentLogId (either from edit mode or after creation), update instead of create
      if (currentLogId) {
        const updateRequest: UpdateJournalLogRequest = {
          propertyId: formData.propertyId,
          tenancyId: formData.tenancyId,
          tenantId: formData.tenantId,
          journalTypeId: formData.journalTypeId,
          journalSubTypeId: formData.journalSubTypeId,
          amount: formData.amount,
          description: formData.description || undefined,
          transactionDate: formData.transactionDate,
          addToCalendar: !!formData.addToCalendar,
          calendarDate: formData.addToCalendar ? (formData.calendarDate || formData.transactionDate) : undefined,
          trackingDataOnly: !!formData.trackingDataOnly,
        };
        await journalService.updateJournalLog(currentLogId, updateRequest);
        savedLogId = currentLogId;
      } else {
        const createdLog = await journalService.createJournalLog({
          ...formData,
          addToCalendar: !!formData.addToCalendar,
          calendarDate: formData.addToCalendar ? (formData.calendarDate || formData.transactionDate) : undefined,
          trackingDataOnly: !!formData.trackingDataOnly,
        });
        savedLogId = createdLog.journalLogId;
        setCurrentLogId(savedLogId);
        // Load the created log to get attachments and tags
        const logData = await journalService.getJournalLog(savedLogId);
        setAttachments(logData.attachments || []);
        const tagData = await tagService.getTagLogsByEntity('JournalLog', savedLogId);
        setTags(tagData);
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

  const handleTagAdded = async () => {
    if (!currentLogId) return;
    try {
      const tagData = await tagService.getTagLogsByEntity('JournalLog', currentLogId);
      setTags(tagData);
    } catch (err: any) {
      setError(err.message || 'Failed to refresh tags');
    }
  };

  const handleRemoveTag = async (tagLogId: number) => {
    if (!window.confirm('Are you sure you want to remove this tag?')) return;

    try {
      await tagService.deleteTagLog(tagLogId);
      setTags(tags.filter(t => t.tagLogId !== tagLogId));
    } catch (err: any) {
      setError(err.message || 'Failed to remove tag');
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  const selectedJournalType = journalTypes.find(t => t.journalTypeId === formData.journalTypeId);
  const availableSubTypes = selectedJournalType?.subTypes || [];
  const propertyGroups = Array.from(
    new Map(
      properties
        .filter((p) => p.propertyGroupId > 0)
        .map((p) => [p.propertyGroupId, { propertyGroupId: p.propertyGroupId, propertyGroupName: p.propertyGroupName }])
    ).values()
  );
  const propertiesForGroup = propertyGroupFilterId
    ? properties.filter((p) => p.propertyGroupId === propertyGroupFilterId)
    : properties;
  
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
              Property Group
            </label>
            <select
              value={propertyGroupFilterId ?? ''}
              onChange={(e) => {
                const nextGroupId = e.target.value ? parseInt(e.target.value, 10) : undefined;
                setPropertyGroupFilterId(nextGroupId);
                setFormData({
                  ...formData,
                  propertyId: 0,
                  tenancyId: undefined,
                  tenantId: undefined,
                });
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">All property groups</option>
              {propertyGroups.map((g) => (
                <option key={g.propertyGroupId} value={g.propertyGroupId}>
                  {g.propertyGroupName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Property *
            </label>
            <select
              value={formData.propertyId}
              onChange={(e) => {
                const newPropertyId = parseInt(e.target.value);
                const selectedProperty = properties.find((p) => p.propertyId === newPropertyId);
                setFormData({
                  ...formData,
                  propertyId: newPropertyId,
                  tenancyId: undefined, // Reset when property changes
                  tenantId: undefined,
                });
                setPropertyGroupFilterId(selectedProperty?.propertyGroupId);
              }}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="0">Select Property</option>
              {propertiesForGroup.map(p => (
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

        <div className="mt-6 rounded-md border border-slate-200 bg-slate-50 p-4">
          <label className="flex items-center gap-2 text-sm font-medium text-gray-700">
            <input
              type="checkbox"
              checked={!!formData.trackingDataOnly}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  trackingDataOnly: e.target.checked,
                }))
              }
            />
            Tracking data only
          </label>
          <p className="mt-2 text-xs text-slate-600">
            When enabled, this entry is for tracking only and will be excluded from financial reports.
          </p>
        </div>

        <div className="mt-6 rounded-md border border-slate-200 bg-slate-50 p-4">
          <label className="flex items-center gap-2 text-sm font-medium text-gray-700">
            <input
              type="checkbox"
              checked={!!formData.addToCalendar}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  addToCalendar: e.target.checked,
                  calendarDate: e.target.checked ? prev.calendarDate || prev.transactionDate : undefined,
                }))
              }
            />
            Add to calendar
          </label>
          {!!formData.addToCalendar && (
            <div className="mt-3">
              <label className="mb-1 block text-sm font-medium text-gray-700">Calendar date</label>
              <input
                type="date"
                value={formData.calendarDate || ''}
                onChange={(e) => setFormData({ ...formData, calendarDate: e.target.value || undefined })}
                className="w-full rounded-md border border-gray-300 px-3 py-2 md:max-w-xs"
                required
              />
              {currentLogId && (
                <p className="mt-2 text-xs text-slate-600">
                  This journal log has a linked calendar appointment; saving updates that appointment.
                </p>
              )}
            </div>
          )}
        </div>

        {/* Tags Section - Only show if log exists */}
        {currentLogId && (
          <div className="mt-6 pt-6 border-t border-gray-200">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold">Tags</h3>
              <button
                type="button"
                onClick={() => setShowTagModal(true)}
                className="px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700"
              >
                Add Tag
              </button>
            </div>
            {tags.length === 0 ? (
              <p className="text-sm text-gray-500">No tags assigned</p>
            ) : (
              <div className="flex flex-wrap gap-2 mb-4">
                {tags.map((tag) => (
                  <Tag key={tag.tagLogId} tag={tag} onRemove={handleRemoveTag} />
                ))}
              </div>
            )}
          </div>
        )}

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
                        {formatFileSize(attachment.fileSize)} • {formatDateUk(attachment.createdDate)}
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
            onClick={exitFormNavigate}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
          >
            {contextReturnPropertyId != null ? 'Property overview' : currentLogId ? 'Done' : 'Cancel'}
          </button>
          <button
            type="submit"
            disabled={saving}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Saving...' : currentLogId ? 'Update' : 'Create'}
          </button>
        </div>
      </form>

      {/* Tag Assignment Modal */}
      {currentLogId && (
        <TagAssignmentModal
          isOpen={showTagModal}
          onClose={() => setShowTagModal(false)}
          entityType="JournalLog"
          entityId={currentLogId}
          existingTagTypeIds={tags.map(t => t.tagTypeId)}
          onTagAdded={handleTagAdded}
        />
      )}
    </div>
  );
};

export default JournalLogForm;
