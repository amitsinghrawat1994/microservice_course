using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("Items")]
    [Authorize]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> itemRepository;
        // private static int requestCounter = 0;
        private readonly IPublishEndpoint publishEndpoint;
        public ItemsController(IRepository<Item> itemRepository, IPublishEndpoint publishEndpoint)
        {
            this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
            this.publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            // requestCounter++;
            // Console.WriteLine($"Request {requestCounter}: starting...");
            // if (requestCounter <= 2)
            // {
            //     Console.WriteLine($"Request {requestCounter}: Delaying...");
            //     await Task.Delay(TimeSpan.FromSeconds(10));
            // }

            // if (requestCounter <= 4)
            // {
            //     Console.WriteLine($"Request {requestCounter}: 500 (internal server error)...");
            //     return StatusCode(500);
            // }

            var items = (await itemRepository.GetAllAsync())
                        .Select(item => item.AsDto());

            // Console.WriteLine($"Request {requestCounter}: 200 (OK)...");

            return Ok(items);
        }

        // Get /Items/
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itemRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await itemRepository.CreateAsync(item);

            await publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await itemRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await itemRepository.UpdateAsync(existingItem);

            await publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var existingItem = await itemRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }

            await itemRepository.RemoveAsync(existingItem.Id);

            await publishEndpoint.Publish(new CatalogItemDeleted(id));

            return NoContent();
        }
    }
}