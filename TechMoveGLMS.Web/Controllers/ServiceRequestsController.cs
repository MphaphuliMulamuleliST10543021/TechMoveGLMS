
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMoveGLMS.Web.Data;
using TechMoveGLMS.Web.Models;
using TechMoveGLMS.Web.Services;

namespace TechMoveGLMS.Web.Controllers;

public class ServiceRequestsController : Controller
{
    private readonly GlmsDbContext _context;
    private readonly CurrencyExchangeService _currencyService;
    private readonly ILogger<ServiceRequestsController> _logger;

    public ServiceRequestsController(
        GlmsDbContext context,
        CurrencyExchangeService currencyService,
        ILogger<ServiceRequestsController> logger)
    {
        _context = context;
        _currencyService = currencyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var requests = await _context.ServiceRequests
            .AsNoTracking()
            .Include(s => s.Contract)
            .ThenInclude(c => c!.Client)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadActiveContractsAsync();
        return View(new ServiceRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("ContractId,Description,CostUsd")] ServiceRequest request,
        CancellationToken cancellationToken)
    {
        request.Description = request.Description?.Trim() ?? string.Empty;

        var contract = await _context.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == request.ContractId,
                cancellationToken);

        if (contract is null)
        {
            ModelState.AddModelError(
                nameof(request.ContractId),
                "Select a valid contract.");
        }
        else if (!ContractRules.CanCreateServiceRequest(
                     contract, DateTime.Today))
        {
            ModelState.AddModelError(
                nameof(request.ContractId),
                "Requests can only be created for active contracts within their valid dates.");
        }

        if (!ModelState.IsValid)
        {
            await LoadActiveContractsAsync(cancellationToken);
            return View(request);
        }

        try
        {
            var costZar = await _currencyService.ConvertUsdToZarAsync(
                request.CostUsd,
                cancellationToken);

            // Only server-calculated values are saved.
            request.CostZar = costZar;
            request.Status = "Pending";
            request.CreatedAt = DateTime.UtcNow;
            request.Contract = null;

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Service request created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Exchange-rate API request failed.");

            ModelState.AddModelError(
                string.Empty,
                "The exchange-rate service is unavailable. Please try again.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Exchange-rate API request timed out.");

            ModelState.AddModelError(
                string.Empty,
                "The exchange-rate request timed out. Please try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Exchange-rate API returned invalid JSON.");

            ModelState.AddModelError(
                string.Empty,
                "The exchange-rate service returned an invalid response. Please try again.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Exchange-rate response was invalid.");

            ModelState.AddModelError(
                string.Empty,
                "A valid exchange rate could not be obtained. Please try again.");
        }

        await LoadActiveContractsAsync(cancellationToken);
        return View(request);
    }

    private async Task LoadActiveContractsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        ViewBag.Contracts = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.Client)
            .Where(c => c.Status == "Active"
                        && c.StartDate <= today
                        && c.EndDate >= today)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }
}
