# Liberty X Nexus CRM

Nexus is a web-based client relationship and financial advisory management system built with ASP.NET Core (.NET 10) and deployed to Microsoft Azure. It was built by the Nexus Dev Team for **Ratul Singh**, a financial advisor at Liberty, as the INSY7315 project. It complements the advisor's existing CRM rather than replacing it, and gives the advisor and his clients separate, role-based experiences for managing policies, cases, meetings, documents and messages.

## Table of Contents

1. [Project Background](#project-background)
2. [Features](#features)
3. [Requirements Status](#requirements-status)
4. [Technology Stack](#technology-stack)
5. [Solution Structure](#solution-structure)
6. [Getting Started](#getting-started)
7. [Configuration](#configuration)
8. [Database](#database)
9. [Authentication and Authorization](#authentication-and-authorization)
10. [Security](#security)
11. [API Overview](#api-overview)
12. [Testing](#testing)
13. [CI/CD](#cicd)
14. [Deployment, Environments and Costs](#deployment-environments-and-costs)
15. [Known Limitations](#known-limitations)
16. [Rollout Plan](#rollout-plan)
17. [Documentation](#documentation)
18. [Authors](#authors)

## Project Background

The advisor identified fragmented client information, repetitive administrative tasks, time-consuming meeting preparation and difficulty keeping a consolidated view of client activity. Nexus addresses this with two interfaces:

- **Advisor Dashboard**: client profiles, policies, cases, meetings, documents, messages and an overview of outstanding work.
- **Client Portal**: clients securely see their own policies, shared documents and case progress, book meetings and message their advisor.

Objectives:

1. Centralise client information and reduce reliance on fragmented administrative processes
2. Help the advisor monitor policies, cases, meetings and outstanding client actions
3. Give clients secure, role-appropriate access to their own information
4. Improve communication through messages and notifications
5. Support controlled document upload, storage and retrieval
6. Reduce repetitive administrative work
7. Provide a maintainable, scalable and secure foundation for future features

## Features

### Advisor

- Secure login with role-based authorization
- Dashboard: active cases, awaiting documents, meetings, pipeline value, deadlines and institution statistics
- Client management: invite clients by email, create, search, filter by status, edit and delete (POPIA consent captured at creation)
- Policy management: shared policy catalogue and per-client policies with status tracking
- Case workflow: move a case through its steps (details submitted, adviser review, FICA verification, submitted to Liberty)
- Document management: upload, link to a client and policy, choose whether each document is visible to the client, download via short-lived links, delete
- Messaging with assigned clients
- Calendar: view, confirm, reschedule and cancel meetings, with clash detection
- Notifications when a client registers, books, reschedules or cancels a meeting, sends a message or uploads a document
- Nexus assistant: FAQs, market information (USD/ZAR rate and financial news) and a calculator for simple finance questions

### Client

- Registration through the advisor's invitation link (single use, valid for 14 days). There is no open sign-up.
- View policies and case progress
- View, download and upload documents (only those the advisor shared, plus their own uploads)
- Book, view, reschedule and cancel meetings
- Messaging with their advisor
- Notifications for meetings, messages, shared documents and application updates
- Nexus assistant: FAQs, market information and calculator
- Password reset by email
- "Keep me signed in" option (7-day session)

## Requirements Status

Status of each functional requirement from the project documentation:

| Requirement | Status | What was built |
| --- | --- | --- |
| User authentication | Met | Login and logout with ASP.NET Core Identity, JWT tokens, account lockout and password reset. Passwords need at least 12 characters. |
| Access control | Met | Server-side role checks. Advisors see only their own clients; clients see only their own records. |
| Client profile management | Met | The advisor creates, views and updates client profiles. |
| Document management | Met | Upload, link to a client and policy, view and download. The advisor chooses whether each document is visible to the client. |
| Client portal | Met | Clients see their profile, policies, visible documents, case status and messages. |
| Client to advisor communication | Met | Both sides can send messages and read the history. |
| Notifications | Met | The advisor is notified when a client registers, case status changes and messages arrive. |
| Case management | Met | The advisor moves a case through its steps and clients follow the progress. |
| View query history for advisor | Not built | Replaced by the Messages feature. The Queries feature was not used. |
| Generated service documents | Not built | Replaced by advisor-uploaded documents with visible/hidden settings. |

## Technology Stack

| Area | Technology |
| --- | --- |
| Backend | ASP.NET Core Web API, .NET 10, C# |
| Frontend | Blazor Web App (interactive server rendering) |
| Data | Entity Framework Core, Azure SQL Database |
| Identity | ASP.NET Core Identity, HS256-signed JWT bearer tokens |
| Files | Azure Blob Storage (managed identity, SAS download links) |
| Email | SMTP (invitations and password reset) |
| External data | Alpha Vantage (exchange rate), Google News RSS |
| Tests | xUnit, Moq, coverlet |
| Hosting and CI/CD | Azure App Service, GitHub, GitHub Actions |

Design patterns in use include the Repository pattern and a layered architecture: **Controllers → Services → Repositories → DbContext**.

## Solution Structure

```text
LibertyXNexusCRMWebApp/
├── API/          # ASP.NET Core Web API (controllers, services, repositories, EF Core, migrations)
├── API.Tests/    # xUnit tests
├── frontend/     # Blazor web frontend
├── Shared/       # Shared models, DTOs and enums
└── LibertyXNexusCRMWebApp.slnx
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022 or later (ASP.NET workload), or the `dotnet` CLI
- SQL Server (LocalDB or full) or access to the Azure SQL database
- Azure Storage access (or a storage connection string) if you need documents
- `dotnet-ef` tool for migrations: `dotnet tool install --global dotnet-ef`

### Clone, restore and build

```bash
git clone <repository-url>
cd Libery_X_Nexus_Dev_Team
dotnet restore LibertyXNexusCRMWebApp/LibertyXNexusCRMWebApp.slnx
dotnet build LibertyXNexusCRMWebApp/LibertyXNexusCRMWebApp.slnx
```

### Set local secrets

Never put secrets in `appsettings.json`. Use User Secrets for the API:

```bash
cd LibertyXNexusCRMWebApp/API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-sql-connection-string>"
dotnet user-secrets set "Jwt:Key" "<random-string-of-at-least-32-bytes>"
dotnet user-secrets set "ConnectionStrings:BlobStorage" "<storage-connection-string>"   # local only; or rely on az login
dotnet user-secrets set "Seed:AdvisorEmail" "advisor@example.com"
dotnet user-secrets set "Seed:AdvisorPassword" "<12+ chars, upper, lower, digit>"
dotnet user-secrets set "Seed:ClientEmail" "client@example.com"
dotnet user-secrets set "Seed:ClientPassword" "<12+ chars, upper, lower, digit>"
dotnet user-secrets set "Email:Smtp:Host" "<smtp-host>"
dotnet user-secrets set "Email:Smtp:Username" "<smtp-user>"
dotnet user-secrets set "Email:Smtp:Password" "<smtp-password>"
dotnet user-secrets set "Email:Smtp:FromEmail" "<from-address>"
dotnet user-secrets set "AlphaVantage:ApiKey" "<api-key>"
```

In Development the API applies migrations and seeds demo data on startup (`Database:MigrateOnStartup` and `Seed:DemoData` are on in `appsettings.Development.json`). The seeded advisor and client accounts use the emails and passwords above.

### Run

Start the **API** first and wait for it to finish starting, then start the **frontend**. Use Visual Studio multiple startup projects, or:

```bash
dotnet run --project LibertyXNexusCRMWebApp/API       # https://localhost:7143
dotnet run --project LibertyXNexusCRMWebApp/frontend
```

The frontend reads the API address from `ApiSettings:BaseUrl` (`https://localhost:7143/api/` in Development). The API only accepts browser calls from the origin set in `Frontend:BaseUrl`, so make sure it matches your frontend URL.

Swagger is available at `/swagger` on the API **in Development only**.

## Configuration

### API settings

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server / Azure SQL connection string (required) |
| `ConnectionStrings:BlobStorage` | Local-development storage connection string (optional) |
| `BlobStorage:AccountUrl` | Storage account URL (required in production, uses managed identity) |
| `BlobStorage:ContainerName` | Document container, default `client-documents` |
| `Jwt:Key` | Signing key, at least 32 bytes (required, validated at startup) |
| `Jwt:Issuer`, `Jwt:Audience` | Token issuer and audience |
| `Jwt:ExpiryMinutes` | Normal session length (default 60) |
| `Jwt:RememberMeExpiryDays` | "Keep me signed in" length (default 7) |
| `Frontend:BaseUrl` | Frontend URL, used for CORS and email links (required) |
| `Email:Smtp:*` | `Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName`, `EnableSsl` |
| `AlphaVantage:ApiKey`, `AlphaVantage:BaseUrl` | Market information |
| `Database:MigrateOnStartup` | Apply migrations when the API starts (on in Development, off in Production) |
| `Seed:DemoData` | Seed demo data and the seed accounts |
| `Seed:SampleDocuments` | Seed sample documents (Development only) |
| `Seed:AdvisorEmail/Password`, `Seed:ClientEmail/Password` | Seed account credentials |
| `RateLimiting:*` | `Enabled`, window sizes and request limits per user, anonymous, auth, password reset and upload |

### Frontend settings

| Setting | Purpose |
| --- | --- |
| `ApiSettings:BaseUrl` | API address ending in `/api/` (required) |

On Azure App Service, use environment variables with double underscores (for example `Jwt__Key`, `Email__Smtp__Host`, `ApiSettings__BaseUrl`). The database connection goes in the **Connection strings** section as `DefaultConnection`.

## Database

EF Core code-first migrations live in `API/Migrations`.

```bash
# Add a migration
dotnet ef migrations add <MigrationName> --project LibertyXNexusCRMWebApp/API

# Apply to the configured database
dotnet ef database update --project LibertyXNexusCRMWebApp/API
```

Production migrations are applied by the CI/CD pipeline using an EF migration bundle, not on API startup.

Main entities: Advisors, Clients, Policies, Cases, Meetings, Documents, Messages, Notifications, Invitations and Audit Logs, plus the ASP.NET Identity tables.

Data integrity: foreign keys stop orphaned rows, unique constraints prevent duplicate emails and tokens, check constraints limit columns to valid values, and indexes support common searches. Azure SQL Database was chosen for relational, structured data; Azure Blob Storage was chosen for client documents.

## Authentication and Authorization

- ASP.NET Core Identity manages accounts and passwords; the API issues signed JWTs on login.
- Roles: **Advisor** and **Client**. A prospective client (guest) is not signed in and can only open an invitation link.
- The token carries `advisorId` or `clientId`, and every endpoint checks that the caller owns the data it asks for.
- A fallback policy requires an authenticated user on every endpoint unless it is explicitly marked anonymous (login, register, password reset, invitation validation, health check).
- Clients register through an emailed invitation link (valid 14 days, single use). A client record the advisor already created is linked to the new account automatically.
- Passwords: minimum 12 characters with upper case, lower case and a digit. Accounts lock for 15 minutes after 5 failed attempts.
- Authorization is enforced by the API, not Azure Easy Auth.

## Security

### Controls in the deployed system

| Area | Control |
| --- | --- |
| Authentication | ASP.NET Core Identity with account lockout, passwords of at least 12 characters, and HS256-signed JWT bearer tokens that expire after 60 minutes (7 days with "Keep me signed in") |
| Authorization | Server-side role checks, deny-by-default endpoints, and per-user ownership checks |
| Abuse protection | Rate limiting on all requests, with stricter limits on login, password reset and document upload |
| Document security | Private Blob container, access through managed identity, read-only links that expire after 15 minutes, and limits on upload size (25 MB) and type (PDF, PNG, JPEG, DOC, DOCX, checked by extension, content type and file signature) |
| Secrets | Connection strings and keys held as protected application settings in Azure and as user secrets on developer machines, never in source code |
| Network | HTTPS between users and the deployed services; CORS restricted to the configured frontend origin |
| Data at rest | Encryption at rest provided by Azure SQL Database and Azure Blob Storage |
| Auditing | Audit log entries for logins and client create, update and delete |
| Pipeline | CI scans NuGet dependencies for known vulnerabilities, runs tests with code coverage, and fails the run if any step fails |

### Security test results

| # | Test | Expected | Actual | Result |
| --- | --- | --- | --- | --- |
| 1 | `GET /api/clients` with no token | 401 Unauthorized | 401 with a `WWW-Authenticate: Bearer` header and no data | Pass |
| 2 | `GET /api/clients` with a client's token | 403 Forbidden | 403 Forbidden | Pass |
| 3 | `GET /health` | 200, database and storage healthy | 200 OK; database and blob storage "Healthy" | Pass |
| 4 | Login with a wrong password | 401 with no hint of which field is wrong | 401 "Invalid email or password" | Pass |
| 5 | Repeated login requests | 429 Too Many Requests | 429 with "Too many requests" and `retryAfterSeconds: 60` | Pass |

Accessibility was checked with a Lighthouse audit of the deployed login page. See the project documentation for the evidence.

### Recommended next steps

- **Azure Key Vault**: move secrets out of app settings for audited, centrally managed secrets and automatic rotation.
- **Monitoring and alerting**: `/health` shows whether a release started correctly but does not alert anyone about production failures.

## API Overview

| Route | Purpose |
| --- | --- |
| `api/auth` | login, register, forgot-password, reset-password, me |
| `api/invitation` | create and validate invitations |
| `api/clients` | search, view, create, update, delete clients |
| `api/policies` | catalogue and client policies |
| `api/cases` | case status and step completion |
| `api/meetings` | book, reschedule, cancel, confirm, list |
| `api/documents` | upload, list, download link, visibility, delete |
| `api/message` | conversations and sending messages |
| `api/notifications` | feeds, unread counts, mark as read |
| `api/advisor/dashboard` | advisor dashboard figures |
| `api/faq`, `api/marketinformation` | assistant content |
| `/health` | database and blob storage health check |

## Testing

```bash
dotnet test LibertyXNexusCRMWebApp/API.Tests/API.Tests.csproj
```

Unit tests cover the auth, client, policy, meeting, document and JWT services, the main controllers, DTO validation and rate limiting. CI collects code coverage on every run.

## CI/CD

The GitHub Actions workflow (`.github/workflows`) runs on pushes and pull requests to `main` and `dev`. Changes reach `main` only through reviewed pull requests.

**Build and Test** (all runs; a failed step stops the run before anything is deployed):
1. Restore dependencies
2. Scan NuGet packages for vulnerabilities (fails the build if any are found)
3. Build in Release
4. Run tests with code coverage
5. Generate an idempotent migration script

**On push to `main` only:**
1. Apply migrations to Azure SQL using an EF migration bundle (a separate job, not at API startup)
2. Deploy the API to the `nexus-devteam` App Service
3. Deploy the frontend only after the API, so the interface never calls an older API
4. Call `/health` on the API, which checks the database and Blob Storage, and fail the run if either check fails (retries for about 2 minutes)

Required GitHub secrets and variables:

```text
AZURE_API_PUBLISH_PROFILE
AZURE_FRONTEND_PUBLISH_PROFILE
AZURE_SQL_CONNECTION_STRING
AZURE_FRONTEND_URL            (repository variable)
```

## Deployment, Environments and Costs

- **API:** https://nexus-devteam-g8ece0ftgwbwevdt.centralindia-01.azurewebsites.net/
- **Frontend:** https://libertyxnexus-web-thabo-djegfafwe4h3ayc6.centralindia-01.azurewebsites.net/

Infrastructure: Azure App Service (API and frontend), Azure SQL Database, Azure Blob Storage, GitHub and GitHub Actions. The API App Service uses a system-assigned managed identity with the `Storage Blob Data Contributor` role on the storage account, so no storage key is kept in configuration. Swagger is disabled in production.

| Environment | Status |
| --- | --- |
| Development | Local, using Visual Studio, the .NET SDK and the configured development database |
| Staging | Proposed, not yet created |
| Production | Azure App Service, with configuration and credentials managed outside source code |

### Running costs

The system currently runs on the Free (F1) App Service tier at R0/month (API on Windows, frontend on Linux). Estimated cost as usage grows:

| Scale | Clients | Tier | API | Frontend | Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Small | 100 | B1 (Basic) | R292 | R292 | R584 | R7,008 |
| Medium | 1,000 | S1 (Standard) | R2,086 | R2,086 | R4,172 | R50,070 |
| Large | 10,000 | P1V2 (Premium V2) | R2,781 | R1,669 | R4,450 | R53,408 |

Two-year projection on two B1 plans: R7,008 in year 1 and R7,008 in year 2, R14,016 in total.

## Known Limitations

Each of these is a deliberate trade-off for the pilot or a recommended next step.

| Area | Limitation | Planned improvement |
| --- | --- | --- |
| Features | Generated service documents were not built. The advisor uploads his own documents instead. | Add document generation if the advisor asks for it |
| Features | The Nexus assistant is rules-based, not AI, as it only answers the questions it was built for. | Extend its rules or add an approved AI service |
| Security | Invitation tokens are stored as plain text. They are single-use and expire after 14 days. | Store only a hash of each token |
| Security | A token cannot be cancelled before it expires (60 minutes, or 7 days with "Keep me signed in"). | Check the security stamp on each request |
| Security | Secrets are held as app settings, not in Azure Key Vault. | Move secrets into Key Vault |
| Security | No Web Application Firewall, and no private network access to the database. | Upgrade the hosting tier and add these controls |
| Security | Audit logging covers only important actions. | Log more actions and add a log viewer for the advisor |
| Data | Simultaneous edits to the same record are not detected. | Add concurrency tokens |
| Data | Deleting a client does not remove that client's stored files. | Delete files when the client is deleted |
| Operations | No staging environment, no deployment slots, and no automatic rollback. | Add staging and automatic rollback on a failed health check |
| Operations | Application Insights is not enabled, so there is no production alerting. | Enable it and add alerts |
| Testing | Performance targets were not tested under heavy load. | Run a load test |

## Rollout Plan

The advisor is the sole internal user and was involved in requirements gathering from the start. Adoption is supported by advisor training, user acceptance testing and a small pilot before full rollout:

- **Phased rollout**: launch the core features (registration and login, policy tracking, meeting bookings, documents) with a pilot group of 3 to 5 existing clients chosen by the advisor, then invite the wider client base.
- **Quick wins first**: prioritise features that cut the advisor's admin load, such as a single view of client meetings and notifications when a client registers or changes a meeting.
- **Feedback**: gather feedback from the advisor and pilot clients at the end of the pilot and adjust before full rollout.
- **Branding**: the interface uses colours associated with Standard Bank and Liberty to build client trust.

Clients are invited individually by a link from the advisor. The User Guide delivered with the final project is the main training material.

## Documentation

The full INSY7315 project documentation covers requirements, user stories, UX journey maps, UML and architecture diagrams, the data schema, security testing, DevOps evidence, running costs, change management and a walkthrough of the final system.

- [YouTube walkthrough](https://youtu.be/ybFxeS9y7XM)

## Authors

Nexus Dev Team, client: Ratul Singh (Financial Advisor at Liberty)

- Thabo Setsubi ST10445734
- Samuel Sossen ST10445164
- Ethan Jansen ST10440914
- Adam Malander ST10440725

## References

- [ChatGPT](https://chatgpt.com/share/6ac3ec99-a1cc-83e9-a2ce-55da8710c37f)
- [Gemini](https://share.gemini.google/RhsGUFd4msFD)
