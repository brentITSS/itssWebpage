import React from 'react';
import { useNavigate } from 'react-router-dom';

type HubScopedHeaderProps = {
  propertyId: number;
  propertyName: string;
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
};

const HubScopedHeader: React.FC<HubScopedHeaderProps> = ({
  propertyId,
  propertyName,
  title,
  subtitle,
  actions,
}) => {
  const navigate = useNavigate();
  return (
    <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <button
          type="button"
          onClick={() => navigate(`/Property Hub/Property/${propertyId}`)}
          className="mb-1 text-sm font-medium text-slate-600 hover:text-slate-900"
        >
          ← Back to {propertyName}
        </button>
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">{title}</h2>
        {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </div>
  );
};

export default HubScopedHeader;
