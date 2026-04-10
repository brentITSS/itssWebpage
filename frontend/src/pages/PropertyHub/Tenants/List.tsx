import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import { propertyAdminService } from '../../../services/propertyAdminService';
import type { TenancyResponseDto, TenantResponseDto } from '../../../services/adminService';
import HubScopedHeader from '../HubScopedHeader';
import { filterTenanciesForProperty } from '../propertyHubMetrics';

const TenantsList: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const propertyIdParam = searchParams.get('propertyId');
  const propertyId = propertyIdParam ? parseInt(propertyIdParam, 10) : NaN;
  const scoped = Number.isFinite(propertyId);

  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        const [p, t] = await Promise.all([
          propertyService.getProperties(),
          propertyAdminService.getTenancies(),
        ]);
        setProperties(p);
        setTenancies(t);
      } catch (e: any) {
        setError(e.message || 'Failed to load');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const property = properties.find((x) => x.propertyId === propertyId);

  const rows = useMemo(() => {
    if (!scoped) return tenancies;
    return filterTenanciesForProperty(propertyId, tenancies);
  }, [scoped, propertyId, tenancies]);

  const flatTenants = useMemo(() => {
    const list: { tenant: TenantResponseDto; tenancy: TenancyResponseDto }[] = [];
    for (const tenancy of rows) {
      for (const tenant of tenancy.tenants || []) {
        list.push({ tenant, tenancy });
      }
    }
    return list;
  }, [rows]);

  if (loading) {
    return <div className="py-8 text-center text-slate-600">Loading…</div>;
  }

  if (scoped && !property) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
        Unknown property.
        <button type="button" className="ml-2 underline" onClick={() => navigate('/Property Hub/Home')}>
          Home
        </button>
      </div>
    );
  }

  return (
    <div>
      {scoped && property ? (
        <HubScopedHeader
          propertyId={property.propertyId}
          propertyName={property.propertyName}
          title="Tenants"
          subtitle={`${flatTenants.length} tenant${flatTenants.length === 1 ? '' : 's'} on active tenancies (view only)`}
        />
      ) : (
        <div className="mb-6">
          <h2 className="text-2xl font-bold text-slate-900">Tenants</h2>
          <p className="mt-1 text-sm text-slate-500">
            Open this page from a property dashboard to see tenants for that property.
          </p>
        </div>
      )}

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {!scoped && (
        <p className="mb-6 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Use <strong>View all</strong> from a property’s dashboard, or add <code className="rounded bg-white px-1">?propertyId=</code> to the URL.
        </p>
      )}

      {scoped && flatTenants.length === 0 && (
        <p className="text-sm text-slate-500">No tenants on active tenancies for this property.</p>
      )}

      <div className="space-y-3">
        {flatTenants.map(({ tenant, tenancy }) => (
          <div
            key={`${tenancy.tenancyId}-${tenant.tenantId}`}
            className="flex flex-col gap-2 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between"
          >
            <div>
              <p className="font-semibold text-slate-900">
                {tenant.firstName} {tenant.lastName}
              </p>
              <p className="text-sm text-slate-500">
                {tenant.email || 'No email'}
                {tenant.phone ? ` · ${tenant.phone}` : ''}
              </p>
              <p className="mt-1 text-xs text-slate-400">
                Tenancy: {tenancy.description?.slice(0, 80) || `#${tenancy.tenancyId}`}
              </p>
            </div>
            <div className="text-xs text-slate-500">
              {tenant.tenantActive === false ? (
                <span className="rounded-full bg-slate-200 px-2 py-1">Inactive</span>
              ) : (
                <span className="rounded-full bg-emerald-100 px-2 py-1 text-emerald-800">Active</span>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default TenantsList;
