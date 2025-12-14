using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Factory;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace RestaurantMVC.Controllers
{
    [Authorize]  // only logged-in users can use bulk import
    public class BulkImportController : Controller
    {
        private readonly ImportItemFactory _factory;
        private readonly IWebHostEnvironment _env;

        public BulkImportController(ImportItemFactory factory, IWebHostEnvironment env)
        {
            _factory = factory;
            _env = env;
        }

        // GET: /BulkImport/BulkImport
        [HttpGet]
        public async Task<IActionResult> BulkImport(
            [FromKeyedServices("memory")] IItemsRepository itemsRepository)
        {
            var items = await itemsRepository.GetAsync();
            return View(items);
        }

        // POST: /BulkImport/BulkImport  (upload JSON + preview)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImport(
            IFormFile jsonFile,
            [FromKeyedServices("memory")] IItemsRepository itemsRepository)
        {
            if (jsonFile == null || jsonFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please upload a JSON file.");
                var existing = await itemsRepository.GetAsync();
                return View(existing);
            }

            string json;
            using (var stream = jsonFile.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                json = await reader.ReadToEndAsync();
            }

            // FACTORY: build Restaurant / MenuItem instances (all Pending, with ExternalId)
            var items = _factory.Create(json);

            // store temporarily in memory
            await itemsRepository.SaveAsync(items);

            return View(items);
        }

        // GET: /BulkImport/DownloadZip  (create folders + default image)
        [HttpGet]
        public async Task<IActionResult> DownloadZip(
            [FromKeyedServices("memory")] IItemsRepository itemsRepository)
        {
            var items = await itemsRepository.GetAsync();
            if (items == null || items.Count == 0)
            {
                return BadRequest("No items in memory. Upload JSON first.");
            }

            // create a temp root folder
            var tempRoot = Path.Combine(Path.GetTempPath(), "BulkImportZip", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempRoot);

            // default image source (wwwroot/images/default.jpg)
            var defaultImagePath = Path.Combine(_env.WebRootPath, "images", "default.jpg");
            if (!System.IO.File.Exists(defaultImagePath))
            {
                Directory.Delete(tempRoot, recursive: true);
                return Problem("Default image not found at /wwwroot/images/default.jpg");
            }

            // one folder per item (restaurant or menu item)
            foreach (var item in items)
            {
                string externalId = item switch
                {
                    Restaurant r => r.ExternalId ?? Guid.NewGuid().ToString(),
                    MenuItem m => m.ExternalId ?? Guid.NewGuid().ToString(),
                    _ => Guid.NewGuid().ToString()
                };

                var folderName = $"item-{externalId}";
                var itemFolder = Path.Combine(tempRoot, folderName);
                Directory.CreateDirectory(itemFolder);

                var destImagePath = Path.Combine(itemFolder, "default.jpg");
                System.IO.File.Copy(defaultImagePath, destImagePath, overwrite: true);
            }

            var zipPath = Path.Combine(Path.GetTempPath(), $"items-{Guid.NewGuid()}.zip");
            ZipFile.CreateFromDirectory(tempRoot, zipPath);

            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);

            Directory.Delete(tempRoot, recursive: true);
            System.IO.File.Delete(zipPath);

            return File(zipBytes, "application/zip", "items-images.zip");
        }

        // POST: /BulkImport/Commit  (upload images ZIP + save to DB)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Commit(
            IFormFile imagesZip,
            [FromKeyedServices("memory")] IItemsRepository memoryRepository,
            [FromKeyedServices("db")] IItemsRepository dbRepository)
        {
            var items = await memoryRepository.GetAsync();
            if (items == null || items.Count == 0)
            {
                return BadRequest("No items in memory to commit. Upload JSON first.");
            }

            if (imagesZip == null || imagesZip.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please upload the images ZIP.");
                // re-show BulkImport view with existing items
                return View("BulkImport", items);
            }

            // 1. Save uploaded ZIP to temp file
            var tempRoot = Path.Combine(Path.GetTempPath(), "BulkImportImages", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempRoot);

            var tempZipPath = Path.Combine(tempRoot, "upload.zip");
            using (var fs = new FileStream(tempZipPath, FileMode.Create))
            {
                await imagesZip.CopyToAsync(fs);
            }

            // 2. Extract ZIP
            var extractFolder = Path.Combine(tempRoot, "extracted");
            Directory.CreateDirectory(extractFolder);
            ZipFile.ExtractToDirectory(tempZipPath, extractFolder);

            // 3. Copy images to wwwroot and set ImagePath
            var imagesRoot = Path.Combine(_env.WebRootPath, "images", "items");
            Directory.CreateDirectory(imagesRoot);

            foreach (var item in items)
            {
                string externalId = item switch
                {
                    Restaurant r => r.ExternalId ?? string.Empty,
                    MenuItem m => m.ExternalId ?? string.Empty,
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(externalId))
                    continue;

                var itemFolder = Path.Combine(extractFolder, $"item-{externalId}");
                if (!Directory.Exists(itemFolder))
                    continue;

                var imageFiles = Directory.GetFiles(itemFolder);
                if (imageFiles.Length == 0)
                    continue;

                var sourceImagePath = imageFiles[0];
                var extension = Path.GetExtension(sourceImagePath);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var destImagePath = Path.Combine(imagesRoot, fileName);

                System.IO.File.Copy(sourceImagePath, destImagePath, overwrite: true);

                var relativePath = $"/images/items/{fileName}";

                switch (item)
                {
                    case Restaurant r:
                        r.ImagePath = relativePath;
                        break;
                    case MenuItem m:
                        m.ImagePath = relativePath;
                        break;
                }
            }

            // 4. Save to DB
            await dbRepository.SaveAsync(items);

            // 5. Clear in-memory items
            await memoryRepository.ClearAsync();

            // 6. Clean temp folder
            Directory.Delete(tempRoot, recursive: true);

            // 7. Go to catalog
            return RedirectToAction("Index", "Catalog", new { type = "restaurants", mode = "view", view = "card" });
        }
    }
}
