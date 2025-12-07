using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Interfaces;

namespace Domain.Models
{
    public class Restaurant : IItemValidating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }          // int identity PK

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string OwnerEmailAddress { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!;   // Pending / Approved / etc.

        // New: path to image in wwwroot (e.g. "/images/restaurants/xyz.jpg")
        public string? ImagePath { get; set; }

        // New: external id from JSON (R-1001, etc.) – not stored in DB
        [NotMapped]
        public string? ExternalId { get; set; }

        // Navigation property (1-to-many with MenuItem)
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

        // --- IItemValidating implementation ---

        // Restaurants – site admin can approve
        public List<string> GetValidators()
        {
            // You can later read this email from appsettings if you want
            return new List<string>
            {
                "siteadmin@example.com"
            };
        }

        // Name of the partial view that renders a Restaurant card
        public string GetCardPartial() => "_RestaurantCard";
    }
}
