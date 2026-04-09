import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { propertyService, PropertyGroupResponseDto, PropertyResponseDto } from '../../services/propertyService';

const PropertyHubHome: React.FC = () => {
  const navigate = useNavigate();
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [groupsData, propertiesData] = await Promise.all([
        propertyService.getPropertyGroups(),
        propertyService.getProperties(),
      ]);
      setPropertyGroups(groupsData);
      setProperties(propertiesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load property data');
    } finally {
      setLoading(false);
    }
  };

  // Group properties by property group
  const propertiesByGroup = propertyGroups.map(group => ({
    group,
    properties: properties.filter(p => p.propertyGroupId === group.propertyGroupId),
  }));
  const unassignedProperties = properties.filter(
    (p) => !propertyGroups.some((g) => g.propertyGroupId === p.propertyGroupId)
  );

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-10 text-center">
        <div className="text-base font-medium text-slate-600">Loading property data...</div>
      </div>
    );
  }

  const SummaryCard: React.FC<{
    label: string;
    value: number;
    accent: string;
  }> = ({ label, value, accent }) => (
    <div className={`rounded-lg border p-4 ${accent}`}>
      <p className="text-[11px] font-semibold uppercase tracking-wide">{label}</p>
      <p className="mt-2 text-3xl font-semibold">{value}</p>
    </div>
  );

  const PropertyCard: React.FC<{ property: PropertyResponseDto; muted?: boolean }> = ({
    property,
    muted,
  }) => (
    <div
      className={`group rounded-lg border p-4 transition ${
        muted
          ? 'border-slate-200 bg-slate-50'
          : 'border-slate-200 bg-white hover:-translate-y-0.5 hover:shadow-md'
      }`}
    >
      <h3 className="font-semibold text-slate-900">{property.propertyName}</h3>
      {property.address && <p className="mt-2 text-sm text-slate-600">{property.address}</p>}
      {property.postCode && <p className="text-sm text-slate-500">{property.postCode}</p>}
      {!muted && (
        <div className="mt-4 flex items-center gap-2 text-xs font-semibold">
          <button
            className="rounded-md border border-slate-200 bg-white px-2 py-1 text-slate-700 transition hover:border-slate-300"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/Property Hub/Journal Logs?propertyId=${property.propertyId}`);
            }}
          >
            Journal Logs
          </button>
          <button
            className="rounded-md border border-slate-200 bg-white px-2 py-1 text-slate-700 transition hover:border-slate-300"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/Property Hub/Contact Logs?propertyId=${property.propertyId}`);
            }}
          >
            Contact Logs
          </button>
        </div>
      )}
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-slate-200 bg-white p-6">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Property Overview</h1>
        <p className="mt-1 text-sm text-slate-500">
          View property groups and quickly access journal and contact activity.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <SummaryCard
          label="Property Groups"
          value={propertyGroups.length}
          accent="border-slate-200 bg-white text-slate-900"
        />
        <SummaryCard
          label="Total Properties"
          value={properties.length}
          accent="border-indigo-200 bg-indigo-50 text-indigo-900"
        />
        <SummaryCard
          label="Unassigned"
          value={unassignedProperties.length}
          accent="border-amber-200 bg-amber-50 text-amber-900"
        />
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
          {error}
        </div>
      )}

      {propertyGroups.length === 0 && properties.length === 0 && !error && (
        <div className="rounded-lg border border-slate-200 bg-white p-10 text-center">
          <p className="text-slate-500">No property groups or properties found.</p>
        </div>
      )}

      {propertiesByGroup.length > 0 && (
        <div className="space-y-5">
          {propertiesByGroup.map(({ group, properties: groupProperties }) => (
            <section
              key={group.propertyGroupId}
              className="overflow-hidden rounded-lg border border-slate-200 bg-white"
            >
              <div className="flex flex-col gap-2 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-slate-900">{group.propertyGroupName}</h2>
                  {group.description && (
                    <p className="mt-1 text-sm text-slate-500">{group.description}</p>
                  )}
                </div>
                <div className="rounded-md bg-slate-100 px-3 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-600">
                  {groupProperties.length} {groupProperties.length === 1 ? 'Property' : 'Properties'}
                </div>
              </div>

              {groupProperties.length > 0 ? (
                <div className="grid grid-cols-1 gap-4 p-5 md:grid-cols-2 xl:grid-cols-3">
                  {groupProperties.map((property) => (
                    <PropertyCard key={property.propertyId} property={property} />
                  ))}
                </div>
              ) : (
                <div className="px-6 py-8 text-center text-sm text-slate-500">
                  No properties in this group
                </div>
              )}
            </section>
          ))}
        </div>
      )}

      {unassignedProperties.length > 0 && (
        <section className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <div className="border-b border-slate-100 px-5 py-4">
            <h2 className="text-lg font-semibold text-slate-900">Unassigned Properties</h2>
            <p className="mt-1 text-sm text-slate-500">
              These properties do not belong to a property group.
            </p>
          </div>
          <div className="grid grid-cols-1 gap-4 p-5 md:grid-cols-2 xl:grid-cols-3">
            {unassignedProperties.map((property) => (
              <PropertyCard key={property.propertyId} property={property} muted />
            ))}
          </div>
        </section>
      )}
    </div>
  );
};

export default PropertyHubHome;
