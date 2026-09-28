namespace EnterpriseBIDashboard.Models;

public sealed class KpiModel
{
    public decimal TotalSales { get; init; }
    public decimal TotalPurchases { get; init; }
    public decimal GrossMargin { get; init; }
    public decimal PurchaseSalesRatio { get; init; }
    public decimal TotalQuantitySold { get; init; }
    public decimal TotalQuantityPurchased { get; init; }
    public decimal TotalSalesTax { get; init; }
    public decimal TotalSalesDiscount { get; init; }
}
