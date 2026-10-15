
namespace TechMoveGLMS.Web.Models;

public class DashboardViewModel
{
    public int TotalClients { get; set; }

    public int TotalContracts { get; set; }

    public int ActiveContracts { get; set; }

    public int TotalServiceRequests { get; set; }

    public int PendingServiceRequests { get; set; }

    public List<RecentContractViewModel> RecentContracts { get; set; }
        = new();
}

public class RecentContractViewModel
{
    public int Id { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string ServiceLevel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
