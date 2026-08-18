using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AGY.Models;

public class WishlistItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
