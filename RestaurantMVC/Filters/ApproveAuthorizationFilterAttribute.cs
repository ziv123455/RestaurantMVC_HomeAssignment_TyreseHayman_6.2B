using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RestaurantMVC.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ApproveAuthorizationFilterAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // Must be logged in
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new ForbidResult();
                return;
            }

            var userEmail =
                user.FindFirstValue(ClaimTypes.Email) ??
                user.Identity!.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                context.Result = new ForbidResult();
                return;
            }

            userEmail = userEmail.ToLowerInvariant();

            // ----- Read action parameters safely with TryGetValue -----

            context.ActionArguments.TryGetValue("itemType", out var itemTypeObj);
            var itemType = itemTypeObj as string;

            context.ActionArguments.TryGetValue("restaurantIds", out var restIdsObj);
            var restaurantIds = restIdsObj as List<int>;

            context.ActionArguments.TryGetValue("menuItemIds", out var menuIdsObj);
            var menuItemIds = menuIdsObj as List<Guid>;

            if (string.IsNullOrWhiteSpace(itemType))
            {
                context.Result = new ForbidResult();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<RestaurantDbContext>();

            bool authorized = true;

            if (itemType == "restaurant" && restaurantIds != null && restaurantIds.Count > 0)
            {
                var restaurants = await db.Restaurants
                    .Where(r => restaurantIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var r in restaurants)
                {
                    var allowedEmails = r.GetValidators()
                        .Select(v => v.ToLowerInvariant());

                    if (!allowedEmails.Contains(userEmail))
                    {
                        authorized = false;
                        break;
                    }
                }
            }
            else if (itemType == "menuItem" && menuItemIds != null && menuItemIds.Count > 0)
            {
                var menuItems = await db.MenuItems
                    .Include(m => m.Restaurant)
                    .Where(m => menuItemIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var m in menuItems)
                {
                    var allowedEmails = m.GetValidators()
                        .Select(v => v.ToLowerInvariant());

                    if (!allowedEmails.Contains(userEmail))
                    {
                        authorized = false;
                        break;
                    }
                }
            }

            if (!authorized)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
