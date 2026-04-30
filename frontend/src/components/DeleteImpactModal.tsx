import React from 'react';

type DeleteImpactItem = {
  label: string;
  count?: number | null;
};

interface DeleteImpactModalProps {
  isOpen: boolean;
  title: string;
  subjectLabel: string;
  impacts: DeleteImpactItem[];
  confirmText?: string;
  isProcessing?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

const formatCount = (count?: number | null): string => {
  if (count === null || count === undefined) return 'Unknown';
  return String(count);
};

const DeleteImpactModal: React.FC<DeleteImpactModalProps> = ({
  isOpen,
  title,
  subjectLabel,
  impacts,
  confirmText = 'Delete',
  isProcessing = false,
  onCancel,
  onConfirm,
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-lg bg-white shadow-xl">
        <div className="border-b border-slate-200 px-5 py-4">
          <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
        </div>
        <div className="space-y-3 px-5 py-4 text-sm text-slate-700">
          <p>
            Deleting <span className="font-semibold">{subjectLabel}</span> will also remove linked records:
          </p>
          <ul className="space-y-1 rounded border border-slate-200 bg-slate-50 p-3">
            {impacts.map((impact) => (
              <li key={impact.label} className="flex items-center justify-between">
                <span>{impact.label}</span>
                <span className="font-semibold">{formatCount(impact.count)}</span>
              </li>
            ))}
          </ul>
          <p className="text-xs text-slate-500">This action cannot be undone.</p>
        </div>
        <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-4">
          <button
            type="button"
            className="rounded border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
            onClick={onCancel}
            disabled={isProcessing}
          >
            Cancel
          </button>
          <button
            type="button"
            className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-70"
            onClick={onConfirm}
            disabled={isProcessing}
          >
            {isProcessing ? 'Deleting...' : confirmText}
          </button>
        </div>
      </div>
    </div>
  );
};

export default DeleteImpactModal;
