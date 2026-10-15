
using System.ComponentModel.DataAnnotations;

namespace TechMoveGLMS.Web.Models;

public class Client
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string ContactNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Region { get; set; } = string.Empty;

    public ICollection<Contract> Contracts { get; set; }
        = new List<Contract>();
}
