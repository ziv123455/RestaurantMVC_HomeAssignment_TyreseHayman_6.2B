using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Domain.Interfaces;
using Domain.Models;

namespace DataAccess.Factory
{
    public class ImportItemFactory
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public List<IItemValidating> Create(string json)
        {
            var rawItems = JsonSerializer.Deserialize<List<RawImportItem>>(json, _options)
                           ?? new List<RawImportItem>();

            var result = new List<IItemValidating>();
            var restaurantByExternalId = new Dictionary<string, Restaurant>();

            // 1st pass – restaurants
            foreach (var raw in rawItems.Where(r =>
                         string.Equals(r.Type, "restaurant", StringComparison.OrdinalIgnoreCase)))
            {
                var restaurant = new Restaurant
                {
                    Name = raw.Name ?? string.Empty,
                    OwnerEmailAddress = raw.OwnerEmailAddress ?? string.Empty,
                    Status = "Pending",            // default status
                    ExternalId = raw.Id            // keep JSON id, e.g. "R-1001"
                };

                result.Add(restaurant);

                if (!string.IsNullOrWhiteSpace(raw.Id))
                {
                    restaurantByExternalId[raw.Id.Trim()] = restaurant;
                }
            }

            // 2nd pass – menu items
            foreach (var raw in rawItems.Where(r =>
                         string.Equals(r.Type, "menuItem", StringComparison.OrdinalIgnoreCase)))
            {
                var menuItem = new MenuItem
                {
                    Title = raw.Title ?? string.Empty,
                    Price = raw.Price ?? 0m,
                    Status = "Pending",
                    ExternalId = raw.Id           // keep JSON id, e.g. "M-2001"
                };

                // link to restaurant in-memory (for preview)
                if (!string.IsNullOrWhiteSpace(raw.RestaurantId) &&
                    restaurantByExternalId.TryGetValue(raw.RestaurantId.Trim(), out var parent))
                {
                    menuItem.Restaurant = parent;
                }

                result.Add(menuItem);
            }

            return result;
        }

        // DTO used only inside the factory
        private class RawImportItem
        {
            public string? Type { get; set; }
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? OwnerEmailAddress { get; set; }
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public string? Title { get; set; }
            public decimal? Price { get; set; }
            public string? Currency { get; set; }
            public string? RestaurantId { get; set; }
        }
    }
}
