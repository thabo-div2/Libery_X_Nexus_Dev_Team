# Liberty X Nexus CRM

Nexus is a web-based client relationship and financial
advisory management system buil with ASP .NET Core and 
deployed to Microsoft Azure. The solution provides 
separate Advisor and Client experiences and supports
client management, cases, policies, meetings, documents, 
messaging, notifications, and dashboard reporting.

## Features

### Advisor

-   Secure advisor authentication and role-based authorization
-   Advisor dashboard with active cases, awaiting documents, meetings,
    pipeline value, deadlines, and institution statistics
-   Client creation and management
-   Case and policy workflow management
-   Client document management
-   Client/advisor messaging
-   Meeting and calendar management
-   Notifications and client queries
-   AI assistant functionality

### Client

-   Secure client authentication
-   View policies and case information
-   Upload and access permitted documents
-   Communicate with the assigned advisor
-   View meetings and notifications
-   Submit queries

## Technology Stack

-   ASP.NET Core / .NET 10
-   C#
-   Entity Framework Core
-   ASP.NET Core Identity
-   JWT authentication
-   SQL Server / Azure SQL Database
-   Azure App Service
-   Azure Blob Storage
-   Azure Managed Identity
-   Swagger
-   GitHub Actions CI/CD

## Solution Structure

``` text
LibertyXNexusCRMWebApp/
├── API/        # ASP.NET Core Web API
├── API.Tests/  # Unit Tests
├── frontend/   # Web frontend
├── Shared/     # Shared models and enums
└── LibertyXNexusCRMWebApp.slnx
```

## Local Setup

### Prerequisites

-   .NET 10 SDK
-   Visual Studio 2022 or later with ASP.NET and web development tools
-   SQL Server or access to the configured Azure SQL database
-   Azure Storage access if document functionality is required

### Clone and restore

``` bash
git clone <repository-url>
cd Libery_X_Nexus_Dev_Team
dotnet restore LibertyXNexusCRMWebApp/LibertyXNexusCRMWebApp.slnx
```

### Build

``` bash
dotnet build LibertyXNexusCRMWebApp/LibertyXNexusCRMWebApp.slnx
```

### Run tests

``` bash
dotnet test LibertyXNexusCRMWebApp/LibertyXNexusCRMWebApp.slnx
```

### Run the applications

Start the API and frontend projects from Visual Studio, or run each
project with `dotnet run`.

## Configuration

Sensitive values should not be committed to source control. Use User
Secrets for local development and Azure App Service environment
variables/connection strings in production.

Example configuration:

``` json
{
  "ConnectionStrings": {
    "DefaultConnection": "<database-connection-string>"
  },
  "BlobStorage": {
    "AccountUrl": "https://<storage-account>.blob.core.windows.net",
    "ContainerName": "client-documents"
  },
  "Jwt": {
    "Key": "<secure-key-at-least-32-bytes>",
    "Issuer": "LibertyXNexus.API",
    "Audience": "LibertyXNexus.Clients",
    "ExpiryMinutes": 60
  },
  "Frontend": {
    "BaseUrl": "<frontend-url>"
  },
  "AlphaVantage": {
    "BaseUrl": "https://www.alphavantage.co/query",
    "ApiKey": "<api-key>"
  }
}
```

The frontend requires:

``` text
ApiSettings__BaseUrl=https://<api-app-service>/api/
```

## Database

Entity Framework Core is used for database access and migrations.

Typical migration commands:

``` bash
dotnet ef migrations add <MigrationName> --project LibertyXNexusCRMWebApp/API
dotnet ef database update --project LibertyXNexusCRMWebApp/API
```

The production database is hosted in Azure SQL.

## Azure Blob Storage

Client documents are stored in Azure Blob Storage.

The API App Service uses a system-assigned managed identity with the
`Storage Blob Data Contributor` role on the storage account. This allows
the deployed API to access blobs without storing the storage account key
in application configuration.

## Authentication and Authorization

The system uses ASP.NET Core Identity and JWT bearer authentication.

Application roles include:

-   Advisor
-   Client

Protected API endpoints require a valid JWT. Authorization is enforced
by the API rather than Azure App Service Easy Auth.

## Swagger

Swagger is available on the deployed API for endpoint testing
and documentation:


[Swagger](https://nexus-devteam-g8ece0ftgwbwevdt.centralindia-01.azurewebsites.net/swagger)


## Deployed Applications

### API

``` text
https://nexus-devteam-g8ece0ftgwbwevdt.centralindia-01.azurewebsites.net/
```

### Frontend

``` text
https://libertyxnexus-web-thabo-djegfafwe4h3ayc6.centralindia-01.azurewebsites.net/
```

## CI/CD

GitHub Actions provides continuous integration and deployment.

For pushes to `main`, the workflow:

1.  Restores dependencies.
2.  Builds the .NET solution in Release configuration.
3.  Runs automated tests.
4.  Publishes and deploys the API to the `nexus-devteam` Azure App
    Service.
5.  Publishes and deploys the frontend to the `libertyxnexus-web-thabo`
    Azure App Service.

Pull requests to `main` run the build and test stages without deploying.

The deployment workflow uses GitHub repository secrets containing Azure
App Service publish profiles:

``` text
AZURE_API_PUBLISH_PROFILE
AZURE_FRONTEND_PUBLISH_PROFILE
```

Publish profiles and other credentials must never be committed to the
repository.

## Production Environment Variables

The API uses Azure App Service settings including:

``` text
AlphaVantage__ApiKey
AlphaVantage__BaseUrl
BlobStorage__AccountUrl
BlobStorage__ContainerName
Email__Smtp__EnableSsl
Email__Smtp__FromEmail
Email__Smtp__FromName
Email__Smtp__Host
Email__Smtp__Password
Email__Smtp__Port
Email__Smtp__Username
Frontend__BaseUrl
Jwt__Audience
Jwt__Issuer
Jwt__Key
Seed__SampleDocuments
```

The database connection is configured separately in the Azure App
Service **Connection strings** section as `DefaultConnection`.

## Security

-   Secrets and passwords must not be committed to Git.
-   Production secrets are stored in Azure App Service configuration or
    GitHub Actions secrets.
-   JWT signing keys should be strong and at least 32 bytes.
-   Azure Managed Identity is used for Blob Storage access.
-   Exposed or compromised credentials should be rotated immediately.
-   HTTPS is used for communication between deployed services.

## Youtube Presentation

- [Youtube](https://youtu.be/ybFxeS9y7XM)

## Authors

- Thabo Setsubi ST10445734
- Sam Sossen ST10445164
- Ethan Jansen ST10440914
- Adam Malander ST10440725

## Reference
- [ChatGPT](https://chatgpt.com/share/6ac3ec99-a1cc-83e9-a2ce-55da8710c37f)
- [Gemini](https://share.gemini.google/RhsGUFd4msFD)

