using Microsoft.AspNetCore.Identity;
using AGY.Models;

namespace AGY.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // 1. Seed Roles
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Admin and User accounts
        IdentityUser? adminUser = await userManager.FindByEmailAsync("admin@camping.com");
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = "admin@camping.com",
                Email = "admin@camping.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        IdentityUser? normalUser = await userManager.FindByEmailAsync("user@camping.com");
        if (normalUser == null)
        {
            normalUser = new IdentityUser
            {
                UserName = "user@camping.com",
                Email = "user@camping.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(normalUser, "User");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(normalUser, "User");
            }
        }

        // 3. Seed Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Camping Tents (خيام التخييم)", Description = "High-quality portable tents for all weather conditions and group sizes.", ImageUrl = "/images/categories/tents.jpg" },
                new() { Name = "Sleeping Bags (أكياس نوم)", Description = "Comfortable, insulated sleeping bags designed for sub-zero and warm nights.", ImageUrl = "/images/categories/sleeping_bags.jpg" },
                new() { Name = "Backpacks (حقائب الظهر)", Description = "Heavy-duty, ergonomic hiking packs and daypacks for wilderness or travel.", ImageUrl = "/images/categories/backpacks.jpg" },
                new() { Name = "Portable Cooking Gear (أدوات طهي متنقلة)", Description = "Stoves, cookware, and multi-tools built for outdoor cooking efficiency.", ImageUrl = "/images/categories/cooking.jpg" },
                new() { Name = "Travel Luggage (حقائب سفر)", Description = "Durable, water-resistant hardshell rollers and duffels for global explorers.", ImageUrl = "/images/categories/luggage.jpg" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // 4. Seed Products
        if (!context.Products.Any())
        {
            var tentsCat = context.Categories.First(c => c.Name.Contains("Tents"));
            var sleepingCat = context.Categories.First(c => c.Name.Contains("Sleeping"));
            var backpackCat = context.Categories.First(c => c.Name.Contains("Backpacks"));
            var cookingCat = context.Categories.First(c => c.Name.Contains("Cooking"));
            var luggageCat = context.Categories.First(c => c.Name.Contains("Luggage"));

            var products = new List<Product>
            {
                // Tents
                new() {
                    Name = "Summit Ridge 4-Person Tent",
                    Description = "A double-layer waterproof dome tent featuring quick pitch technology, gear loft, and superior ventilation for family camping trips.",
                    Price = 189.99m,
                    StockQuantity = 12,
                    CategoryId = tentsCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/tent1_main.jpg" },
                        new() { ImageUrl = "/images/products/tent1_detail1.jpg" }
                    }
                },
                new() {
                    Name = "Ultralight Solo Dome Tent",
                    Description = "Designed for solo backpackers. Weighs only 1.2 kg, crafted from high-density nylon ripstop, and features a silicone coating for heavy rains.",
                    Price = 129.50m,
                    StockQuantity = 8,
                    CategoryId = tentsCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/tent2_main.jpg" }
                    }
                },
                // Sleeping Bags
                new() {
                    Name = "Sub-Zero Mummy Sleeping Bag",
                    Description = "Rated for temperatures down to -10°C (14°F). Filled with high-loft duck down and wrapped in a windproof ripstop shell.",
                    Price = 95.00m,
                    StockQuantity = 15,
                    CategoryId = sleepingCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/sleeping1_main.jpg" },
                        new() { ImageUrl = "/images/products/sleeping1_detail1.jpg" }
                    }
                },
                new() {
                    Name = "Summer Comfort Envelope Sleeping Bag",
                    Description = "Lightweight and breathable, ideal for summer camping and couch surfing. Can be fully unzipped and used as a large camping blanket.",
                    Price = 39.99m,
                    StockQuantity = 25,
                    CategoryId = sleepingCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/sleeping2_main.jpg" }
                    }
                },
                // Backpacks
                new() {
                    Name = "Pathfinder 65L Hiking Pack",
                    Description = "Ergonomic frame system with padded hip pads, hydration sleeve, and multiple utility loops. Built for multi-day expeditions.",
                    Price = 145.00m,
                    StockQuantity = 10,
                    CategoryId = backpackCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/backpack1_main.jpg" },
                        new() { ImageUrl = "/images/products/backpack1_detail1.jpg" }
                    }
                },
                new() {
                    Name = "Vagabond Daypack 25L",
                    Description = "Water-resistant commuter daypack with dedicated laptop compartment and secret travel document pockets.",
                    Price = 55.00m,
                    StockQuantity = 30,
                    CategoryId = backpackCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/backpack2_main.jpg" }
                    }
                },
                // Cooking Gear
                new() {
                    Name = "Compact Portable Camping Stove",
                    Description = "Ultralight piezoceramic ignition camping stove, compatible with standard butane canister. Packs away in a pocket-sized plastic carry case.",
                    Price = 24.99m,
                    StockQuantity = 40,
                    CategoryId = cookingCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/cooking1_main.jpg" },
                        new() { ImageUrl = "/images/products/cooking1_detail1.jpg" }
                    }
                },
                new() {
                    Name = "Anodized Aluminum Cookset (10-Piece)",
                    Description = "Nesting pots, pans, bowls, and serving utensils. Hard-anodized aluminum conducts heat evenly and cleans up effortlessly.",
                    Price = 49.99m,
                    StockQuantity = 18,
                    CategoryId = cookingCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/cooking2_main.jpg" }
                    }
                },
                // Luggage
                new() {
                    Name = "Nomad Hardshell Roller (24-inch)",
                    Description = "Premium polycarbonate expander suitcase featuring 360-degree silent spinner wheels and TSA-approved integrated lock system.",
                    Price = 110.00m,
                    StockQuantity = 15,
                    CategoryId = luggageCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/luggage1_main.jpg" }
                    }
                },
                new() {
                    Name = "Dry-Pack Canvas Duffel Bag",
                    Description = "Rugged heavy-gauge waxed canvas with leather straps and handles. Fully waterproof zippers make it perfect for boating and road trips.",
                    Price = 75.00m,
                    StockQuantity = 22,
                    CategoryId = luggageCat.Id,
                    Images = new List<ProductImage> {
                        new() { ImageUrl = "/images/products/luggage2_main.jpg" }
                    }
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // 5. Seed Reviews
            var p1 = context.Products.First();
            var p2 = context.Products.Skip(2).First(); // Mummy sleeping bag
            var reviews = new List<Review>
            {
                new() { ProductId = p1.Id, UserId = normalUser.Id, UserFullName = "Bashar Al-Khateeb", Content = "Absolutely fantastic tent! Spent a weekend in heavy rain and not a single leak. Setup took less than 10 minutes.", Rating = 5, IsApproved = true },
                new() { ProductId = p1.Id, UserId = normalUser.Id, UserFullName = "Shorouq Ali", Content = "Spacious and beautiful design, but it is a bit heavy for ultra-backpacking.", Rating = 4, IsApproved = true },
                new() { ProductId = p2.Id, UserId = normalUser.Id, UserFullName = "Dawood Ibrahim", Content = "Kept me cozy and warm in high-altitude camping. Highly recommended!", Rating = 5, IsApproved = true },
                new() { ProductId = p1.Id, UserId = normalUser.Id, UserFullName = "Guest Tester", Content = "Testing product review pending approval. Hope it works!", Rating = 3, IsApproved = false }
            };
            context.Reviews.AddRange(reviews);

            // 6. Seed Testimonials
            var testimonials = new List<Testimonial>
            {
                new() { UserId = normalUser.Id, UserFullName = "Bashar Al-Khateeb", Content = "Antigravity Camping has the best outdoor gear I have ever bought. The Pathfinder backpack is a masterpiece of design!", IsApproved = true },
                new() { UserId = normalUser.Id, UserFullName = "Shorouq Ali", Content = "Fast delivery and premium materials. Their customer service helped me pick the right sleeping bag for my expedition.", IsApproved = true },
                new() { UserId = normalUser.Id, UserFullName = "Dawood Ibrahim", Content = "Pending Testimonial: Loved the camping stove! Compact and cooks food in minutes.", IsApproved = false }
            };
            context.Testimonials.AddRange(testimonials);

            await context.SaveChangesAsync();
        }
    }
}
