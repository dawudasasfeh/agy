using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AGY.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string UserFullName { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    public bool IsApproved { get; set; } = false;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
