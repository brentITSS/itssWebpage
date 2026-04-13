import React from 'react';

type EntityActionButtonsProps = {
  onView: () => void;
  onEdit: () => void;
  onDelete: () => void;
  compact?: boolean;
};

const baseButtonClass =
  'inline-flex items-center justify-center rounded-md transition focus:outline-none focus:ring-2 focus:ring-offset-1';

const regularButtonClass = `${baseButtonClass} h-9 w-9 border`;
const compactButtonClass = `${baseButtonClass} h-8 w-8`;

const iconClass = 'h-4 w-4';

const EntityActionButtons: React.FC<EntityActionButtonsProps> = ({
  onView,
  onEdit,
  onDelete,
  compact = false,
}) => {
  const cls = compact ? compactButtonClass : regularButtonClass;

  return (
    <div className="flex items-center gap-1.5">
      <button
        type="button"
        onClick={onView}
        aria-label="View"
        title="View"
        className={`${cls} border-sky-200 text-sky-700 hover:bg-sky-50 focus:ring-sky-400`}
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
          <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6z" />
          <circle cx="12" cy="12" r="2.8" />
        </svg>
      </button>

      <button
        type="button"
        onClick={onEdit}
        aria-label="Edit"
        title="Edit"
        className={`${cls} border-emerald-200 text-emerald-700 hover:bg-emerald-50 focus:ring-emerald-400`}
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
          <path d="M4 20h4l10-10-4-4L4 16v4z" />
          <path d="M12 6l4 4" />
        </svg>
      </button>

      <button
        type="button"
        onClick={onDelete}
        aria-label="Delete"
        title="Delete"
        className={`${cls} border-rose-200 text-rose-700 hover:bg-rose-50 focus:ring-rose-400`}
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={iconClass}>
          <path d="M4 7h16" />
          <path d="M9 7V5h6v2" />
          <path d="M7 7l1 12h8l1-12" />
          <path d="M10 11v6M14 11v6" />
        </svg>
      </button>
    </div>
  );
};

export default EntityActionButtons;
