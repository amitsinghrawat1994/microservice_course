using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Dtos;
using Play.Inventory.Service.Entities;
using System.Linq;
using Play.Inventory.Service.Clients;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> itemRepository;
        private readonly CatalogClient catalogClient;

        public ItemsController(IRepository<InventoryItem> itemRepository, CatalogClient catalogClient)
        {
            this.itemRepository = itemRepository;
            this.catalogClient = catalogClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var catalogItems = await catalogClient.GetCatalogItemAsync();
            var inventoryItemEntires = await itemRepository.GetAllAsync(item => item.UserId == userId);

            var inventoryItemDtos = inventoryItemEntires.Select(inventoryItem =>
            {
                var catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);

                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(GrandItemsDto grandItemsDto)
        {
            var inventoryItem = await itemRepository.GetAsync(item => item.UserId == grandItemsDto.UserId
            && item.CatalogItemId == grandItemsDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem()
                {
                    CatalogItemId = grandItemsDto.CatalogItemId,
                    UserId = grandItemsDto.UserId,
                    Quantity = grandItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };
                await itemRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity = grandItemsDto.Quantity;
                await itemRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }
    }
}