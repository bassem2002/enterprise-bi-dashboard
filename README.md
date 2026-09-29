# Enterprise BI Dashboard

<p align="center">
  <img src="https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/SSAS%20%7C%20MDX-Business%20Intelligence-0F766E?style=for-the-badge" alt="SSAS and MDX" />
  <img src="https://img.shields.io/badge/Chart.js-FF6384?style=for-the-badge&logo=chartdotjs&logoColor=white" alt="Chart.js" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-14B8A6?style=for-the-badge" alt="MIT License" /></a>
</p>

<p align="center"><a href="#technology-stack">Stack</a> · <a href="#architecture">Architecture</a> · <a href="#running-the-application">Run locally</a></p>

An ASP.NET Core MVC application connected directly to the `EnterpriseCube` SSAS Multidimensional cube running on `localhost`.

This project delivers a professional business intelligence dashboard with KPIs, global filters, specialized analytics views, Chart.js visualizations, and JSON endpoints for every analysis.

## Key Features

- Executive overview combining sales, purchases, quantities, taxes, discounts, and calculated OLAP measures
- Dedicated analytics views for products, customers, suppliers, employees, dates, categories, brands, colors, and sizes
- Global Year, Month, Product, Customer, Supplier, and Country filters backed by SSAS member unique names
- Reusable MDX query construction centralized in a dedicated service
- Chart.js visualizations and responsive Bootstrap dashboard components
- JSON API endpoints for every dashboard analysis
- Live SSAS connection-status reporting
- Stable error and empty states when the cube or a calculated measure is unavailable

## Business Value

The dashboard converts multidimensional warehouse data into decision-ready information. Executives can compare sales and purchasing activity, monitor margins and delivery indicators, identify high- and low-performing products, and analyze customer or supplier concentration. Operational users can refine the same measures through shared filters without writing MDX or interacting directly with SSAS tooling.

## Author

Developed by **Bassem Wali** — [bassem2002](https://github.com/bassem2002).

## Technology Stack

- ASP.NET Core MVC (`net8.0`)
- C#
- ADOMD.NET: `Microsoft.AnalysisServices.AdomdClient.NetCore.retail.amd64`
- MDX
- Chart.js
- Bootstrap 5

## BI Configuration

```json
"SqlServer": {
  "Server": "DESKTOP-28L6KA8\\KHALED",
  "DataWarehouse": "EnterpriseDW"
},
"Ssas": {
  "ServerName": "localhost",
  "CubeName": "EnterpriseCube",
  "ConnectionString": "Data Source=localhost;Catalog=EnterpriseCube;Integrated Security=SSPI;",
  "CommandTimeoutSeconds": 60
}
```

If the SSAS database and cube use different names, update `Catalog` in `appsettings.json` and keep `CubeName` set to the name used in `FROM [EnterpriseCube]`.

## Architecture

```text
Controllers/
  DashboardController.cs
Models/
  ApiResponse.cs
  ChartDataPoint.cs
  ConnectionStatusModel.cs
  DashboardFilters.cs
  DashboardViewModel.cs
  FilterOption.cs
  FilterOptionsModel.cs
  KpiModel.cs
  SsasQueryResult.cs
Services/
  ISsasService.cs
  SsasService.cs
ViewModels/
  AnalysisPageViewModel.cs
Views/Dashboard/
  Index.cshtml
  Analysis.cshtml
  _ChartPanel.cshtml
  _FilterBar.cshtml
wwwroot/
  css/site.css
  js/dashboard.js
```

All MDX logic is centralized in `Services/SsasService.cs`. The views contain no MDX queries.

### Request and Data Flow

```text
┌──────────────────────────────────────┐
│ Browser                              │
│ Razor views · Filters · Chart.js     │
└──────────────────┬───────────────────┘
                   │ MVC pages / JSON requests
                   ▼
┌──────────────────────────────────────┐
│ DashboardController                  │
│ Routes · validation · API responses  │
└──────────────────┬───────────────────┘
                   │ ISsasService
                   ▼
┌──────────────────────────────────────┐
│ SsasService                          │
│ Filter tuples · MDX · result mapping │
└──────────────────┬───────────────────┘
                   │ ADOMD.NET
                   ▼
┌──────────────────────────────────────┐
│ SQL Server Analysis Services         │
│ EnterpriseCube · dimensions · KPIs   │
└──────────────────────────────────────┘
```

### Detailed Project Structure

```text
EnterpriseBIDashboard/
├── Controllers/
│   ├── DashboardController.cs         # MVC pages and dashboard JSON endpoints
│   └── HomeController.cs              # Default MVC routes and errors
├── Models/                            # Filters, KPIs, chart points and API contracts
├── Services/
│   ├── ISsasService.cs                # SSAS service abstraction
│   └── SsasService.cs                 # Connection, MDX generation and result mapping
├── ViewModels/
│   └── AnalysisPageViewModel.cs       # Shared analysis-page presentation model
├── Views/
│   ├── Dashboard/                     # Overview, analysis pages and partial views
│   ├── Home/                          # Default MVC pages
│   └── Shared/                        # Layout, validation and error views
├── wwwroot/
│   ├── css/site.css                   # Dashboard presentation
│   ├── js/dashboard.js                # Filters, API calls and charts
│   └── lib/                           # Bootstrap, jQuery and validation assets
├── Properties/launchSettings.json     # Local launch profiles
├── appsettings.json                   # SSAS and warehouse configuration
├── Program.cs                         # Dependency injection and MVC pipeline
└── EnterpriseBIDashboard.csproj
```

## MVC Pages

- `/` or `/Dashboard/Index`: main dashboard
- `/Dashboard/SalesAnalytics`
- `/Dashboard/PurchasesAnalytics`
- `/Dashboard/ProductsAnalytics`
- `/Dashboard/CustomersAnalytics`
- `/Dashboard/SuppliersAnalytics`
- `/Dashboard/ExecutiveSummary`

## Global Filters

The filter bar applies the following dimensions to compatible endpoints:

- Year
- Month
- Product
- Customer
- Supplier
- Customer country

Filter values are loaded from the cube using `MEMBER_UNIQUE_NAME`. This prevents errors when a displayed label differs from the member's MDX key.

## JSON Endpoints

```text
GET /api/dashboard/connection-status
GET /api/dashboard/filter-options
GET /api/dashboard/kpis
GET /api/dashboard/calculated-kpis
GET /api/dashboard/sales-by-product
GET /api/dashboard/top-products
GET /api/dashboard/low-products
GET /api/dashboard/sales-by-year
GET /api/dashboard/sales-by-month
GET /api/dashboard/sales-by-quarter
GET /api/dashboard/sales-by-customer
GET /api/dashboard/top-customers
GET /api/dashboard/sales-by-country
GET /api/dashboard/sales-by-status
GET /api/dashboard/purchases-by-supplier
GET /api/dashboard/top-suppliers
GET /api/dashboard/purchases-by-year
GET /api/dashboard/purchases-by-month
GET /api/dashboard/purchases-by-product
GET /api/dashboard/sales-vs-purchases
GET /api/dashboard/quantity-sales-vs-purchases
GET /api/dashboard/sales-by-employee
GET /api/dashboard/sales-by-category
GET /api/dashboard/sales-by-brand
GET /api/dashboard/sales-by-color
GET /api/dashboard/sales-by-size
```

Each endpoint returns:

```json
{
  "success": true,
  "message": null,
  "data": []
}
```

If an SSAS or MDX error occurs, the API returns HTTP `503` with a clear message. The interface displays the error without rendering a blank page.

## KPIs

The core KPIs are:

- Total sales: `[Measures].[Line Total - Fact Sales]`
- Total purchases: `[Measures].[Line Total]`
- Quantity sold: `[Measures].[Quantity]`
- Quantity purchased: `[Measures].[Ordered Quantity]`
- Sales tax: `[Measures].[Tax Amount]`
- Sales discounts: `[Measures].[Discount Amount]`

The Overview page separately displays four calculated members provided directly by the OLAP cube, without recalculating them with `WITH MEMBER` statements in the dashboard:

- Gross margin: `[Measures].[Marge Brute]`
- Undelivered quantity: `[Measures].[Quantite Non Livree]`
- Purchase-to-sales ratio: `[Measures].[Taux Achats Ventes]`
- Delivery rate: `[Measures].[Taux Livraison]`

These values are served by:

```text
GET /api/dashboard/calculated-kpis
```

If a calculated measure is missing or unavailable in SSAS, the endpoint remains stable and the interface displays `N/A` on the corresponding card.

## Detected MDX Dimensions

The deployed cube exposes the following dimensions, among others:

```text
[Dim Date].[Year Number]
[Dim Date].[Month Name]
[Dim Date].[Quarter Number]
[Dim Product].[Product Code]
[Dim Product].[Category ID]
[Dim Product].[Brand ID]
[Dim Product].[Color]
[Dim Product].[Size]
[DimCustomer].[Company Name]
[DimCustomer].[Country]
[DimCustomer].[Customer Status]
[Dim Supplier].[Supplier Code]
[Dim Employee].[First Name]
```

Important: in the deployed cube, the customer dimension is named `[DimCustomer]`, not `[Dim Customer]`. The `[Dim Supplier].[Supplier Name]` hierarchy is not exposed, so supplier analytics use `[Dim Supplier].[Supplier Code]`.

## MDX Examples

### Sales vs. purchases by year

```mdx
SELECT
    {
        [Measures].[Line Total - Fact Sales],
        [Measures].[Line Total]
    } ON COLUMNS,
    NON EMPTY
        ORDER(
            [Dim Date].[Year Number].[Year Number].MEMBERS,
            [Dim Date].[Year Number].CURRENTMEMBER.MEMBER_CAPTION,
            BASC
        ) ON ROWS
FROM [EnterpriseCube]
```

### Top products

```mdx
SELECT
    { [Measures].[Line Total - Fact Sales] } ON COLUMNS,
    NON EMPTY
        TOPCOUNT(
            [Dim Product].[Product Code].[Product Code].MEMBERS,
            10,
            [Measures].[Line Total - Fact Sales]
        ) ON ROWS
FROM [EnterpriseCube]
```

### Filters

Filters are generated with `StrToMember(..., CONSTRAINED)` using the `MEMBER_UNIQUE_NAME` values returned by SSAS:

```mdx
WHERE (
    StrToMember('[Dim Date].[Year Number].&[2025]', CONSTRAINED)
)
```

When a filter targets the same hierarchy as the displayed axis, the service applies the member directly to the `ROWS` axis to avoid the SSAS "hierarchy already appears in the axis" error.

## Prerequisites and Current Limitations

### Prerequisites

- .NET 8 SDK
- A reachable SQL Server Analysis Services Multidimensional instance
- A deployed and processed `EnterpriseCube`, or equivalent cube with matching measures and dimensions
- Windows credentials with permission to read the SSAS database when using `Integrated Security=SSPI`
- Network access to the configured SSAS server

### Current Limitations

- The application depends on a specific cube schema and MDX member names; differently named dimensions or measures require configuration or query changes.
- Local configuration currently references a development SQL Server machine and should be replaced for each environment.
- Integrated Windows authentication is suitable for local or intranet use but requires a deliberate identity strategy for hosted deployment.
- No automated test project currently validates MDX generation, result mapping, or controller behavior.
- The repository does not include the SSAS cube definition, warehouse deployment scripts, or representative sample data.
- No public hosted demonstration or dashboard screenshots are currently available.
- Dashboard freshness depends on the external cube being deployed, processed, and reachable.

## Running the Application

```powershell
cd EnterpriseBIDashboard
dotnet restore
dotnet run
```

Open:

```text
http://localhost:5244
```

In Visual Studio, open `EnterpriseBIDashboard.csproj`, select the `http` or `https` profile, and press `F5`.

## Verification

Useful commands:

```powershell
dotnet build
Invoke-WebRequest http://localhost:5244/api/dashboard/kpis
Invoke-WebRequest http://localhost:5244/api/dashboard/sales-vs-purchases
```

Checklist:

1. SQL Server Analysis Services is running.
2. `EnterpriseCube` is deployed and processed.
3. The current Windows user has SSAS read permissions.
4. `/api/dashboard/connection-status` returns `isConnected: true`.
5. The `/` page displays the KPIs, charts, and tables.

## Notes

No fallback values are presented as BI data. When an error occurs, the application displays an error or empty state instead of inventing values.
