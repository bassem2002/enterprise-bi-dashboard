using EnterpriseBIDashboard.Models;
using EnterpriseBIDashboard.Services;
using EnterpriseBIDashboard.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBIDashboard.Controllers;

public sealed class DashboardController : Controller
{
    private readonly ISsasService _ssasService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ISsasService ssasService,
        IConfiguration configuration,
        ILogger<DashboardController> logger)
    {
        _ssasService = ssasService;
        _configuration = configuration;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new DashboardViewModel
        {
            ServerName = _configuration["Ssas:ServerName"] ?? "localhost",
            CubeName = _configuration["Ssas:CubeName"] ?? "EnterpriseCube",
            DataWarehouseName = _configuration["SqlServer:DataWarehouse"] ?? "EnterpriseDW"
        };

        return View(model);
    }

    public IActionResult SalesAnalytics() => View("Analysis", CreatePage(
        "sales",
        "Sales Analytics",
        "Analyse du chiffre d'affaires, des clients, des pays et du calendrier commercial.",
        "Sales"));

    public IActionResult PurchasesAnalytics() => View("Analysis", CreatePage(
        "purchases",
        "Purchases Analytics",
        "Analyse des achats, fournisseurs, produits achetes et quantites commandees.",
        "Purchases"));

    public IActionResult ProductsAnalytics() => View("Analysis", CreatePage(
        "products",
        "Products Analytics",
        "Performance produit par chiffre d'affaires, categorie, marque, couleur et taille.",
        "Products"));

    public IActionResult CustomersAnalytics() => View("Analysis", CreatePage(
        "customers",
        "Customers Analytics",
        "Contribution client, repartition pays, statut et segmentation commerciale.",
        "Customers"));

    public IActionResult SuppliersAnalytics() => View("Analysis", CreatePage(
        "suppliers",
        "Suppliers Analytics",
        "Pilotage fournisseurs par volume d'achats et evolution dans le temps.",
        "Suppliers"));

    public IActionResult ExecutiveSummary() => View("Analysis", CreatePage(
        "executive",
        "Executive Summary",
        "Synthese directionnelle des ventes, achats, marge estimee et tendances.",
        "Executive"));

    [HttpGet("/api/dashboard/connection-status")]
    public Task<IActionResult> ConnectionStatus() => ExecuteJson(() => _ssasService.GetConnectionStatus());

    [HttpGet("/api/dashboard/filter-options")]
    public Task<IActionResult> FilterOptions([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetFilterOptions(filters));

    [HttpGet("/api/dashboard/kpis")]
    public Task<IActionResult> ApiKpis([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetKpis(filters));

    [HttpGet("/api/dashboard/calculated-kpis")]
    public Task<IActionResult> ApiCalculatedKpis([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetCalculatedKpis(filters));

    [HttpGet("/api/dashboard/sales-by-product")]
    public Task<IActionResult> ApiSalesByProduct([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByProduct(filters));

    [HttpGet("/api/dashboard/top-products")]
    public Task<IActionResult> ApiTopProducts([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetTopProducts(filters));

    [HttpGet("/api/dashboard/low-products")]
    public Task<IActionResult> ApiLowProducts([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetLowProducts(filters));

    [HttpGet("/api/dashboard/sales-by-year")]
    public Task<IActionResult> ApiSalesByYear([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByYear(filters));

    [HttpGet("/api/dashboard/sales-by-month")]
    public Task<IActionResult> ApiSalesByMonth([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByMonth(filters));

    [HttpGet("/api/dashboard/sales-by-quarter")]
    public Task<IActionResult> ApiSalesByQuarter([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByQuarter(filters));

    [HttpGet("/api/dashboard/sales-by-customer")]
    public Task<IActionResult> ApiSalesByCustomer([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByCustomer(filters));

    [HttpGet("/api/dashboard/top-customers")]
    public Task<IActionResult> ApiTopCustomers([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetTopCustomers(filters));

    [HttpGet("/api/dashboard/sales-by-country")]
    public Task<IActionResult> ApiSalesByCountry([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByCountry(filters));

    [HttpGet("/api/dashboard/sales-by-status")]
    public Task<IActionResult> ApiSalesByStatus([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByCustomerStatus(filters));

    [HttpGet("/api/dashboard/purchases-by-supplier")]
    public Task<IActionResult> ApiPurchasesBySupplier([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetPurchasesBySupplier(filters));

    [HttpGet("/api/dashboard/top-suppliers")]
    public Task<IActionResult> ApiTopSuppliers([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetTopSuppliers(filters));

    [HttpGet("/api/dashboard/purchases-by-year")]
    public Task<IActionResult> ApiPurchasesByYear([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetPurchasesByYear(filters));

    [HttpGet("/api/dashboard/purchases-by-month")]
    public Task<IActionResult> ApiPurchasesByMonth([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetPurchasesByMonth(filters));

    [HttpGet("/api/dashboard/purchases-by-product")]
    public Task<IActionResult> ApiPurchasesByProduct([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetPurchasesByProduct(filters));

    [HttpGet("/api/dashboard/sales-vs-purchases")]
    public Task<IActionResult> ApiSalesVsPurchases([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesVsPurchasesByYear(filters));

    [HttpGet("/api/dashboard/quantity-sales-vs-purchases")]
    public Task<IActionResult> ApiQuantitySalesVsPurchases([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetQuantitySalesVsPurchases(filters));

    [HttpGet("/api/dashboard/sales-by-employee")]
    public Task<IActionResult> ApiSalesByEmployee([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByEmployee(filters));

    [HttpGet("/api/dashboard/sales-by-category")]
    public Task<IActionResult> ApiSalesByCategory([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByCategory(filters));

    [HttpGet("/api/dashboard/sales-by-brand")]
    public Task<IActionResult> ApiSalesByBrand([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByBrand(filters));

    [HttpGet("/api/dashboard/sales-by-color")]
    public Task<IActionResult> ApiSalesByColor([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesByColor(filters));

    [HttpGet("/api/dashboard/sales-by-size")]
    public Task<IActionResult> ApiSalesBySize([FromQuery] DashboardFilters filters) =>
        ExecuteJson(() => _ssasService.GetSalesBySize(filters));

    [HttpGet]
    public Task<IActionResult> GetKpis([FromQuery] DashboardFilters filters) => ApiKpis(filters);

    [HttpGet]
    public Task<IActionResult> GetSalesByProduct([FromQuery] DashboardFilters filters) => ApiSalesByProduct(filters);

    [HttpGet]
    public Task<IActionResult> GetSalesByYear([FromQuery] DashboardFilters filters) => ApiSalesByYear(filters);

    [HttpGet]
    public Task<IActionResult> GetPurchasesBySupplier([FromQuery] DashboardFilters filters) => ApiPurchasesBySupplier(filters);

    [HttpGet]
    public Task<IActionResult> GetSalesVsPurchasesByYear([FromQuery] DashboardFilters filters) => ApiSalesVsPurchases(filters);

    [HttpGet]
    public Task<IActionResult> GetTopProducts([FromQuery] DashboardFilters filters) => ApiTopProducts(filters);

    private static AnalysisPageViewModel CreatePage(string key, string title, string subtitle, string activeMenu)
    {
        return new AnalysisPageViewModel
        {
            PageKey = key,
            Title = title,
            Subtitle = subtitle,
            ActiveMenu = activeMenu
        };
    }

    private async Task<IActionResult> ExecuteJson<T>(Func<Task<T>> query)
    {
        try
        {
            var data = await query();
            return Json(ApiResponse<T>.Ok(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des donnees du dashboard.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<T>.Fail(ex.Message));
        }
    }
}
