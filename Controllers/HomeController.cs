using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AGY.Data;
using AGY.Models;

namespace AGY.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // Get categories
        var categories = await _context.Categories.Take(5).ToListAsync();

        // Get 4 featured products (e.g. products with highest price or just first 4)
        var featuredProducts = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Take(4)
            .ToListAsync();

        // Get 3 approved testimonials
        var testimonials = await _context.Testimonials
            .Where(t => t.IsApproved)
            .OrderByDescending(t => t.SubmittedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Testimonials = testimonials;

        return View(featuredProducts);
    }

    public async Task<IActionResult> About()
    {
        // Get all approved testimonials for display
        var testimonials = await _context.Testimonials
            .Where(t => t.IsApproved)
            .OrderByDescending(t => t.SubmittedAt)
            .ToListAsync();

        return View(testimonials);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTestimonial(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Testimonial content cannot be empty.";
            return RedirectToAction("About");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["ErrorMessage"] = "You must be logged in to submit a testimonial.";
            return RedirectToAction("Login", "Account");
        }

        var testimonial = new Testimonial
        {
            UserId = user.Id,
            UserFullName = user.Email ?? "Valued Explorer",
            Content = content,
            IsApproved = false, // Needs admin approval!
            SubmittedAt = DateTime.UtcNow
        };

        _context.Testimonials.Add(testimonial);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your testimonial has been submitted and is pending admin approval.";
        return RedirectToAction("About");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
