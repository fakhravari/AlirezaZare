using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreOrdersAdminData(ShopDbContext db) : IStoreOrdersAdminData
{
    private sealed record OrderItemRow(decimal Id, decimal IdOrder, decimal IdProduct, long IdSalesPlan,
        string ProductName, string? VariantName, int Quantity, long? IdVariant, decimal IdPriceVersion,
        decimal PriceAmount, decimal DiscountPercent, decimal FinalPriceAmount, bool IsActive,
        DateTime CreatedAtUtc, string? ChangeReason, string SalesPlanName);
    private sealed record AccessoryReferenceRow(decimal IdOrder, decimal IdProduct, long IdSalesPlan,
        decimal IdPriceVersion, long? IdVariant, decimal IdAccessory, decimal PriceAmount, decimal BaseAmount,
        decimal DiscountPercent, decimal FinalPriceAmount, bool IsActive, DateTime CreatedAtUtc,
        string? ChangeReason, string AccessoryName);

    public async Task<IReadOnlyList<OrderStatusData>> ReadStatusesAsync(CancellationToken ct)
    {
        var statuses = await db.OrderStatuses.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameFa, x.NameEn }).ToListAsync(ct);
        return statuses.Select(x => new OrderStatusData(x.Id,
            StoreOrderStatusNames.CodeFromNameEn(x.NameEn), x.NameFa, x.NameEn)).ToArray();
    }

    public async Task<ShippingStatusUpdateStatus> UpdateShippingStatusAsync(decimal id, string expectedStatus,
        string status, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.StoreCustomerOrders.Include(x => x.StatusReference)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        string? expectedStatusName = StoreOrderStatusNames.NameEnFromCode(expectedStatus);
        string? nextStatusName = StoreOrderStatusNames.NameEnFromCode(status);
        if (expectedStatusName is null || nextStatusName is null || order is null ||
            order.StatusReference.NameEn != expectedStatusName || order.IdVoucher is null ||
            !await db.StorePaymentAttempts.AnyAsync(p => p.IdOrder == id &&
                p.StatusReference.Code == "verified" && p.Amount == order.TotalAmount, ct))
            return ShippingStatusUpdateStatus.Conflict;
        var next = await db.OrderStatuses.SingleOrDefaultAsync(x => x.NameEn == nextStatusName, ct);
        if (next is null) return ShippingStatusUpdateStatus.MissingStatus;
        order.IdStatus = next.Id;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ShippingStatusUpdateStatus.Updated;
    }

    public async Task<OrderListData> ReadOrdersAsync(string search, string status, DateTime? fromUtc,
        DateTime? untilUtc, int offset, int pageSize, CancellationToken ct)
    {
        decimal? orderIdSearch = decimal.TryParse(search, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out decimal parsedOrderId) ? parsedOrderId : null;
        string? statusName = status == "" ? null : StoreOrderStatusNames.NameEnFromCode(status);
        var query = db.StoreCustomerOrders.AsNoTracking()
            .Join(db.AccountProfiles.AsNoTracking(), order => order.IdProfile, profile => profile.Id,
                (order, profile) => new { order, profile })
            .Where(x => (search == "" || (orderIdSearch.HasValue && x.order.Id == orderIdSearch.Value) ||
                    (x.profile.FirstName + " " + x.profile.LastName).Contains(search)) &&
                (statusName == null || x.order.StatusReference.NameEn == statusName) &&
                (!fromUtc.HasValue || x.order.CreatedAtUtc >= fromUtc.Value) &&
                (!untilUtc.HasValue || x.order.CreatedAtUtc < untilUtc.Value))
            .OrderByDescending(x => x.order.Id)
            .Select(x => new
            {
                x.order.Id,
                x.order.IdStatus,
                StatusNameEn = x.order.StatusReference.NameEn,
                StatusName = x.order.StatusReference.NameFa,
                PaymentMethod = x.order.Bank.Code,
                x.order.IdBank,
                BankName = x.order.Bank == null ? null : x.order.Bank.NameFa,
                ItemCount = db.StoreCustomerOrderItems.Where(item => item.IdOrder == x.order.Id)
                    .Sum(item => (int?)item.Quantity) ?? 0,
                x.order.TotalAmount,
                x.order.CreatedAtUtc,
                CustomerName = (x.profile.FirstName + " " + x.profile.LastName).Trim()
            });
        int total = await query.CountAsync(ct);
        var rawRows = await query.Skip(offset).Take(pageSize).ToListAsync(ct);
        var rows = rawRows.Select(x => new OrderListRowData(x.Id, x.IdStatus,
            x.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StoreOrderStatusNames.CodeFromNameEn(x.StatusNameEn), x.StatusName, x.PaymentMethod, x.IdBank,
            x.BankName, x.ItemCount, x.TotalAmount, x.CreatedAtUtc, x.CustomerName)).ToArray();
        return new OrderListData(rows, total);
    }

    public async Task<OrderDetailsData?> ReadOrderDetailsAsync(decimal id, CancellationToken ct)
    {
        var order = await db.StoreCustomerOrders.AsNoTracking().Where(x => x.Id == id)
            .Join(db.AccountProfiles.AsNoTracking(), x => x.IdProfile, p => p.Id, (x, p) => new OrderDetailsData
            {
                Id = x.Id, IdStatus = x.IdStatus, OrderNumber = "", Status = x.StatusReference.NameEn,
                StatusName = x.StatusReference.NameFa, PaymentMethod = x.Bank.Code, IdBank = x.IdBank,
                BankName = x.Bank == null ? null : x.Bank.NameFa,
                ItemCount = db.StoreCustomerOrderItems.Where(item => item.IdOrder == x.Id)
                    .Sum(item => (int?)item.Quantity) ?? 0,
                TotalAmount = x.TotalAmount, SubtotalAmount = x.SubtotalAmount,
                CouponDiscountAmount = x.CouponDiscountAmount, CreatedAtUtc = x.CreatedAtUtc,
                CustomerName = (p.FirstName + " " + p.LastName).Trim(), IdVoucher = x.IdVoucher,
                IdAddress = x.IdAddress,
                AddressJson = "{}"
            }).SingleOrDefaultAsync(ct);
        if (order is null) return null;
        var address = await db.AccountAddresses.AsNoTracking().Where(x => x.Id == order.IdAddress)
            .Select(x => new
            {
                recipient = x.Recipient,
                phone = x.Phone,
                street = x.Street,
                postal = x.PostalCode,
                province = db.GeoProvinces.Where(row => row.Id == x.IdProvince).Select(row => row.NameFa).FirstOrDefault() ?? "",
                city = db.GeoCities.Where(row => row.Id == x.IdCity).Select(row => row.NameFa).FirstOrDefault() ?? ""
            }).SingleOrDefaultAsync(ct);
        order.AddressJson = JsonSerializer.Serialize(address ?? new { recipient = "", phone = "", street = "", postal = "", province = "", city = "" });
        order.OrderNumber = order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        order.Status = StoreOrderStatusNames.CodeFromNameEn(order.Status);

        var itemRows = await db.StoreCustomerOrderItems.AsNoTracking().Where(x => x.IdOrder == id)
            .Join(db.SalesPlanReferences.AsNoTracking(), x => x.IdSalesPlan, plan => plan.Id,
                (x, plan) => new { x, plan })
            .Join(db.Products.AsNoTracking(), row => row.x.IdProduct, product => product.Id,
                (row, product) => new OrderItemRow(row.x.Id, row.x.IdOrder, row.x.IdProduct, row.x.IdSalesPlan,
                    product.NameFa,
                    db.ProductVariants.Where(variant => variant.Id == row.x.IdVariant)
                        .Select(variant => variant.VariantName.TreePathNames).FirstOrDefault(),
                    row.x.Quantity, row.x.IdVariant, row.x.IdPriceVersion,
                    row.x.PriceVersion.PriceBeforeDiscountAmount, row.x.PriceVersion.DiscountPercent,
                    row.x.PriceVersion.FinalPriceAmount, row.x.PriceVersion.IsActive,
                    row.x.PriceVersion.CreatedAtUtc, row.x.PriceVersion.ChangeReason, row.plan.DisplayName))
            .OrderBy(x => x.Id).ToListAsync(ct);
        var references = await db.StoreOrderAccessories.AsNoTracking()
            .Join(db.StorePriceVersions.AsNoTracking(), x => x.IdPriceVersion, version => version.Id,
                (x, version) => new { x, version })
            .Where(x => x.x.IdOrder == id).OrderBy(x => x.x.IdProduct).ThenBy(x => x.x.IdSalesPlan)
            .ThenBy(x => x.x.IdAccessory)
            .Select(x => new AccessoryReferenceRow(x.x.IdOrder, x.x.IdProduct, x.x.IdSalesPlan,
                x.x.IdPriceVersion, x.version.IdVariant, x.x.IdAccessory, x.version.FinalPriceAmount,
                x.version.PriceBeforeDiscountAmount, x.version.DiscountPercent, x.version.FinalPriceAmount,
                x.version.IsActive, x.version.CreatedAtUtc, x.version.ChangeReason, x.x.Accessory.Name))
            .ToListAsync(ct);
        decimal[] productIds = references.Select(x => x.IdProduct).Distinct().ToArray();
        decimal[] accessoryIds = references.Select(x => x.IdAccessory).Distinct().ToArray();
        var requirements = accessoryIds.Length == 0 ? [] : await db.StoreProductAccessories.AsNoTracking()
            .Where(x => productIds.Contains(x.IdProduct) && accessoryIds.Contains(x.IdAccessory))
            .Select(x => new { x.IdProduct, x.IdAccessory, x.IsRequired }).ToListAsync(ct);
        var requirementMap = requirements.ToDictionary(x => (x.IdProduct, x.IdAccessory), x => x.IsRequired);

        var components = new List<object>();
        foreach (var item in itemRows)
            components.Add(new
            {
                productName = item.ProductName, componentType = item.IdVariant.HasValue ? "تنوع" : "کالا",
                accessoryRequirement = (string?)null, componentId = item.IdVariant ?? item.IdProduct,
                priceVersionId = item.IdPriceVersion,
                treePathNames = item.IdVariant.HasValue ? item.VariantName : null,
                accessoryName = (string?)null, salesPlan = item.SalesPlanName, quantity = item.Quantity,
                baseAmount = item.PriceAmount, discountPercent = item.DiscountPercent,
                discountAmount = item.PriceAmount - item.FinalPriceAmount,
                finalUnitAmount = item.FinalPriceAmount, lineTotal = item.FinalPriceAmount * item.Quantity
            });
        foreach (var group in references.GroupBy(x => new { x.IdProduct, x.IdSalesPlan, x.IdAccessory }))
        {
            var matching = itemRows.Where(x => x.IdProduct == group.Key.IdProduct &&
                x.IdSalesPlan == group.Key.IdSalesPlan).ToArray();
            var reference = group.First();
            bool hasRequirement = requirementMap.TryGetValue((group.Key.IdProduct, group.Key.IdAccessory),
                out bool required);
            components.Add(new
            {
                productName = matching.FirstOrDefault()?.ProductName, componentType = "متعلقه",
                accessoryRequirement = hasRequirement ? required ? "الزامی (تعریف فعلی)" : "اختیاری (تعریف فعلی)" : null,
                componentId = group.Key.IdAccessory, priceVersionId = reference.IdPriceVersion,
                treePathNames = (string?)null, accessoryName = reference.AccessoryName,
                salesPlan = matching.FirstOrDefault()?.SalesPlanName, quantity = matching.Sum(x => x.Quantity),
                baseAmount = reference.BaseAmount, discountPercent = reference.DiscountPercent,
                discountAmount = reference.BaseAmount - reference.PriceAmount,
                finalUnitAmount = reference.PriceAmount, lineTotal = reference.PriceAmount * matching.Sum(x => x.Quantity)
            });
        }
        var items = itemRows.Select(item => new
        {
            name = item.ProductName, variantName = item.VariantName, quantity = item.Quantity,
            unitAmount = item.FinalPriceAmount + references.Where(x => x.IdOrder == item.IdOrder &&
                x.IdProduct == item.IdProduct && x.IdSalesPlan == item.IdSalesPlan).Sum(x => x.PriceAmount),
            salesPlan = item.SalesPlanName,
            options = new { accessories = references.Where(x => x.IdOrder == item.IdOrder &&
                x.IdProduct == item.IdProduct && x.IdSalesPlan == item.IdSalesPlan)
                .Select(x => new { id = x.IdAccessory.ToString(), selected = true }).ToArray() },
            priceReferences = references.Where(x => x.IdOrder == item.IdOrder && x.IdProduct == item.IdProduct &&
                x.IdSalesPlan == item.IdSalesPlan).Select(x => new
                {
                    priceVersionId = x.IdPriceVersion,
                    component = StorePriceVersionReference.GetComponent(x.IdVariant, x.IdAccessory),
                    name = x.AccessoryName, calculatedUnitAmount = x.PriceAmount, recordedPriceAmount = x.PriceAmount,
                    discountPercent = x.DiscountPercent, isCurrent = x.IsActive, recordedAtUtc = x.CreatedAtUtc,
                    reason = x.ChangeReason
                }).Prepend(new
                {
                    priceVersionId = item.IdPriceVersion,
                    component = item.IdVariant.HasValue ? "variant" : "product",
                    name = item.VariantName ?? item.ProductName, calculatedUnitAmount = item.FinalPriceAmount,
                    recordedPriceAmount = item.PriceAmount, discountPercent = item.DiscountPercent,
                    isCurrent = item.IsActive, recordedAtUtc = item.CreatedAtUtc, reason = item.ChangeReason
                }).ToArray()
        }).ToList();
        order.CalculatedItemSubtotal = items.Sum(x => x.unitAmount * x.quantity);
        order.HasItemSubtotalMismatch = items.Count > 0 && order.CalculatedItemSubtotal != order.SubtotalAmount;
        order.ItemsJson = System.Text.Json.JsonSerializer.Serialize(items);
        order.ComponentsJson = System.Text.Json.JsonSerializer.Serialize(components);
        return order;
    }
}
