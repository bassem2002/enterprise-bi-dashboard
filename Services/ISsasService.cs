using EnterpriseBIDashboard.Models;

namespace EnterpriseBIDashboard.Services;

public interface ISsasService
{
    Task<SsasQueryResult> ExecuteMdxAsync(string mdx);
    Task<ConnectionStatusModel> GetConnectionStatus();
    Task<FilterOptionsModel> GetFilterOptions(DashboardFilters filters);
    Task<KpiModel> GetKpis(DashboardFilters filters);
    Task<CalculatedKpiModel> GetCalculatedKpis(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByProduct(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetTopProducts(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetLowProducts(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByYear(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByMonth(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByQuarter(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByCustomer(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetTopCustomers(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByCountry(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByCustomerStatus(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetPurchasesBySupplier(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetTopSuppliers(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetPurchasesByYear(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetPurchasesByMonth(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetPurchasesByProduct(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesVsPurchasesByYear(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetQuantitySalesVsPurchases(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByEmployee(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByCategory(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByBrand(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesByColor(DashboardFilters filters);
    Task<IReadOnlyList<ChartDataPoint>> GetSalesBySize(DashboardFilters filters);
}
