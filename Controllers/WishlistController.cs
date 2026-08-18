using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AGY.Data;
using AGY.Models;

namespace AGY.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public WishlistController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // --- VIEW WISHLIST ---
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);
        var wishlist = await _context.WishlistItems
            .Include(w => w.Product)
            .ThenInclude(p => p!.Images)
            .Where(w => w.UserId == currentUserId)
            .ToListAsync();

        return View(wishlist);
    }

    // --- TOGGLE WISHLIST (AJAX / POST) ---
    [HttpPost]
    [AllowAnonymous] // Allow anonymous hits so we can return redirect responses for AJAX
    public async Task<IActionResult> Toggle(int productId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            // Redirect to login if user is not authenticated
            var returnUrl = $"/Shop/Details/{productId}";
            return Json(new { success = false, message = "Please sign in to save wishlist items.", redirect = Url.Action("Login", "Account", new { returnUrl }) });
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Json(new { success = false, message = "User not found." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Product not found." });
        }

        var wishlistItem = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == currentUserId);

        bool isAdded = false;
        string message;

        if (wishlistItem != null)
        {
            // Remove
            _context.WishlistItems.Remove(wishlistItem);
            message = $"{product.Name} removed from wishlist.";
        }
        else
        {
            // Add
            wishlistItem = new WishlistItem
            {
                ProductId = productId,
                UserId = currentUserId
            };
            _context.WishlistItems.Add(wishlistItem);
            isAdded = true;
            message = $"{product.Name} saved to wishlist!";
        }

        await _context.SaveChangesAsync();

        // Get total wishlist count for the user
        var wishlistCount = await _context.WishlistItems.CountAsync(w => w.UserId == currentUserId);

        return Json(new { success = true, isAdded, message, wishlistCount });
    }

    // --- REMOVE DIRECTLY (REDIRECT) ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        var currentUserId = _userManager.GetUserId(User);
        var wishlistItem = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == currentUserId);

        if (wishlistItem != null)
        {
            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product removed from wishlist.";
        }

        return RedirectToAction("Index");
    }
}
