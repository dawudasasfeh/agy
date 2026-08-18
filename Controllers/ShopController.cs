using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AGY.Data;
using AGY.Models;

namespace AGY.Controllers;

public class ShopController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ShopController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // --- PRODUCT CATALOG ---
    public async Task<IActionResult> Index(int? categoryId, string? searchQuery, string? sortBy, decimal? minPrice, decimal? maxPrice)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .AsQueryable();

        // 1. Partial Search by product name OR category name
        if (!string.IsNullOrEmpty(searchQuery))
        {
            var search = searchQuery.ToLower().Trim();
            query = query.Where(p => 
                p.Name.ToLower().Contains(search) || 
                (p.Category != null && p.Category.Name.ToLower().Contains(search))
            );
        }

        // 2. Category Filter
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // 3. Price Range Filter
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // 4. Sorting
        switch (sortBy)
        {
            case "price_asc":
                query = query.OrderBy(p => p.Price);
                break;
            case "price_desc":
                query = query.OrderByDescending(p => p.Price);
                break;
            case "name_asc":
                query = query.OrderBy(p => p.Name);
                break;
            default:
                query = query.OrderByDescending(p => p.Id); // default newest
                break;
        }

        var products = await query.ToListAsync();

        // Provide metadata for filters in the view
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.CurrentCategory = categoryId;
        ViewBag.SearchQuery = searchQuery;
        ViewBag.SortBy = sortBy;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

        return View(products);
    }

    // --- PRODUCT DETAILS ---
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction("Index");
        }

        // Only display APPROVED reviews in product details
        var approvedReviews = product.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.SubmittedAt).ToList();
        ViewBag.ApprovedReviews = approvedReviews;

        // Compute average rating
        ViewBag.AverageRating = approvedReviews.Any() ? approvedReviews.Average(r => r.Rating) : 5.0;

        // Get related products (same category, excluding self)
        var relatedProducts = await _context.Products
            .Include(p => p.Images)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
            .Take(4)
            .ToListAsync();
        ViewBag.RelatedProducts = relatedProducts;

        return View(product);
    }

    // --- SUBMIT COMMENT & RATING ---
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(int productId, string content, int rating)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Review content cannot be empty.";
            return RedirectToAction("Details", new { id = productId });
        }

        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Please select a valid star rating (1 to 5).";
            return RedirectToAction("Details", new { id = productId });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction("Index");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["ErrorMessage"] = "You must be logged in to submit a review.";
            return RedirectToAction("Login", "Account");
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = user.Id,
            UserFullName = user.Email ?? "Valued Explorer",
            Content = content,
            Rating = rating,
            IsApproved = false, // Requires Admin approval!
            SubmittedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your review comment has been submitted and is pending admin approval.";
        return RedirectToAction("Details", new { id = productId });
    }
}
