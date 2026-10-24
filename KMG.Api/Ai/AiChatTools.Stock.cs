using System.Security.Claims;
using KMG.Core.DTOs.Stock;
using KMG.Core.Enums;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Ai
{
    public partial class AiChatTools
    {
        private IEnumerable<ToolDefinition> StockTools()
        {
            yield return new ToolDefinition(
                "search_materials",
                "Inventory list: materials with id, current quantity, unit, minimum threshold and low-stock flag. " +
                "Use get_material for one material's purchases, movements and prices.",
                Schema(
                    ("query", StringProp("Free text matched against material name and category.")),
                    ("lowStockOnly", BoolProp("Only materials below their minimum threshold.")),
                    ("limit", IntProp("Max rows to return (1-50, default 20)."))),
                ViewStock,
                SearchMaterialsAsync);

            yield return new ToolDefinition(
                "get_material",
                "One material by id, as on its stock details screen: quantity, threshold, category fields, purchase history " +
                "(date, quantity, unit price, transport cost, total, supplier) with purchase totals and last purchase, " +
                "other stock movements (issued to / returned from projects, opening balance, adjustments) and available price batches.",
                Schema(new[]
                {
                    ("id", IntProp("Material id.")),
                    ("movementType", EnumProp("Only movements of this type.", Enum.GetNames<MovementType>())),
                    ("limit", IntProp("Max movements to return (1-30, default 10)."))
                }, "id"),
                ViewStock,
                GetMaterialAsync);
        }

        private async Task<object> SearchMaterialsAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var query = args.GetString("query", MaxQueryLength);
            var lowStockOnly = args.GetBool("lowStockOnly");
            var limit = args.GetInt("limit", 20, 1, 50);

            var all = await Service<IStockService>().GetAllMaterialsAsync();
            var matches = all
                .Where(m => !lowStockOnly || m.IsLowStock)
                .Where(m => ArabicText.ContainsAllTokens(query, m.Name, m.CategoryName))
                .OrderByDescending(m => m.IsLowStock)
                .ThenBy(m => m.Name)
                .ToList();

            return new
            {
                totalMaterialsInStock = all.Count,
                lowStockCountOverall = all.Count(m => m.IsLowStock),
                totalMatches = matches.Count,
                returned = Math.Min(limit, matches.Count),
                materials = matches.Take(limit).Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    category = m.CategoryName,
                    unit = m.Unit,
                    quantity = m.Quantity,
                    minimumThreshold = m.MinimumThreshold,
                    isLowStock = m.IsLowStock,
                    unitPrice = m.UnitPrice
                })
            };
        }

        private async Task<object> GetMaterialAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var id = args.GetRequiredInt("id", 1, int.MaxValue);
            var movementType = args.GetEnum<MovementType>("movementType");
            var limit = args.GetInt("limit", 10, 1, 30);
            var stock = Service<IStockService>();

            var material = await stock.GetMaterialByIdAsync(id);
            if (material == null) return new { found = false, id };

            var movements = await stock.GetMovementsAsync(materialId: id);
            var batches = await stock.GetPriceBatchesAsync(id);
            return BuildMaterialDetails(material, movements, batches, movementType, limit);
        }

        internal static object BuildMaterialDetails(
            MaterialDTO material, IReadOnlyCollection<StockMovementDTO> movements, IReadOnlyCollection<StockPriceBatchDTO> batches,
            MovementType? movementType, int limit)
        {
            var purchases = movements
                .Where(m => m.MovementType == nameof(MovementType.Purchase))
                .OrderByDescending(m => m.MovementDate)
                .ToList();
            var purchasedQuantity = purchases.Sum(m => m.Quantity);
            var purchasesValue = purchases.Sum(m => m.Quantity * m.UnitPriceAtTime);
            var last = purchases.FirstOrDefault();

            var filtered = movements
                .Where(m => movementType == null || m.MovementType == movementType.ToString())
                .OrderByDescending(m => m.MovementDate)
                .ToList();

            return new
            {
                found = true,
                material = new
                {
                    id = material.Id,
                    name = material.Name,
                    category = material.CategoryName,
                    unit = material.Unit,
                    quantity = material.Quantity,
                    minimumThreshold = material.MinimumThreshold,
                    isLowStock = material.IsLowStock,
                    currentUnitPrice = material.UnitPrice,
                    stockValue = material.TotalPrice,
                    lastUpdated = Day(material.LastUpdated),
                    categoryFields = material.ExtraFieldValues.Count == 0 ? null : material.ExtraFieldValues
                },
                purchaseSummary = new
                {
                    purchasesCount = purchases.Count,
                    totalPurchasedQuantity = purchasedQuantity,
                    totalPurchasesValue = purchasesValue,
                    totalTransportCost = purchases.Sum(m => m.TransportCost),
                    weightedAverageUnitPrice = purchasedQuantity == 0 ? (decimal?)null : Math.Round(purchasesValue / purchasedQuantity, 2),
                    lastPurchase = last == null ? null : MovementRow(last)
                },
                movementCountsByType = movements
                    .GroupBy(m => m.MovementType)
                    .ToDictionary(g => MovementTypeLabel(g.Key), g => g.Count()),
                movementsFilter = movementType == null ? "all types" : MovementTypeLabel(movementType.ToString()!),
                totalMovements = filtered.Count,
                returned = Math.Min(limit, filtered.Count),
                movements = filtered.Take(limit).Select(MovementRow),
                availablePriceBatches = batches.Select(b => new
                {
                    unitPrice = b.UnitPrice,
                    availableQuantity = b.AvailableQuantity,
                    firstPurchaseDate = Day(b.FirstPurchaseDate)
                })
            };
        }

        private static object MovementRow(StockMovementDTO m) => new
        {
            id = m.Id,
            date = Day(m.MovementDate),
            type = MovementTypeLabel(m.MovementType),
            quantity = m.Quantity,
            unitPrice = m.UnitPriceAtTime,
            total = m.Quantity * m.UnitPriceAtTime,
            transportCost = m.TransportCost,
            supplier = m.SupplierName == null ? null : new { id = m.SupplierId, name = m.SupplierName },
            project = m.ProjectId == null ? null : new { id = m.ProjectId, code = m.ProjectCode, name = m.ProjectName },
            notes = ArabicText.Truncate(m.Notes, 150)
        };

        private static string MovementTypeLabel(string type) => type switch
        {
            nameof(MovementType.Purchase) => "شراء",
            nameof(MovementType.IssueToProject) => "صرف لمشروع",
            nameof(MovementType.ReturnFromProject) => "مرتجع من مشروع",
            nameof(MovementType.OpeningBalance) => "رصيد افتتاحي",
            nameof(MovementType.Adjustment) => "تسوية جرد",
            _ => type
        };
    }
}
