using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Interfaces;

namespace Domain.Models
{
    public class MenuItem : IItemValidating
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(8,2)")]
        public decimal Price { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public Restaurant? Restaurant { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = null!;

        // New: path to the image in wwwroot (e.g. "/images/menu/xyz.jpg")
        public string? ImagePath { get; set; }

        // New: external id from JSON (M-2001, etc.) – not stored in DB
        [NotMapped]
        public string? ExternalId { get; set; }

        // --- IItemValidating implementation ---

        // MenuItem – restaurant owner can approve
        public List<string> GetValidators()
        {
            var validators = new List<string>();

            if (Restaurant != null && !string.IsNullOrWhiteSpace(Restaurant.OwnerEmailAddress))
            {
                validators.Add(Restaurant.OwnerEmailAddress);
            }

            return validators;
        }

        // Name of the partial view that renders a MenuItem card
        public string GetCardPartial() => "_MenuItemCard";
    }
}
