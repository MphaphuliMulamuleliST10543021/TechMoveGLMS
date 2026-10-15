
using System.ComponentModel.DataAnnotations;

namespace TechMoveGLMS.Web.Models;

public class ServiceRequest
{
    public int Id { get; set; }

    [Required]
    public int ContractId { get; set; }

    public Contract? Contract { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000000)]
    public decimal CostUsd { get; set; }

    [Range(0, 1000000000)]
    public decimal CostZar { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
