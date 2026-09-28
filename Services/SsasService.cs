using System.Globalization;
using EnterpriseBIDashboard.Models;
using Microsoft.AnalysisServices.AdomdClient;

namespace EnterpriseBIDashboard.Services;

public sealed class SsasService : ISsasService
{
    private const string SalesAmount = "[Measures].[Line Total - Fact Sales]";
    private const string PurchaseAmount = "[Measures].[Line Total]";
    private const string SalesQuantity = "[Measures].[Quantity]";
    private const string PurchaseQuantity = "[Measures].[Ordered Quantity]";
    private const string SalesTax = "[Measures].[Tax Amount]";
    private const string SalesDiscount = "[Measures].[Discount Amount]";
    private const string CalculatedGrossMargin = "[Measures].[Marge Brute]";
    private const string CalculatedUndeliveredQuantity = "[Measures].[Quantite Non Livree]";
    private const string CalculatedPurchaseSalesRatio = "[Measures].[Taux Achats Ventes]";
    private const string CalculatedDeliveryRate = "[Measures].[Taux Livraison]";

    private static readonly DimensionSpec ProductCode = new("[Dim Product].[Product Code]", "[Dim Product].[Product Code].[Product Code]");
    private static readonly DimensionSpec ProductCategory = new("[Dim Product].[Category ID]", "[Dim Product].[Category ID].[Category ID]");
    private static readonly DimensionSpec ProductBrand = new("[Dim Product].[Brand ID]", "[Dim Product].[Brand ID].[Brand ID]");
    private static readonly DimensionSpec ProductColor = new("[Dim Product].[Color]", "[Dim Product].[Color].[Color]");
    private static readonly DimensionSpec ProductSize = new("[Dim Product].[Size]", "[Dim Product].[Size].[Size]");
    private static readonly DimensionSpec CustomerCompany = new("[DimCustomer].[Company Name]", "[DimCustomer].[Company Name].[Company Name]");
    private static readonly DimensionSpec CustomerCountry = new("[DimCustomer].[Country]", "[DimCustomer].[Country].[Country]");
    private static readonly DimensionSpec CustomerStatus = new("[DimCustomer].[Customer Status]", "[DimCustomer].[Customer Status].[Customer Status]");
    private static readonly DimensionSpec SupplierCode = new("[Dim Supplier].[Supplier Code]", "[Dim Supplier].[Supplier Code].[Supplier Code]");
    private static readonly DimensionSpec Year = new("[Dim Date].[Year Number]", "[Dim Date].[Year Number].[Year Number]");
    private static readonly DimensionSpec Month = new("[Dim Date].[Month Name]", "[Dim Date].[Month Name].[Month Name]");
    private static readonly DimensionSpec Quarter = new("[Dim Date].[Quarter Number]", "[Dim Date].[Quarter Number].[Quarter Number]");
    private static readonly DimensionSpec EmployeeFirstName = new("[Dim Employee].[First Name]", "[Dim Employee].[First Name].[First Name]");

    private readonly string _connectionString;
    private readonly string _cubeName;
    private readonly string _serverName;
    private readonly int _commandTimeout;
    private readonly ILogger<SsasService> _logger;

    public SsasService(IConfiguration configuration, ILogger<SsasService> logger)
    {
        _connectionString = configuration["Ssas:ConnectionString"]
            ?? "Data Source=localhost;Catalog=EnterpriseCube;Integrated Security=SSPI;";
        _cubeName = configuration["Ssas:CubeName"] ?? "EnterpriseCube";
        _serverName = configuration["Ssas:ServerName"] ?? "localhost";
        _commandTimeout = configuration.GetValue("Ssas:CommandTimeoutSeconds", 60);
        _logger = logger;
    }

    public Task<SsasQueryResult> ExecuteMdxAsync(string mdx)
    {
        return Task.Run(() =>
        {
            using var connection = new AdomdConnection(_connectionString);

            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur de connexion SSAS.");
                throw new InvalidOperationException(
                    "Impossible de se connecter au cube SSAS EnterpriseCube sur localhost. Verifiez que SQL Server Analysis Services est demarre, que le cube est deploye et que l'utilisateur Windows dispose des droits de lecture.",
                    ex);
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = mdx;
                command.CommandTimeout = _commandTimeout;

                using var reader = command.ExecuteReader();
                var columns = Enumerable.Range(0, reader.FieldCount)
                    .Select(reader.GetName)
                    .ToList();

                var rows = new List<IReadOnlyDictionary<string, object?>>();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    rows.Add(row);
                }

                return new SsasQueryResult(columns, rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur MDX SSAS.");
                throw new InvalidOperationException(
                    $"La requete MDX n'a pas pu etre executee. Verifiez les noms exacts des dimensions, hierarchies et mesures dans le cube. Detail SSAS : {ex.Message}",
                    ex);
            }
        });
    }

    public async Task<ConnectionStatusModel> GetConnectionStatus()
    {
        try
        {
            await ExecuteMdxAsync($$"""
                SELECT { {{SalesAmount}} } ON COLUMNS
                FROM [{{_cubeName}}]
                """);

            return new ConnectionStatusModel
            {
                IsConnected = true,
                ServerName = _serverName,
                CubeName = _cubeName,
                Message = "Cube connecte"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionStatusModel
            {
                IsConnected = false,
                ServerName = _serverName,
                CubeName = _cubeName,
                Message = ex.Message
            };
        }
    }

    public async Task<FilterOptionsModel> GetFilterOptions(DashboardFilters filters)
    {
        var years = QueryFilterOptions(Year, SalesAmount, 50, SortMode.CaptionAscending, new DashboardFilters());
        var months = QueryFilterOptions(Month, SalesAmount, 50, SortMode.CaptionAscending, new DashboardFilters());
        var products = QueryFilterOptions(ProductCode, SalesAmount, 100, SortMode.ValueDescending, filters);
        var customers = QueryFilterOptions(CustomerCompany, SalesAmount, 100, SortMode.ValueDescending, filters);
        var suppliers = QueryFilterOptions(SupplierCode, PurchaseAmount, 100, SortMode.ValueDescending, filters);
        var countries = QueryFilterOptions(CustomerCountry, SalesAmount, 100, SortMode.ValueDescending, filters);

        await Task.WhenAll(years, months, products, customers, suppliers, countries);

        return new FilterOptionsModel
        {
            Years = years.Result,
            Months = months.Result,
            Products = products.Result,
            Customers = customers.Result,
            Suppliers = suppliers.Result,
            Countries = countries.Result
        };
    }

    public async Task<KpiModel> GetKpis(DashboardFilters filters)
    {
        var mdx = $$"""
            SELECT
                {
                    {{SalesAmount}},
                    {{PurchaseAmount}},
                    {{SalesQuantity}},
                    {{PurchaseQuantity}},
                    {{SalesTax}},
                    {{SalesDiscount}}
                } ON COLUMNS
            FROM [{{_cubeName}}]
            {{BuildWhere(filters)}}
            """;

        var result = await ExecuteMdxAsync(mdx);
        var values = result.Rows.FirstOrDefault() is { } row
            ? GetNumericValues(row).ToList()
            : [];

        return new KpiModel
        {
            TotalSales = values.ElementAtOrDefault(0),
            TotalPurchases = values.ElementAtOrDefault(1),
            GrossMargin = 0,
            PurchaseSalesRatio = 0,
            TotalQuantitySold = values.ElementAtOrDefault(2),
            TotalQuantityPurchased = values.ElementAtOrDefault(3),
            TotalSalesTax = values.ElementAtOrDefault(4),
            TotalSalesDiscount = values.ElementAtOrDefault(5)
        };
    }

    public async Task<CalculatedKpiModel> GetCalculatedKpis(DashboardFilters filters)
    {
        var mdx = $$"""
            SELECT
            {
                {{CalculatedGrossMargin}},
                {{CalculatedUndeliveredQuantity}},
                {{CalculatedPurchaseSalesRatio}},
                {{CalculatedDeliveryRate}}
            } ON COLUMNS
            FROM [{{_cubeName}}]
            {{BuildWhere(filters)}}
            """;

        try
        {
            var result = await ExecuteMdxAsync(mdx);
            var values = result.Rows.FirstOrDefault() is { } row
                ? GetNumericValues(row).ToList()
                : [];

            return new CalculatedKpiModel
            {
                MargeBrute = values.ElementAtOrDefault(0),
                QuantiteNonLivree = values.ElementAtOrDefault(1),
                TauxAchatsVentes = values.ElementAtOrDefault(2),
                TauxLivraison = values.ElementAtOrDefault(3)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "La requete groupée des membres calcules a echoue. Fallback mesure par mesure.");
            var margeBrute = await TryGetCalculatedMeasure(CalculatedGrossMargin, filters);
            var quantiteNonLivree = await TryGetCalculatedMeasure(CalculatedUndeliveredQuantity, filters);
            var tauxAchatsVentes = await TryGetCalculatedMeasure(CalculatedPurchaseSalesRatio, filters);
            var tauxLivraison = await TryGetCalculatedMeasure(CalculatedDeliveryRate, filters);

            return new CalculatedKpiModel
            {
                MargeBrute = margeBrute,
                QuantiteNonLivree = quantiteNonLivree,
                TauxAchatsVentes = tauxAchatsVentes,
                TauxLivraison = tauxLivraison,
                IsAvailable = margeBrute.HasValue || quantiteNonLivree.HasValue || tauxAchatsVentes.HasValue || tauxLivraison.HasValue,
                Message = "Une ou plusieurs mesures calculees MDX sont indisponibles dans le cube."
            };
        }
    }

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByProduct(DashboardFilters filters) =>
        QuerySingle(ProductCode, SalesAmount, filters, 15, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetTopProducts(DashboardFilters filters) =>
        QuerySingle(ProductCode, SalesAmount, filters, 10, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetLowProducts(DashboardFilters filters) =>
        QuerySingle(ProductCode, SalesAmount, filters, 10, SortMode.ValueAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByYear(DashboardFilters filters) =>
        QuerySingle(Year, SalesAmount, filters, 50, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByMonth(DashboardFilters filters) =>
        QuerySingle(Month, SalesAmount, filters, 12, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByQuarter(DashboardFilters filters) =>
        QuerySingle(Quarter, SalesAmount, filters, 8, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByCustomer(DashboardFilters filters) =>
        QuerySingle(CustomerCompany, SalesAmount, filters, 15, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetTopCustomers(DashboardFilters filters) =>
        QuerySingle(CustomerCompany, SalesAmount, filters, 10, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByCountry(DashboardFilters filters) =>
        QuerySingle(CustomerCountry, SalesAmount, filters, 12, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByCustomerStatus(DashboardFilters filters) =>
        QuerySingle(CustomerStatus, SalesAmount, filters, 12, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetPurchasesBySupplier(DashboardFilters filters) =>
        QuerySingle(SupplierCode, PurchaseAmount, filters, 15, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetTopSuppliers(DashboardFilters filters) =>
        QuerySingle(SupplierCode, PurchaseAmount, filters, 10, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetPurchasesByYear(DashboardFilters filters) =>
        QuerySingle(Year, PurchaseAmount, filters, 50, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetPurchasesByMonth(DashboardFilters filters) =>
        QuerySingle(Month, PurchaseAmount, filters, 12, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetPurchasesByProduct(DashboardFilters filters) =>
        QuerySingle(ProductCode, PurchaseAmount, filters, 15, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesVsPurchasesByYear(DashboardFilters filters) =>
        QueryTwoMeasures(Year, SalesAmount, PurchaseAmount, filters, 50, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetQuantitySalesVsPurchases(DashboardFilters filters) =>
        QueryTwoMeasures(Year, SalesQuantity, PurchaseQuantity, filters, 50, SortMode.CaptionAscending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByEmployee(DashboardFilters filters) =>
        QuerySingle(EmployeeFirstName, SalesAmount, filters, 15, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByCategory(DashboardFilters filters) =>
        QuerySingle(ProductCategory, SalesAmount, filters, 12, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByBrand(DashboardFilters filters) =>
        QuerySingle(ProductBrand, SalesAmount, filters, 12, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesByColor(DashboardFilters filters) =>
        QuerySingle(ProductColor, SalesAmount, filters, 12, SortMode.ValueDescending);

    public Task<IReadOnlyList<ChartDataPoint>> GetSalesBySize(DashboardFilters filters) =>
        QuerySingle(ProductSize, SalesAmount, filters, 12, SortMode.ValueDescending);

    private async Task<IReadOnlyList<FilterOption>> QueryFilterOptions(
        DimensionSpec dimension,
        string measure,
        int count,
        SortMode sortMode,
        DashboardFilters filters)
    {
        var rows = BuildRowsExpression(dimension, measure, count, sortMode, GetAxisFilterValue(dimension, filters));
        var mdx = $$"""
            SELECT
                { {{measure}} } ON COLUMNS,
                NON EMPTY
                    {{rows}}
                    DIMENSION PROPERTIES MEMBER_CAPTION, MEMBER_UNIQUE_NAME ON ROWS
            FROM [{{_cubeName}}]
            {{BuildWhere(filters, dimension)}}
            """;

        return MapFilterOptions(await ExecuteMdxAsync(mdx));
    }

    private async Task<IReadOnlyList<ChartDataPoint>> QuerySingle(
        DimensionSpec dimension,
        string measure,
        DashboardFilters filters,
        int count,
        SortMode sortMode)
    {
        var rows = BuildRowsExpression(dimension, measure, count, sortMode, GetAxisFilterValue(dimension, filters));
        var mdx = $$"""
            SELECT
                { {{measure}} } ON COLUMNS,
                NON EMPTY
                    {{rows}} ON ROWS
            FROM [{{_cubeName}}]
            {{BuildWhere(filters, dimension)}}
            """;

        return MapSingleMeasure(await ExecuteMdxAsync(mdx));
    }

    private async Task<decimal?> TryGetCalculatedMeasure(string measureUniqueName, DashboardFilters filters)
    {
        var mdx = $$"""
            SELECT { {{measureUniqueName}} } ON COLUMNS
            FROM [{{_cubeName}}]
            {{BuildWhere(filters)}}
            """;

        try
        {
            var result = await ExecuteMdxAsync(mdx);
            return result.Rows.FirstOrDefault() is { } row
                ? GetNumericValues(row).FirstOrDefault()
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mesure calculee indisponible : {MeasureUniqueName}", measureUniqueName);
            return null;
        }
    }

    private async Task<IReadOnlyList<ChartDataPoint>> QueryTwoMeasures(
        DimensionSpec dimension,
        string primaryMeasure,
        string secondaryMeasure,
        DashboardFilters filters,
        int count,
        SortMode sortMode)
    {
        var rows = BuildRowsExpression(dimension, primaryMeasure, count, sortMode, GetAxisFilterValue(dimension, filters));
        var mdx = $$"""
            SELECT
                { {{primaryMeasure}}, {{secondaryMeasure}} } ON COLUMNS,
                NON EMPTY
                    {{rows}} ON ROWS
            FROM [{{_cubeName}}]
            {{BuildWhere(filters, dimension)}}
            """;

        return MapTwoMeasures(await ExecuteMdxAsync(mdx));
    }

    private static string BuildRowsExpression(DimensionSpec dimension, string measure, int count, SortMode sortMode, string? axisFilterValue)
    {
        var members = string.IsNullOrWhiteSpace(axisFilterValue)
            ? $"{dimension.LevelUniqueName}.MEMBERS"
            : $"{{ {CreateStrToMember(dimension, axisFilterValue)} }}";

        return sortMode switch
        {
            SortMode.CaptionAscending => $"ORDER({members}, {dimension.HierarchyUniqueName}.CURRENTMEMBER.MEMBER_CAPTION, BASC)",
            SortMode.ValueAscending => $"BOTTOMCOUNT(FILTER({members}, NOT ISEMPTY({measure})), {count}, {measure})",
            _ => $"TOPCOUNT({members}, {count}, {measure})"
        };
    }

    private string BuildWhere(DashboardFilters filters, DimensionSpec? axisDimension = null)
    {
        var members = new List<string>();
        AddFilter(members, filters.Year, Year, axisDimension);
        AddFilter(members, filters.Month, Month, axisDimension);
        AddFilter(members, filters.Product, ProductCode, axisDimension);
        AddFilter(members, filters.Customer, CustomerCompany, axisDimension);
        AddFilter(members, filters.Supplier, SupplierCode, axisDimension);
        AddFilter(members, filters.Country, CustomerCountry, axisDimension);

        return members.Count == 0
            ? string.Empty
            : $"WHERE ({string.Join(", ", members)})";
    }

    private static void AddFilter(ICollection<string> members, string? value, DimensionSpec dimension, DimensionSpec? axisDimension)
    {
        if (string.IsNullOrWhiteSpace(value) || dimension == axisDimension)
        {
            return;
        }

        members.Add(CreateStrToMember(dimension, value));
    }

    private static string? GetAxisFilterValue(DimensionSpec dimension, DashboardFilters filters)
    {
        if (dimension == Year)
        {
            return filters.Year;
        }

        if (dimension == Month)
        {
            return filters.Month;
        }

        if (dimension == ProductCode)
        {
            return filters.Product;
        }

        if (dimension == CustomerCompany)
        {
            return filters.Customer;
        }

        if (dimension == SupplierCode)
        {
            return filters.Supplier;
        }

        if (dimension == CustomerCountry)
        {
            return filters.Country;
        }

        return null;
    }

    private static string CreateStrToMember(DimensionSpec dimension, string value)
    {
        var memberUniqueName = IsMemberUniqueName(value, dimension)
            ? value.Trim()
            : $"{dimension.HierarchyUniqueName}.&[{EscapeMdxKey(value)}]";
        return $"StrToMember('{EscapeMdxString(memberUniqueName)}', CONSTRAINED)";
    }

    private static string EscapeMdxKey(string value) => value.Trim().Replace("]", "]]", StringComparison.Ordinal);

    private static string EscapeMdxString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static bool IsMemberUniqueName(string value, DimensionSpec dimension)
    {
        return value.TrimStart().StartsWith($"{dimension.HierarchyUniqueName}.", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<FilterOption> MapFilterOptions(SsasQueryResult result)
    {
        return result.Rows
            .Select(row =>
            {
                var label = GetLabel(row);
                var uniqueName = row
                    .Where(item => item.Key.Contains("MEMBER_UNIQUE_NAME", StringComparison.OrdinalIgnoreCase))
                    .Select(item => Convert.ToString(item.Value, CultureInfo.InvariantCulture))
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

                return new FilterOption
                {
                    Label = label,
                    Value = uniqueName ?? label
                };
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Label) && !string.IsNullOrWhiteSpace(option.Value))
            .DistinctBy(option => option.Value)
            .ToList();
    }

    private static IReadOnlyList<ChartDataPoint> NormalizePoints(IEnumerable<ChartDataPoint> points)
    {
        return points
            .Where(point => !string.IsNullOrWhiteSpace(point.Label))
            .ToList();
    }

    private static IReadOnlyList<ChartDataPoint> MapSingleMeasure(SsasQueryResult result)
    {
        return result.Rows
            .Select(row => new ChartDataPoint
            {
                Label = GetLabel(row),
                Value = GetNumericValues(row).FirstOrDefault()
            })
            .Where(point => !string.IsNullOrWhiteSpace(point.Label))
            .ToList();
    }

    private static IReadOnlyList<ChartDataPoint> MapTwoMeasures(SsasQueryResult result)
    {
        return result.Rows
            .Select(row =>
            {
                var values = GetNumericValues(row).ToList();
                return new ChartDataPoint
                {
                    Label = GetLabel(row),
                    Value = values.ElementAtOrDefault(0),
                    SecondaryValue = values.ElementAtOrDefault(1)
                };
            })
            .Where(point => !string.IsNullOrWhiteSpace(point.Label))
            .ToList();
    }

    private static string GetLabel(IReadOnlyDictionary<string, object?> row)
    {
        var labelCandidate = row
            .Where(item => !IsMeasureColumn(item.Key))
            .OrderByDescending(item => IsCaptionColumn(item.Key))
            .Select(item => Convert.ToString(item.Value, CultureInfo.InvariantCulture))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return labelCandidate ?? "Non renseigne";
    }

    private static IEnumerable<decimal> GetNumericValues(IReadOnlyDictionary<string, object?> row)
    {
        var measureValues = row
            .Where(item => IsMeasureColumn(item.Key))
            .Select(item => TryToDecimal(item.Value, out var value) ? value : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        if (measureValues.Count > 0)
        {
            return measureValues;
        }

        return row
            .Skip(1)
            .Select(item => TryToDecimal(item.Value, out var value) ? value : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value);
    }

    private static bool IsMeasureColumn(string columnName)
    {
        return columnName.Contains("[Measures]", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Measure", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Line Total", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Quantity", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Amount", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Unit Price", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Margin", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Ratio", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCaptionColumn(string columnName)
    {
        return columnName.Contains("MEMBER_CAPTION", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Caption", StringComparison.OrdinalIgnoreCase)
            || columnName.Contains("Name", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryToDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case null:
                result = 0;
                return false;
            case decimal decimalValue:
                result = decimalValue;
                return true;
            case double doubleValue:
                result = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                return true;
            case float floatValue:
                result = Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case string stringValue when decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private sealed record DimensionSpec(string HierarchyUniqueName, string LevelUniqueName);

    private enum SortMode
    {
        CaptionAscending,
        ValueAscending,
        ValueDescending
    }
}
