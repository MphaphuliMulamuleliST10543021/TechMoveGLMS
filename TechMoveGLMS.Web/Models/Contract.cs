
using System.ComponentModel.DataAnnotations;

namespace TechMoveGLMS.Web.Models;

public class Contract
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    public Client? Client { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Draft";

    [Required]
    [StringLength(100)]
    public string ServiceLevel { get; set; } = string.Empty;

    // Stores the server-generated PDF filename, not the PDF itself.
    [StringLength(255)]
    public string? SignedPdfFileName { get; set; }

    public ICollection<ServiceRequest> ServiceRequests { get; set; }
        = new List<ServiceRequest>();
}
