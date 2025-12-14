using System.Collections.Generic;
using Domain.Models;

namespace RestaurantMVC.Models
{
    public class VerificationViewModel
    {
        public List<Restaurant> PendingRestaurants { get; set; } = new();
        public List<MenuItem> PendingMenuItems { get; set; } = new();
    }
}
