import React from 'react';
import { TagDto } from '../services/tagService';

interface TagProps {
  tag: TagDto;
  onRemove?: (tagLogId: number) => void;
  showRemove?: boolean;
}

const Tag: React.FC<TagProps> = ({ tag, onRemove, showRemove = true }) => {
  const backgroundColor = tag.color ? `${tag.color}20` : '#e5e7eb';
  const textColor = tag.color || '#374151';
  const borderColor = tag.color || '#d1d5db';

  return (
    <span
      className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium border"
      style={{
        backgroundColor,
        color: textColor,
        borderColor,
      }}
    >
      {tag.tagTypeName}
      {showRemove && onRemove && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            onRemove(tag.tagLogId);
          }}
          className="ml-2 text-gray-500 hover:text-gray-700 focus:outline-none"
          aria-label={`Remove ${tag.tagTypeName} tag`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      )}
    </span>
  );
};

export default Tag;
