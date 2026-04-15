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
            <span className="font-semibold">Property Hub User (workstream user):</span> users assigned to the Property
            Hub workstream with non-admin permission (for example, Edit/View). In the Property Hub top navigation they
            only see <span className="font-semibold">Home</span> and <span className="font-semibold">Logout</span> (no
            Admin or operational tabs). They can still use operational CRUD when they open the corresponding URLs (for
            example from bookmarks or links from Home). Property Hub admin-only routes and API actions remain blocked.
          </p>
        </div>
      </section>

      <section className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-900">What is hidden or restricted</h3>
        <ul className="mt-3 list-disc space-y-2 pl-5 text-sm text-slate-700">
          <li>
            Global Admin pages (<code className="rounded bg-slate-100 px-1">/Admin/…</code>) are restricted to Global
            Admins in the UI. Property Hub workstream admins cannot open Global Admin. API also enforces this on
            user/role/workstream management endpoints.
          </li>
          <li>
            Property Hub Admin section (<code className="rounded bg-slate-100 px-1">/Property Hub/Admin/…</code>) is
            only for Global Admins or users with <span className="font-semibold">Admin</span> permission on a Property
            Hub workstream (workstream admin). Workstream users are redirected to Home if they try to open it.
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
            The SPA enforces route access from your profile (<code className="rounded bg-slate-100 px-1">/auth/me</code>
            ): Global Admin vs Property Hub workstream admin vs workstream user (top nav and admin routes). API remains
            the final authority and returns 403 if access is insufficient.
          </li>
        </ul>
      </section>

      <section className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-900">Permission matrix</h3>
        <p className="mt-2 text-sm text-slate-600">
          Quick reference for what each access level can do.
        </p>
        <div className="mt-4 overflow-x-auto">
          <table className="min-w-full border-collapse text-left text-sm text-slate-700">
            <thead>
              <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <th className="border border-slate-200 px-3 py-2">Capability</th>
                <th className="border border-slate-200 px-3 py-2">Global Admin</th>
                <th className="border border-slate-200 px-3 py-2">Workstream Admin</th>
                <th className="border border-slate-200 px-3 py-2">Workstream User (Edit)</th>
                <th className="border border-slate-200 px-3 py-2">Workstream User (View)</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">Open Global Admin (`/Admin/...`)</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
              </tr>
              <tr className="bg-slate-50/50">
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">Open Property Hub Home</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
              </tr>
              <tr>
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">See full Property Hub top nav</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">No (Home + Logout only)</td>
                <td className="border border-slate-200 px-3 py-2">No (Home + Logout only)</td>
              </tr>
              <tr className="bg-slate-50/50">
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">Open Property Hub Admin (`/Property Hub/Admin/...`)</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
              </tr>
              <tr>
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">Operational read (journals/contact/reminders/maintenance)</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
              </tr>
              <tr className="bg-slate-50/50">
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">Operational create/update/delete</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">No (API blocks)</td>
              </tr>
              <tr>
                <td className="border border-slate-200 px-3 py-2 font-medium text-slate-900">Manage Property Hub setup data (groups/properties/tenancies/lookups)</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">Yes</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
                <td className="border border-slate-200 px-3 py-2">No</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};

export default PermissionGuide;
