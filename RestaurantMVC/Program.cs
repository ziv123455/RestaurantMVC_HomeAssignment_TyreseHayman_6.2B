using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Data;
using DataAccess;
using Domain.Interfaces;
using DataAccess.Repositories;
using DataAccess.Factory;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// DbContexts
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<RestaurantDbContext>(options =>
    options.UseSqlServer(connectionString));

// Memory cache (needed for ItemsInMemoryRepository)
builder.Services.AddMemoryCache();

// Keyed repositories
builder.Services.AddKeyedScoped<IItemsRepository>("memory", (sp, key) =>
    new ItemsInMemoryRepository(sp.GetRequiredService<IMemoryCache>()));

builder.Services.AddKeyedScoped<IItemsRepository>("db", (sp, key) =>
    new ItemsDbRepository(sp.GetRequiredService<RestaurantDbContext>()));

// Also register ItemsDbRepository directly so we can inject it into controllers/filters
builder.Services.AddScoped<ItemsDbRepository>();

// Factory for bulk import
builder.Services.AddScoped<ImportItemFactory>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Disable email confirmation requirement so login works immediately
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
