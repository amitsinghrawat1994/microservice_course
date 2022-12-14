using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Dtos;
using Play.Inventory.Service.Entities;
using System.Linq;
using Play.Inventory.Service.Clients;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";
        private readonly IRepository<InventoryItem> inventoryItemRepository;
        // private readonly CatalogClient catalogClient;
        private readonly IRepository<CatalogItem> catalogItemRepository;

        public ItemsController(IRepository<InventoryItem> inventoryItemRepository, IRepository<CatalogItem> catalogItemRepository)
        {
            this.inventoryItemRepository = inventoryItemRepository;
            this.catalogItemRepository = catalogItemRepository;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (Guid.Parse(currentUserId) != userId)
            {
                if (!User.IsInRole(AdminRole))
                {
                    return Forbid();
                }
            }

            // var catalogItems = await catalogClient.GetCatalogItemAsync();

            // not getting the item by id
            var inventoryItemEntires = await inventoryItemRepository.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemEntires.Select(item => item.CatalogItemId);

            var catalogItemEntities = await catalogItemRepository.GetAllAsync(item => itemIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemEntires.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);

                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> PostAsync(GrandItemsDto grandItemsDto)
        {
            var inventoryItem = await inventoryItemRepository.GetAsync(item => item.UserId == grandItemsDto.UserId
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
                await inventoryItemRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity = grandItemsDto.Quantity;
                await inventoryItemRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }
    }
}