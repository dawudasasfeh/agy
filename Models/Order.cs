using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AGY.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Processing"; // Processing, Completed, Cancelled

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
