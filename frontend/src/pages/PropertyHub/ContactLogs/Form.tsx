import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { contactLogService, CreateContactLogRequest, UpdateContactLogRequest, ContactLogResponseDto, AttachmentDto } from '../../../services/contactLogService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import { propertyAdminService, TenantResponseDto, TenancyResponseDto } from '../../../services/propertyAdminService';
import { tagService, TagDto } from '../../../services/tagService';
import Tag from '../../../components/Tag';
import TagAssignmentModal from '../../../components/TagAssignmentModal';

const ContactLogForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = !!id || searchParams.get('edit') === 'true';
  const contactLogId = id ? parseInt(id) : null;

  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<any[]>([]);
  const [contactLog, setContactLog] = useState<ContactLogResponseDto | null>(null);
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [tags, setTags] = useState<TagDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [showTagModal, setShowTagModal] = useState(false);
  const [currentLogId, setCurrentLogId] = useState<number | null>(contactLogId);

  // Form state
  const [formData, setFormData] = useState<CreateContactLogRequest>({
    propertyId: 0,
    tenantId: undefined,
    contactLogTypeId: 0,
    subject: '',
    notes: '',
    contactDate: new Date().toISOString().split('T')[0],
  });

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [propertiesData, tenantsData, tenanciesData, typesData] = await Promise.all([
        propertyService.getProperties(),
        propertyAdminService.getTenants(),
        propertyAdminService.getTenancies(),
        contactLogService.getContactLogTypes(),
      ]);

      setProperties(propertiesData);
      setTenants(tenantsData);
      setTenancies(tenanciesData);
      setContactLogTypes(typesData);

      if (isEdit && contactLogId) {
        const logData = await contactLogService.getContactLog(contactLogId);
        setContactLog(logData);
        setAttachments(logData.attachments || []);
        setCurrentLogId(contactLogId);
        // Load tags for this contact log
        const tagData = await tagService.getTagLogsByEntity('ContactLog', contactLogId);
        setTags(tagData);
        setFormData({
          propertyId: logData.propertyId,
          tenantId: logData.tenantId || undefined,
          contactLogTypeId: logData.contactLogTypeId,
          subject: logData.subject,
          notes: logData.notes,
          contactDate: new Date(logData.contactDate).toISOString().split('T')[0],
        });
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  }, [isEdit, contactLogId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      let savedLogId: number;
      if (isEdit && contactLogId) {
        const updateRequest: UpdateContactLogRequest = {
          propertyId: formData.propertyId,
          tenantId: formData.tenantId,
          contactLogTypeId: formData.contactLogTypeId,
          subject: formData.subject,
          notes: formData.notes,
          contactDate: formData.contactDate,
        };
        await contactLogService.updateContactLog(contactLogId, updateRequest);
        savedLogId = contactLogId;
      } else {
        const createdLog = await contactLogService.createContactLog(formData);
        savedLogId = createdLog.contactLogId;
        setCurrentLogId(savedLogId);
        // Load the created log to get attachments and tags
        const logData = await contactLogService.getContactLog(savedLogId);
        setContactLog(logData);
        setAttachments(logData.attachments || []);
        const tagData = await tagService.getTagLogsByEntity('ContactLog', savedLogId);
        setTags(tagData);
      }
      // Don't navigate away - stay on form to allow attachment uploads
    } catch (err: any) {
      setError(err.message || 'Failed to save contact log');
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
      const attachment = await contactLogService.addAttachment(currentLogId, file);
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
      await contactLogService.deleteAttachment(attachmentId);
      setAttachments(attachments.filter(a => a.attachmentId !== attachmentId));
    } catch (err: any) {
      setError(err.message || 'Failed to delete attachment');
    }
  };

  const handleTagAdded = async () => {
    if (!currentLogId) return;
    try {
      const tagData = await tagService.getTagLogsByEntity('ContactLog', currentLogId);
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

  // Filter tenancies by selected property
  const availableTenancies = formData.propertyId
    ? tenancies.filter(t => t.propertyId === formData.propertyId)
    : [];

  // Filter tenants by selected property through tenancies
  const availableTenants = formData.propertyId
    ? tenants.filter(t => availableTenancies.some(ten => ten.tenants.some(tenant => tenant.tenantId === t.tenantId)))
    : [];

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900">
          {isEdit ? 'Edit Contact Log' : 'New Contact Log'}
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
              onChange={(e) => setFormData({
                ...formData,
                propertyId: parseInt(e.target.value),
                tenantId: undefined, // Reset tenant when property changes
              })}
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
              Contact Date *
            </label>
            <input
              type="date"
              value={formData.contactDate}
              onChange={(e) => setFormData({ ...formData, contactDate: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
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
              disabled={!formData.propertyId || formData.propertyId === 0}
            >
              <option value="">None</option>
              {availableTenants.map(t => (
                <option key={t.tenantId} value={t.tenantId}>
                  {t.firstName} {t.lastName}
                </option>
              ))}
            </select>
            {(!formData.propertyId || formData.propertyId === 0) && (
              <p className="mt-1 text-xs text-gray-500">Please select a property first</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Contact Log Type *
            </label>
            <select
              value={formData.contactLogTypeId}
              onChange={(e) => setFormData({ ...formData, contactLogTypeId: parseInt(e.target.value) })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="0">Select Contact Log Type</option>
              {contactLogTypes.map(t => (
                <option key={t.contactLogTypeId} value={t.contactLogTypeId}>{t.contactLogTypeName}</option>
              ))}
            </select>
          </div>

          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Subject *
            </label>
            <input
              type="text"
              value={formData.subject}
              onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>

        <div className="mt-6">
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Notes *
          </label>
          <textarea
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            rows={6}
            required
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
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
            onClick={() => navigate('/Property Hub/Contact Logs')}
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

      {/* Tag Assignment Modal */}
      {currentLogId && (
        <TagAssignmentModal
          isOpen={showTagModal}
          onClose={() => setShowTagModal(false)}
          entityType="ContactLog"
          entityId={currentLogId}
          existingTagTypeIds={tags.map(t => t.tagTypeId)}
          onTagAdded={handleTagAdded}
        />
      )}
    </div>
  );
};

export default ContactLogForm;
