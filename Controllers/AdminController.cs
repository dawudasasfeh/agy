using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AGY.Data;
using AGY.Models;
using System.ComponentModel.DataAnnotations;

namespace AGY.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // --- DASHBOARD OVERVIEW & STATS ---
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalProducts = await _context.Products.CountAsync();
        ViewBag.TotalCategories = await _context.Categories.CountAsync();
        ViewBag.TotalOrders = await _context.Orders.CountAsync();

        // Get 5 recent orders for display
        var recentOrders = await _context.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToListAsync();

        return View(recentOrders);
    }

    // --- MANAGE CATEGORIES ---
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(Category model, IFormFile? imageFile)
    {
        if (ModelState.IsValid)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                // Strict image validation
                if (!imageFile.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("", "Only image files are allowed.");
                    return View(model);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                model.ImageUrl = "/images/categories/" + fileName;
            }

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Categories));
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, Category model, IFormFile? imageFile)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var existingCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                if (existingCategory == null) return NotFound();

                model.ImageUrl = existingCategory.ImageUrl;

                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!imageFile.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("", "Only image files are allowed.");
                        return View(model);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    model.ImageUrl = "/images/categories/" + fileName;
                }

                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(model.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Categories));
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category deleted successfully.";
        }
        return RedirectToAction(nameof(Categories));
    }

    // --- MANAGE PRODUCTS ---
    [HttpGet]
    public async Task<IActionResult> Products()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToListAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product model, List<IFormFile> imageFiles)
    {
        if (ModelState.IsValid)
        {
            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            // Handle multiple image uploads
            if (imageFiles != null && imageFiles.Any())
            {
                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        if (!file.ContentType.StartsWith("image/"))
                        {
                            TempData["ErrorMessage"] = "Failed to upload some files: only images are allowed.";
                            continue;
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = model.Id,
                            ImageUrl = "/images/products/" + fileName
                        };
                        _context.ProductImages.Add(productImage);
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Product created successfully.";
            return RedirectToAction(nameof(Products));
        }

        ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, Product model, List<IFormFile> imageFiles, List<int>? deleteImageIds)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                // Delete selected images
                if (deleteImageIds != null && deleteImageIds.Any())
                {
                    foreach (var imgId in deleteImageIds)
                    {
                        var img = await _context.ProductImages.FindAsync(imgId);
                        if (img != null)
                        {
                            _context.ProductImages.Remove(img);
                        }
                    }
                }

                // Add new images
                if (imageFiles != null && imageFiles.Any())
                {
                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            if (!file.ContentType.StartsWith("image/"))
                            {
                                TempData["ErrorMessage"] = "Failed to upload some files: only images are allowed.";
                                continue;
                            }

                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

                            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = model.Id,
                                ImageUrl = "/images/products/" + fileName
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                }

                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(model.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Products));
        }

        ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product deleted successfully.";
        }
        return RedirectToAction(nameof(Products));
    }

    // --- MANAGE ORDERS ---
    [HttpGet]
    public async Task<IActionResult> Orders(string? statusFilter, string? userFilter)
    {
        var query = _context.Orders.AsQueryable();

        // Filter by Status
        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        // Filter by user email
        if (!string.IsNullOrEmpty(userFilter))
        {
            query = query.Where(o => o.Email.Contains(userFilter));
        }

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.UserFilter = userFilter;

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Order #{orderId} status updated to {status}.";
        }
        return RedirectToAction(nameof(Orders));
    }

    // --- MANAGE REVIEWS ---
    [HttpGet]
    public async Task<IActionResult> Reviews()
    {
        var reviews = await _context.Reviews
            .Include(r => r.Product)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();
        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            review.IsApproved = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Review approved.";
        }
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Review comment rejected & removed.";
        }
        return RedirectToAction(nameof(Reviews));
    }

    // --- MANAGE TESTIMONIALS ---
    [HttpGet]
    public async Task<IActionResult> Testimonials()
    {
        var testimonials = await _context.Testimonials
            .OrderByDescending(t => t.SubmittedAt)
            .ToListAsync();
        return View(testimonials);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveTestimonial(int id)
    {
        var testimonial = await _context.Testimonials.FindAsync(id);
        if (testimonial != null)
        {
            testimonial.IsApproved = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Testimonial approved.";
        }
        return RedirectToAction(nameof(Testimonials));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectTestimonial(int id)
    {
        var testimonial = await _context.Testimonials.FindAsync(id);
        if (testimonial != null)
        {
            _context.Testimonials.Remove(testimonial);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Testimonial rejected & removed.";
        }
        return RedirectToAction(nameof(Testimonials));
    }

    private bool CategoryExists(int id) => _context.Categories.Any(e => e.Id == id);
    private bool ProductExists(int id) => _context.Products.Any(e => e.Id == id);
}
