import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { propertyService, PropertyGroupResponseDto, PropertyResponseDto } from '../../services/propertyService';
import { propertyAdminService } from '../../services/propertyAdminService';
import { reminderService } from '../../services/reminderService';
import { maintenanceService } from '../../services/maintenanceService';
import {
  countActiveTenantsForProperty,
  countMaintenanceForProperty,
  countOpenRemindersForProperty,
} from './propertyHubMetrics';

function IconUsers({ className }: { className?: string }) {
  return (
    <svg className={className} width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path
        d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"
        fill="currentColor"
      />
    </svg>
  );
}

function IconReminder({ className }: { className?: string }) {
  return (
    <svg className={className} width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path
        d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"
        fill="currentColor"
      />
    </svg>
  );
}

function IconWrench({ className }: { className?: string }) {
  return (
    <svg className={className} width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path
        d="M22.7 19l-9.1-9.1c.9-2.3.4-5-1.5-6.9-2-2-5-2.4-7.4-1.3L9 6 6 9 1.6 4.7C.4 7.1.9 10.1 2.9 12.1c1.9 1.9 4.6 2.4 6.9 1.5l9.1 9.1c.4.4 1 .4 1.4 0l2.3-2.3c.5-.4.5-1.1.1-1.4z"
        fill="currentColor"
      />
    </svg>
  );
}

const PropertyHubHome: React.FC = () => {
  const navigate = useNavigate();
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<Awaited<ReturnType<typeof propertyAdminService.getTenancies>>>([]);
  const [reminders, setReminders] = useState<Awaited<ReturnType<typeof reminderService.getReminders>>>([]);
  const [maintenanceRows, setMaintenanceRows] = useState<
    Awaited<ReturnType<typeof maintenanceService.getMaintenanceRecords>>
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        const [groupsData, propertiesData, tenanciesData, remindersData, maintData] = await Promise.all([
          propertyService.getPropertyGroups(),
          propertyService.getProperties(),
          propertyAdminService.getTenancies(),
          reminderService.getReminders(),
          maintenanceService.getMaintenanceRecords(),
        ]);
        setPropertyGroups(groupsData);
        setProperties(propertiesData);
        setTenancies(tenanciesData);
        setReminders(remindersData);
        setMaintenanceRows(maintData);
      } catch (err: any) {
        setError(err.message || 'Failed to load property data');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const propertiesByGroup = propertyGroups.map((group) => ({
    group,
    properties: properties.filter((p) => p.propertyGroupId === group.propertyGroupId),
  }));
  const unassignedProperties = properties.filter(
    (p) => !propertyGroups.some((g) => g.propertyGroupId === p.propertyGroupId)
  );

  const PropertyCard: React.FC<{ property: PropertyResponseDto; muted?: boolean }> = ({ property, muted }) => {
    const tenants = countActiveTenantsForProperty(property.propertyId, tenancies);
    const openRem = countOpenRemindersForProperty(property.propertyId, reminders);
    const maint = countMaintenanceForProperty(property.propertyId, maintenanceRows);

    return (
      <button
        type="button"
        onClick={() => navigate(`/Property Hub/Property/${property.propertyId}`)}
        className={`w-full rounded-xl border p-3 sm:p-5 text-left transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 ${
          muted
            ? 'border-slate-200 bg-slate-50 hover:border-slate-300'
            : 'border-slate-200 bg-white hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md'
        }`}
      >
        <h3 className="text-base font-semibold text-slate-900 sm:text-lg">{property.propertyName}</h3>
        <div className="mt-3 grid grid-cols-3 gap-2 sm:mt-4 sm:gap-3">
          <div className="flex items-center gap-1.5 rounded-lg bg-blue-50/80 px-2 py-1.5 sm:gap-2 sm:px-3 sm:py-2">
            <IconUsers className="hidden shrink-0 text-blue-600 sm:block" />
            <div>
              <p className="text-base font-semibold leading-tight text-blue-900 sm:text-lg">{tenants}</p>
              <p className="text-[10px] font-medium leading-tight text-blue-800/80 sm:text-xs">Tenants</p>
            </div>
          </div>
          <div className="flex items-center gap-1.5 rounded-lg bg-blue-50/80 px-2 py-1.5 sm:gap-2 sm:px-3 sm:py-2">
            <IconReminder className="hidden shrink-0 text-blue-600 sm:block" />
            <div>
              <p className="text-base font-semibold leading-tight text-blue-900 sm:text-lg">{openRem}</p>
              <p className="text-[10px] font-medium leading-tight text-blue-800/80 sm:text-xs">Reminders</p>
            </div>
          </div>
          <div className="flex items-center gap-1.5 rounded-lg bg-blue-50/80 px-2 py-1.5 sm:gap-2 sm:px-3 sm:py-2">
            <IconWrench className="hidden shrink-0 text-blue-600 sm:block" />
            <div>
              <p className="text-base font-semibold leading-tight text-blue-900 sm:text-lg">{maint}</p>
              <p className="text-[10px] font-medium leading-tight text-blue-800/80 sm:text-xs">Maintenance</p>
            </div>
          </div>
        </div>
      </button>
    );
  };

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-10 text-center">
        <div className="text-base font-medium text-slate-600">Loading property data...</div>
      </div>
    );
  }

  const SummaryCard: React.FC<{ label: string; value: number; accent: string }> = ({ label, value, accent }) => (
    <div className={`rounded-lg border p-3 sm:p-4 ${accent}`}>
      <p className="text-[11px] font-semibold uppercase tracking-wide">{label}</p>
      <p className="mt-1 text-2xl font-semibold sm:mt-2 sm:text-3xl">{value}</p>
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-slate-200 bg-white p-4 sm:p-6">
        <h1 className="text-xl font-semibold tracking-tight text-slate-900 sm:text-2xl">Property overview</h1>
        <p className="mt-1 text-xs text-slate-500 sm:text-sm">
          Select a property to open its dashboard (maintenance, reminders, tenants, contact logs).
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <SummaryCard
          label="Property groups"
          value={propertyGroups.length}
          accent="border-slate-200 bg-white text-slate-900"
        />
        <SummaryCard
          label="Total properties"
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
              <div className="flex flex-col gap-2 border-b border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5 sm:py-4">
                <div>
                  <h2 className="text-base font-semibold text-slate-900 sm:text-lg">{group.propertyGroupName}</h2>
                  {group.description && <p className="mt-1 text-xs text-slate-500 sm:text-sm">{group.description}</p>}
                </div>
                <div className="rounded-md bg-slate-100 px-3 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-600">
                  {groupProperties.length} {groupProperties.length === 1 ? 'property' : 'properties'}
                </div>
              </div>

              {groupProperties.length > 0 ? (
                <div className="grid grid-cols-1 gap-3 p-3 sm:gap-4 sm:p-5 md:grid-cols-2 xl:grid-cols-3">
                  {groupProperties.map((property) => (
                    <PropertyCard key={property.propertyId} property={property} />
                  ))}
                </div>
              ) : (
                <div className="px-6 py-8 text-center text-sm text-slate-500">No properties in this group</div>
              )}
            </section>
          ))}
        </div>
      )}

      {unassignedProperties.length > 0 && (
        <section className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <div className="border-b border-slate-100 px-4 py-3 sm:px-5 sm:py-4">
            <h2 className="text-base font-semibold text-slate-900 sm:text-lg">Unassigned properties</h2>
            <p className="mt-1 text-xs text-slate-500 sm:text-sm">These properties are not in a property group.</p>
          </div>
          <div className="grid grid-cols-1 gap-3 p-3 sm:gap-4 sm:p-5 md:grid-cols-2 xl:grid-cols-3">
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
