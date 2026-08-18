using Microsoft.AspNetCore.Mvc;
using AGY.Data;
using AGY.Helpers;
using AGY.Models;
using Microsoft.EntityFrameworkCore;

namespace AGY.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    // --- VIEW CART ---
    [HttpGet]
    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
        ViewBag.CartTotal = cart.Sum(item => item.TotalPrice);
        return View(cart);
    }

    // --- ADD TO CART (AJAX / POST) ---
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Invalid quantity specified." });
        }

        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return Json(new { success = false, message = "Product not found." });
        }

        if (product.StockQuantity < quantity)
        {
            return Json(new { success = false, message = $"Only {product.StockQuantity} items in stock." });
        }

        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

        // Check if item already exists
        var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);
        if (cartItem != null)
        {
            // Update quantity
            var newQty = cartItem.Quantity + quantity;
            if (newQty > product.StockQuantity)
            {
                return Json(new { success = false, message = $"Cannot add more. Max stock is {product.StockQuantity} items." });
            }
            cartItem.Quantity = newQty;
        }
        else
        {
            // Add new item
            cart.Add(new CartItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.Images.FirstOrDefault()?.ImageUrl ?? "",
                Price = product.Price,
                Quantity = quantity
            });
        }

        // Save cart
        HttpContext.Session.SetObjectAsJson("Cart", cart);
        var totalCount = cart.Sum(item => item.Quantity);

        return Json(new { success = true, message = $"{product.Name} added to cart!", cartCount = totalCount });
    }

    // --- UPDATE QUANTITY ---
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Quantity must be at least 1." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Product not found." });
        }

        if (product.StockQuantity < quantity)
        {
            return Json(new { success = false, message = $"Only {product.StockQuantity} items in stock." });
        }

        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
        var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);

        if (cartItem != null)
        {
            cartItem.Quantity = quantity;
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        var cartTotal = cart.Sum(item => item.TotalPrice);
        var totalCount = cart.Sum(item => item.Quantity);

        return Json(new { 
            success = true, 
            message = "Cart updated successfully.", 
            itemTotal = cartItem?.TotalPrice.ToString("N2") ?? "0.00", 
            cartTotal = cartTotal.ToString("N2"),
            cartCount = totalCount 
        });
    }

    // --- REMOVE FROM CART ---
    [HttpPost]
    public IActionResult RemoveFromCart(int productId)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
        var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);

        if (cartItem != null)
        {
            cart.Remove(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        TempData["SuccessMessage"] = "Item removed from cart.";
        return RedirectToAction("Index");
    }
}
