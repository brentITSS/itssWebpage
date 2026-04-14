import React from 'react';

const cardClass = 'rounded-xl border border-slate-200 bg-white p-5';

const PermissionGuide: React.FC = () => {
  return (
    <div className="space-y-6">
      <section className={cardClass}>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Permission Guide</h2>
        <p className="mt-2 text-sm text-slate-600">
          How access is currently enforced across Global Admin and Property Hub.
        </p>
      </section>

      <section className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-900">Roles and access levels</h3>
        <div className="mt-3 space-y-3 text-sm text-slate-700">
          <p>
            <span className="font-semibold">Global Admin:</span> full access across all workstreams and all Global
            Admin screens (`Users`, `Roles`, `Workstreams`, `Permissions`).
          </p>
          <p>
            <span className="font-semibold">Property Hub Admin:</span> not a role; this is a workstream assignment
            where permission type is <span className="font-semibold">Admin</span> on the Property Hub workstream.
            These users can manage Property Hub admin entities such as groups, properties, tenancies, lookups, and
            property group user assignments.
          </p>
          <p>
            <span className="font-semibold">Property Hub User:</span> users assigned to the Property Hub workstream
            with non-admin permission (for example, Edit/View). They can use operational areas such as Journal Logs,
            Contact Logs, Reminders, and Maintenance, but are blocked from Property Hub admin-only actions.
          </p>
        </div>
      </section>

      <section className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-900">What is hidden or restricted</h3>
        <ul className="mt-3 list-disc space-y-2 pl-5 text-sm text-slate-700">
          <li>
            Global Admin pages are intended for Global Admins only. API enforces this on user/role/workstream
            management endpoints.
          </li>
          <li>
            Property Hub Admin actions (create/update/delete for groups/properties/tenancies/lookups, and user
            assignment to property groups) require Property Hub Admin or Global Admin.
          </li>
          <li>
            Property Hub operational records (journals/contact logs/reminders/maintenance) require Property Hub
            workstream access; they are not limited to Property Hub Admin.
          </li>
          <li>
            Property Hub Admin UI currently shows a locked state for User Management when the signed-in user is not a
            Global Admin.
          </li>
        </ul>
      </section>

      <section className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-900">Important implementation notes</h3>
        <ul className="mt-3 list-disc space-y-2 pl-5 text-sm text-slate-700">
          <li>
            Workstream access is resolved from `tblWorkstreamUsers` + `tblPermissionType`; Global Admin always
            overrides access checks.
          </li>
          <li>
            Property Hub access checks are name-based (`Property Hub` / contains `Property`), so keep workstream naming
            consistent.
          </li>
          <li>
            Frontend route guards currently check for token presence; API remains the final authority and returns 403 if
            access is insufficient.
          </li>
        </ul>
      </section>
    </div>
  );
};

export default PermissionGuide;
