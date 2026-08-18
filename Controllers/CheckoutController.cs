using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AGY.Data;
using AGY.Helpers;
using AGY.Models;
using System.ComponentModel.DataAnnotations;

namespace AGY.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public CheckoutController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // --- CHECKOUT PAGE ---
    [HttpGet]
    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
            return RedirectToAction("Index", "Cart");
        }

        ViewBag.Cart = cart;
        ViewBag.CartTotal = cart.Sum(item => item.TotalPrice);

        // Prepopulate email if logged in
        var model = new CheckoutViewModel
        {
            Email = User.Identity?.Name ?? ""
        };

        return View(model);
    }

    // --- SECURE PAYMENT & ORDER PLACEMENT ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
        
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Cart = cart;
            ViewBag.CartTotal = cart.Sum(item => item.TotalPrice);
            return View("Index", model);
        }

        // Verify stock for all items
        foreach (var item in cart)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                TempData["ErrorMessage"] = $"Product '{item.ProductName}' was not found in database.";
                return RedirectToAction("Index", "Cart");
            }
            if (product.StockQuantity < item.Quantity)
            {
                TempData["ErrorMessage"] = $"Sorry, '{product.Name}' is low on stock. Only {product.StockQuantity} left.";
                return RedirectToAction("Index", "Cart");
            }
        }

        var currentUserId = _userManager.GetUserId(User) ?? "";

        // Create new Order
        var order = new Order
        {
            UserId = currentUserId,
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            ShippingAddress = model.ShippingAddress,
            TotalAmount = cart.Sum(item => item.TotalPrice),
            Status = "Processing", // Initial status
            OrderDate = DateTime.UtcNow
        };

        // Add items to order and decrease stock
        foreach (var item in cart)
        {
            var product = await _context.Products.FirstAsync(p => p.Id == item.ProductId);
            
            // Decrease stock
            product.StockQuantity -= item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            });
        }

        // Save order and stock changes to DB
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Clear shopping cart session
        HttpContext.Session.Remove("Cart");

        TempData["SuccessMessage"] = "Order placed successfully! Payment approved.";
        
        // Redirect to Invoice
        return RedirectToAction("OrderDetails", "Account", new { id = order.Id });
    }
}

// Checkout Form Binding ViewModel
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Shipping Address is required.")]
    [StringLength(500)]
    [Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    // Credit Card Simulation inputs
    [Required(ErrorMessage = "Credit Card number is required.")]
    [CreditCard(ErrorMessage = "Please enter a valid credit card number.")]
    [Display(Name = "Card Number")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiration date is required.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{4}|[0-9]{2})$", ErrorMessage = "Expiration must be in MM/YY format.")]
    [Display(Name = "Expiration Date")]
    public string ExpirationDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "CVV security code is required.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
    [Display(Name = "CVV Security Code")]
    public string CVV { get; set; } = string.Empty;
}
