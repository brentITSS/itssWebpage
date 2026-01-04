import React, { useState, useEffect } from 'react';
import { tagService, TagTypeResponseDto, CreateTagLogRequest } from '../services/tagService';

interface TagAssignmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  entityType: string;
  entityId: number;
  existingTagTypeIds: number[];
  onTagAdded: () => void;
}

const TagAssignmentModal: React.FC<TagAssignmentModalProps> = ({
  isOpen,
  onClose,
  entityType,
  entityId,
  existingTagTypeIds,
  onTagAdded,
}) => {
  const [tagTypes, setTagTypes] = useState<TagTypeResponseDto[]>([]);
  const [selectedTagTypeId, setSelectedTagTypeId] = useState<number>(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      loadTagTypes();
    }
  }, [isOpen]);

  const loadTagTypes = async () => {
    try {
      setLoading(true);
      setError(null);
      const types = await tagService.getTagTypes();
      setTagTypes(types);
      
      // Filter out already assigned tags
      const availableTypes = types.filter(t => !existingTagTypeIds.includes(t.tagTypeId));
      if (availableTypes.length > 0) {
        setSelectedTagTypeId(availableTypes[0].tagTypeId);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load tag types');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedTagTypeId === 0) return;

    try {
      setLoading(true);
      setError(null);
      const request: CreateTagLogRequest = {
        tagTypeId: selectedTagTypeId,
        entityType,
        entityId,
      };
      await tagService.createTagLog(request);
      onTagAdded();
      onClose();
      setSelectedTagTypeId(0);
    } catch (err: any) {
      setError(err.message || 'Failed to add tag');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  // Filter out already assigned tags
  const availableTagTypes = tagTypes.filter(t => !existingTagTypeIds.includes(t.tagTypeId));

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-bold">Add Tag</h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {availableTagTypes.length === 0 ? (
          <div className="mb-4">
            <p className="text-gray-500">All available tags have been assigned to this entity.</p>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Select Tag Type *
              </label>
              <select
                value={selectedTagTypeId}
                onChange={(e) => setSelectedTagTypeId(parseInt(e.target.value))}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
              >
                <option value="0">Select a tag type</option>
                {availableTagTypes.map(tagType => (
                  <option key={tagType.tagTypeId} value={tagType.tagTypeId}>
                    {tagType.tagTypeName}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex justify-end space-x-2">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={loading || selectedTagTypeId === 0}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
              >
                {loading ? 'Adding...' : 'Add Tag'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default TagAssignmentModal;
