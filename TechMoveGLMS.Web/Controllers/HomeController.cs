
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMoveGLMS.Web.Data;
using TechMoveGLMS.Web.Models;

namespace TechMoveGLMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly GlmsDbContext _context;

    public HomeController(GlmsDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var model = new DashboardViewModel
        {
            TotalClients = await _context.Clients.CountAsync(),

            TotalContracts = await _context.Contracts.CountAsync(),

            ActiveContracts = await _context.Contracts.CountAsync(c =>
                c.Status == "Active" &&
                c.StartDate <= today &&
                c.EndDate >= today),

            TotalServiceRequests =
                await _context.ServiceRequests.CountAsync(),

            PendingServiceRequests =
                await _context.ServiceRequests.CountAsync(r =>
                    r.Status == "Pending"),

            RecentContracts = await _context.Contracts
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .Take(5)
                .Select(c => new RecentContractViewModel
                {
                    Id = c.Id,
                    ClientName = c.Client != null
                        ? c.Client.Name
                        : "Unknown client",
                    ServiceLevel = c.ServiceLevel,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate
                })
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
