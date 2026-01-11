import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { journalService, JournalLogResponseDto } from '../../../services/journalService';
import { tagService, TagDto } from '../../../services/tagService';
import Tag from '../../../components/Tag';
import TagAssignmentModal from '../../../components/TagAssignmentModal';
import JournalLogForm from './Form';

const JournalLogDetail: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEdit = searchParams.get('edit') === 'true';

  const [journalLog, setJournalLog] = useState<JournalLogResponseDto | null>(null);
  const [tags, setTags] = useState<TagDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [showTagModal, setShowTagModal] = useState(false);

  const loadJournalLog = useCallback(async () => {
    if (!id) return;

    try {
      setLoading(true);
      setError(null);
      const [logData, tagData] = await Promise.all([
        journalService.getJournalLog(parseInt(id)),
        tagService.getTagLogsByEntity('JournalLog', parseInt(id)),
      ]);
      setJournalLog(logData);
      setTags(tagData);
    } catch (err: any) {
      setError(err.message || 'Failed to load journal log');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    if (id && !isEdit) {
      loadJournalLog();
    }
  }, [id, isEdit, loadJournalLog]);

  // If edit mode, show form instead
  if (isEdit) {
    return <JournalLogForm />;
  }

  const handleTagAdded = async () => {
    if (!id) return;
    try {
      const tagData = await tagService.getTagLogsByEntity('JournalLog', parseInt(id));
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

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !id) return;

    setUploading(true);
    setError(null);

    try {
      await journalService.addAttachment(parseInt(id), file);
      loadJournalLog(); // Reload to get updated attachments
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
      loadJournalLog();
    } catch (err: any) {
      setError(err.message || 'Failed to delete attachment');
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
    }).format(amount);
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  if (!journalLog) {
    return (
      <div className="text-center py-8">
        <p className="text-gray-500">Journal log not found</p>
        <button
          onClick={() => navigate('/Property Hub/Journal Logs')}
          className="mt-4 text-blue-600 hover:text-blue-800"
        >
          Back to Journal Logs
        </button>
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <button
            onClick={() => navigate('/Property Hub/Journal Logs')}
            className="text-gray-600 hover:text-gray-900 mb-2"
          >
            ← Back to Journal Logs
          </button>
          <h2 className="text-2xl font-bold text-gray-900">Journal Log Details</h2>
        </div>
        <div className="space-x-2">
          <button
            onClick={() => navigate(`/Property Hub/Journal Logs/${id}?edit=true`)}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Edit
          </button>
          <button
            onClick={async () => {
              if (!window.confirm('Are you sure you want to delete this journal log?')) return;
              try {
                await journalService.deleteJournalLog(parseInt(id!));
                navigate('/Property Hub/Journal Logs');
              } catch (err: any) {
                setError(err.message || 'Failed to delete journal log');
              }
            }}
            className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
          >
            Delete
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Journal Log Details */}
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-semibold mb-4">Transaction Details</h3>
          <dl className="grid grid-cols-1 gap-4">
            <div>
              <dt className="text-sm font-medium text-gray-500">Transaction Date</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {new Date(journalLog.transactionDate).toLocaleDateString()}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Property</dt>
              <dd className="mt-1 text-sm text-gray-900">{journalLog.propertyName}</dd>
            </div>
            {journalLog.tenantName && (
              <div>
                <dt className="text-sm font-medium text-gray-500">Tenant</dt>
                <dd className="mt-1 text-sm text-gray-900">{journalLog.tenantName}</dd>
              </div>
            )}
            <div>
              <dt className="text-sm font-medium text-gray-500">Journal Type</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {journalLog.journalTypeName}
                {journalLog.journalSubTypeName && (
                  <span className="text-gray-600"> - {journalLog.journalSubTypeName}</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Amount</dt>
              <dd className="mt-1 text-sm font-semibold text-gray-900">
                {formatCurrency(journalLog.amount)}
              </dd>
            </div>
            {journalLog.description && (
              <div>
                <dt className="text-sm font-medium text-gray-500">Description</dt>
                <dd className="mt-1 text-sm text-gray-900 whitespace-pre-wrap">{journalLog.description}</dd>
              </div>
            )}
            <div>
              <dt className="text-sm font-medium text-gray-500">Created Date</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {new Date(journalLog.createdDate).toLocaleString()}
              </dd>
            </div>
          </dl>

          {/* Tags Section */}
          <div className="mt-6 pt-6 border-t border-gray-200">
            <div className="flex justify-between items-center mb-4">
              <h4 className="text-sm font-medium text-gray-700">Tags</h4>
              <button
                onClick={() => setShowTagModal(true)}
                className="px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700"
              >
                Add Tag
              </button>
            </div>
            {tags.length === 0 ? (
              <p className="text-sm text-gray-500">No tags assigned</p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {tags.map((tag) => (
                  <Tag key={tag.tagLogId} tag={tag} onRemove={handleRemoveTag} />
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Attachments */}
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold">Attachments</h3>
          </div>

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

          {journalLog.attachments.length === 0 ? (
            <p className="text-sm text-gray-500">No attachments</p>
          ) : (
            <div className="space-y-2">
              {journalLog.attachments.map((attachment) => (
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
                  <div className="flex space-x-2">
                    <button
                      onClick={() => {
                        // Placeholder for download - would need a download endpoint
                        console.log('Download attachment:', attachment.attachmentId);
                      }}
                      className="text-sm text-blue-600 hover:text-blue-800"
                    >
                      Download
                    </button>
                    <button
                      onClick={() => handleDeleteAttachment(attachment.attachmentId)}
                      className="text-sm text-red-600 hover:text-red-800"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Tag Assignment Modal */}
      {id && (
        <TagAssignmentModal
          isOpen={showTagModal}
          onClose={() => setShowTagModal(false)}
          entityType="JournalLog"
          entityId={parseInt(id)}
          existingTagTypeIds={tags.map(t => t.tagTypeId)}
          onTagAdded={handleTagAdded}
        />
      )}
    </div>
  );
};

export default JournalLogDetail;
