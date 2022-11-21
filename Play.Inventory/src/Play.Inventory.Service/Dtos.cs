using System;

namespace Play.Inventory.Dtos
{
    public record GrandItemsDto(Guid UserId, Guid CatalogItemId, int Quantity);

    //string Name, string Description,
    public record InventoryItemDto(Guid CatalogItemId, string Name, string Description, int Quantity, DateTimeOffset AcquireDate);

    public record CatalogItemDto(Guid Id, string Name, String Description);
}