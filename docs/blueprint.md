# Requirements Document (v1)

## 1. Global Requirements

- System must authenticate users via `/Login` using `tblUser`.
- System must determine workstream/page access using:
  - `tblRole`, `tblRoleType`
  - `tblWorkstreamUsers`, `tblPermissionType`
- Global Admins have unrestricted access.
- All pages must be responsive across desktop, tablet, and mobile.
- All create/update/delete actions must be auditable.

## 2. Workstream 1.0 – Login

- Route: `/Login`
- User enters email + password.
- On success → redirect to default workstream landing page.
- On failure → generic error message.

## 3. Workstream 2.0 – Global Admin

- Route: `/Admin`
- Access restricted to `RoleType = Global Admin`.
- Features:
  - Manage users (create, deactivate, reset password)
  - Manage roles and role types
  - Assign workstreams and permissions
  - View audit logs

## 4. Workstream 3.0 – Property Hub

### 4.1 – Home
- Route: `/Property Hub/Home`
- Show property groups and properties user can access.

### 4.2 – Admin
- Route: `/Property Hub/Admin`
- Access: Property Hub Admin permission
- Features:
  - Manage property groups
  - Manage properties
  - Manage tenants and tenancies
  - Manage lookup data (journal types, contact types, tag types)

### 4.3 – User Management
- Route: `/Property Hub/Admin/User Management`
- Assign users to Property Hub workstream
- Set permission type
- View access matrix

### 4.4 – Journal Logs
- Create journal entries in `tblJournalLog`
- Upload attachments to `tblJournalLogAttachment`
- Phase 2: Email ingestion + auto-interpretation

### 4.5 – Contact Logs
- Create contact logs in `tblContactLog`
- Upload attachments to `tblContactLogAttachment`
- Apply tags via `tblTagLog`

## 5. Security Requirements

- Passwords must be hashed + salted.
- HTTPS enforced.
- Secure session handling (JWT or cookies).

## 6. Non-functional Requirements

- Page load < 2 seconds.
- 99.5% uptime target.
- Modular, maintainable codebase.

# Design Document (v1)

## 1. Overview

A multi-workstream web application with fine-grained access control and property management features. Phase 1 includes Login, Global Admin, Property Hub (Home, Admin, User Management), journal logs, and contact logs.

## 2. Architecture

### Frontend
- React SPA hosted on Hostinger
- Tailwind CSS for styling
- React Router for navigation
- Auth state stored in secure cookies or memory

### Backend
- ASP.NET Core 8 Web API
- Deployed to Azure App Service
- REST endpoints for:
  - Authentication
  - Users, roles, workstreams, permissions
  - Properties, tenants, tenancies
  - Journal logs, contact logs, tags, attachments

### Database
- Azure SQL
- Schema based on ERD:
  - Users, roles, workstreams
  - Properties, property groups
  - Journal logs, contact logs
  - Tags and attachments

## 3. Access Control Model

### Authentication
- Username/password validated against `tblUser`
- Backend issues JWT or secure cookie

### Authorisation
- Global Admin:
  - `tblRoleType.roleType = 'Global Admin'`
  - Full access to all workstreams/pages

- Workstream Access:
  - `tblWorkstreamUsers` links user → workstream → permission type

- Page-level Access:
  - Frontend route guards
  - Backend endpoint guards

## 4. Data Model Notes

- Property groups → properties (1-to-many)
- Journal logs link to property, tenancy, tenant
- Contact logs link to property, tenant
- Tags apply to logs, tenants, properties, property groups

## 5. Frontend Design

- Global header with user info + logout
- Left-hand navigation that changes per workstream
- Breadcrumbs for deep pages
- Reusable components:
  - Buttons, inputs, tables, modals, tags, toasts

## 6. CSS / Theming Strategy

- Use **one global design system** (Tailwind)
- Use **CSS variables** or Tailwind theme extension for:
  - Workstream-specific colours
  - Icons
  - Accent styles

Rationale:
- Consistency across the site
- Easier maintenance
- Workstreams still feel distinct

## 7. Deployment

- Frontend → Hostinger (static files)
- Backend → Azure App Service
- Database → Azure SQL
- CI/CD → GitHub Actions

## 8. Risks & Assumptions

- Email ingestion is phase 2
- Initial version focuses on manual entry + audit trails

# 3. Implementation Plan (Phase 1)

This plan defines the exact sequence of development tasks for building the system. Cursor should follow these steps one at a time.

## Step 1 — Create Project Structure
- Create `/frontend` React + TypeScript project.
- Create `/backend` ASP.NET Core Web API project.
- Create `/db` folder for schema + migrations.
- Create `/docs` folder for blueprint, requirements, design, logs.

## Step 2 — Implement Authentication
- Create `POST /api/auth/login` endpoint.
- Validate user against `tblUser`.
- Issue JWT or secure cookie.
- Create `/Login` page in frontend.
- Implement route guards.

## Step 3 — Implement Global Admin Workstream
Backend:
- Users CRUD
- Roles CRUD
- Workstreams CRUD
- Workstream user assignment
- Permission type assignment

Frontend:
- `/Admin` dashboard
- User list + create/edit
- Role assignment UI
- Workstream assignment UI

## Step 4 — Implement Property Hub Home
Backend:
- Property groups list
- Properties list

Frontend:
- `/Property Hub/Home` page
- Display property groups and properties user can access

## Step 5 — Implement Property Hub Admin
Backend:
- CRUD for property groups
- CRUD for properties
- CRUD for tenants and tenancies
- CRUD for lookup tables (journal types, contact types, tag types)

Frontend:
- `/Property Hub/Admin`
- `/Property Hub/Admin/User Management`
- CRUD pages for groups, properties, tenants, lookups

## Step 6 — Implement Journal Logs
Backend:
- CRUD for `tblJournalLog`
- File upload for `tblJournalLogAttachment`

Frontend:
- Journal list page
- Journal create/edit page
- Attachment upload UI

## Step 7 — Implement Contact Logs
Backend:
- CRUD for `tblContactLog`
- File upload for `tblContactLogAttachment`

Frontend:
- Contact log list page
- Contact log create/edit page
- Attachment upload UI

## Step 8 — Implement Tagging
Backend:
- CRUD for `tblTagType`
- CRUD for `tblTagLog`

Frontend:
- Tag UI component
- Tag assignment modal

## Step 9 — CI/CD Setup
- GitHub Actions workflow for frontend → Hostinger
- GitHub Actions workflow for backend → Azure App Service
- Database migration pipeline

# 4. Recommended Folder Structure

/itssWebpage
  /frontend
    /src
      /components
      /pages
      /routes
      /hooks
      /services
      /styles
    package.json
    tsconfig.json

  /backend
    /Controllers
    /Models
    /Services
    /Repositories
    /DTOs
    /Middleware
    appsettings.json
    Program.cs
    Startup.cs

  /db
    schema.sql
    /migrations

  /docs
    blueprint.md
    requirements.md
    design.md
    implementation-plan.md
    change-log.md
    issues-log.md

# 5. API Endpoint Specification (v1)

## Auth
POST /api/auth/login
GET /api/auth/me

## Users
GET /api/users
POST /api/users
PUT /api/users/{id}
DELETE /api/users/{id}

## Roles
GET /api/roles
POST /api/roles
PUT /api/roles/{id}

## Workstreams
GET /api/workstreams
POST /api/workstreams
PUT /api/workstreams/{id}

## Workstream Users
GET /api/workstreams/{id}/users
POST /api/workstreams/{id}/users
DELETE /api/workstreams/{id}/users/{userId}

## Properties
GET /api/properties
POST /api/properties
PUT /api/properties/{id}
DELETE /api/properties/{id}

## Property Groups
GET /api/property-groups
POST /api/property-groups
PUT /api/property-groups/{id}
DELETE /api/property-groups/{id}

## Tenants
GET /api/tenants
POST /api/tenants
PUT /api/tenants/{id}
DELETE /api/tenants/{id}

## Journal Logs
GET /api/journals
POST /api/journals
PUT /api/journals/{id}
DELETE /api/journals/{id}
POST /api/journals/{id}/attachments

## Contact Logs
GET /api/contact-logs
POST /api/contact-logs
PUT /api/contact-logs/{id}
DELETE /api/contact-logs/{id}
POST /api/contact-logs/{id}/attachments

## Tags
GET /api/tags
POST /api/tags
PUT /api/tags/{id}
DELETE /api/tags/{id}

## Tag Log
POST /api/tag-log
DELETE /api/tag-log/{id}

# 6. React Route Map

/Login

/Admin
  /Admin/Users
  /Admin/Roles
  /Admin/Workstreams
  /Admin/Permissions

/Property Hub/Home

/Property Hub/Admin
  /Property Hub/Admin/User Management
  /Property Hub/Admin/Property Groups
  /Property Hub/Admin/Properties
  /Property Hub/Admin/Tenants
  /Property Hub/Admin/Lookups

/Property Hub/Journal Logs
  /Property Hub/Journal Logs/New
  /Property Hub/Journal Logs/{id}

/Property Hub/Contact Logs
  /Property Hub/Contact Logs/New
  /Property Hub/Contact Logs/{id}

  # 7. Database Schema Outline
    -also review .\initial scope\EntityRelationship.png together with the below

## Users & Roles
tblUser
tblRole
tblRoleType

## Workstreams & Permissions
tblWorkstream
tblPermissionType
tblWorkstreamUsers

## Properties
tblPropertyGroup
tblProperty

## Tenants & Tenancies
tblTenant
tblTenancy

## Journal Logs
tblJournalLog
tblJournalType
tblJournalSubType
tblJournalLogAttachment

## Contact Logs
tblContactLog
tblContactLogType
tblContactLogAttachment

## Tags
tblTagType
tblTagLog
