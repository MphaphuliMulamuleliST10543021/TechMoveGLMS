
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMoveGLMS.Web.Data;
using TechMoveGLMS.Web.Models;
using TechMoveGLMS.Web.Services;

namespace TechMoveGLMS.Web.Controllers;

public class ContractsController : Controller
{
    private readonly GlmsDbContext _context;
    private readonly ContractPdfService _pdfService;

    private static readonly string[] ValidStatuses =
    {
        "Draft",
        "Active",
        "Expired",
        "On Hold"
    };

    public ContractsController(
        GlmsDbContext context,
        ContractPdfService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

   
    // Display contracts and filter them by status and date range.
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        DateTime? startDate,
        DateTime? endDate)
    {
        ViewBag.StatusFilter = status ?? string.Empty;
        ViewBag.StartDateFilter = startDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.EndDateFilter = endDate?.ToString("yyyy-MM-dd") ?? string.Empty;

        var query = _context.Contracts
            .AsNoTracking()
            .Include(c => c.Client)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            var selectedStatus = ValidStatuses.First(s =>
                string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

            query = query.Where(c => c.Status == selectedStatus);
        }

        if (startDate.HasValue)
        {
            var fromDate = startDate.Value.Date;
            query = query.Where(c => c.StartDate >= fromDate);
        }

        if (endDate.HasValue)
        {
            var toDate = endDate.Value.Date;
            query = query.Where(c => c.EndDate <= toDate);
        }

        if (startDate.HasValue && endDate.HasValue &&
            startDate.Value.Date > endDate.Value.Date)
        {
            ModelState.AddModelError(
                string.Empty,
                "The start date cannot be later than the end date.");

            query = _context.Contracts
                .AsNoTracking()
                .Include(c => c.Client)
                .Where(c => false);
        }

        var contracts = await query
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.Id)
            .ToListAsync();

        return View(contracts);
    }

    // Display the contract creation form.
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadClientsAsync();

        return View(new Contract
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(12),
            Status = "Draft",
            ServiceLevel = string.Empty
        });
    }

    // Create a contract after validating the submitted values.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contract contract)
    {
        if (!ContractRules.HasValidDates(contract))
        {
            ModelState.AddModelError(
                nameof(Contract.EndDate),
                "The end date cannot be earlier than the start date.");
        }

        var clientExists = await _context.Clients
            .AnyAsync(c => c.Id == contract.ClientId);

        if (!clientExists)
        {
            ModelState.AddModelError(
                nameof(Contract.ClientId),
                "Please select a valid client.");
        }

        if (string.IsNullOrWhiteSpace(contract.Status) ||
            !ValidStatuses.Contains(
                contract.Status,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(Contract.Status),
                "Please select a valid contract status.");
        }
        else
        {
            contract.Status = ValidStatuses.First(
                status => string.Equals(
                    status,
                    contract.Status,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(contract.ServiceLevel))
        {
            ModelState.AddModelError(
                nameof(Contract.ServiceLevel),
                "Service level is required.");
        }
        else
        {
            contract.ServiceLevel = contract.ServiceLevel.Trim();
        }

        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(contract);
        }

        // Do not accept record IDs or PDF filenames from form submissions.
        contract.Id = 0;
        contract.SignedPdfFileName = null;

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Contract created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // Display the PDF upload form.
    [HttpGet]
    public async Task<IActionResult> UploadPdf(int id)
    {
        var contract = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
        {
            return NotFound();
        }

        return View(contract);
    }

    // Upload or replace a signed contract PDF.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPdf(
        int id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var contract = await _context.Contracts
            .FirstOrDefaultAsync(
                c => c.Id == id,
                cancellationToken);

        if (contract is null)
        {
            return NotFound();
        }

        if (!ContractPdfService.IsValidPdf(file))
        {
            ModelState.AddModelError(
                "file",
                "Choose a valid PDF file no larger than 5 MB.");

            return View(contract);
        }

        string? newFileName = null;

        try
        {
            newFileName = await _pdfService.SaveAsync(file!);

            var previousFileName = contract.SignedPdfFileName;
            contract.SignedPdfFileName = newFileName;

            await _context.SaveChangesAsync(cancellationToken);

            // Delete the previous file only after the database save succeeds.
            if (!string.IsNullOrWhiteSpace(previousFileName))
            {
                await _pdfService.DeleteAsync(previousFileName);
            }

            TempData["SuccessMessage"] =
                "Signed contract PDF uploaded successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Preserve request cancellation.
            throw;
        }
        catch (Exception)
        {
            // Attempt to clean up the newly uploaded file.
            if (!string.IsNullOrWhiteSpace(newFileName))
            {
                try
                {
                    await _pdfService.DeleteAsync(newFileName);
                }
                catch (IOException)
                {
                    // Cleanup failed; the file should be checked separately.
                }
            }

            ModelState.AddModelError(
                string.Empty,
                "The PDF could not be saved. Please try again.");

            return View(contract);
        }
    }

    // Download the signed PDF without exposing its storage path.
    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var contract = await _context.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null ||
            string.IsNullOrWhiteSpace(contract.SignedPdfFileName))
        {
            return NotFound();
        }

        var stream = await _pdfService.OpenReadAsync(
            contract.SignedPdfFileName);

        if (stream is null)
        {
            return NotFound();
        }

        return File(
            stream,
            "application/pdf",
            $"SignedContract-{contract.Id}.pdf");
    }


    // Display the contract editing form.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var contract = await _context.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
        {
            return NotFound();
        }

        await LoadClientsAsync();
        return View(contract);
    }

    // Save changes without allowing the form to overwrite the PDF filename.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Contract contract)
    {
        if (id != contract.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Contracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existing is null)
        {
            return NotFound();
        }

        if (!ContractRules.HasValidDates(contract))
        {
            ModelState.AddModelError(
                nameof(Contract.EndDate),
                "The end date cannot be earlier than the start date.");
        }

        var clientExists = await _context.Clients
            .AnyAsync(c => c.Id == contract.ClientId);

        if (!clientExists)
        {
            ModelState.AddModelError(
                nameof(Contract.ClientId),
                "Please select a valid client.");
        }

        if (string.IsNullOrWhiteSpace(contract.Status) ||
            !ValidStatuses.Contains(
                contract.Status,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(Contract.Status),
                "Please select a valid contract status.");
        }
        else
        {
            contract.Status = ValidStatuses.First(
                status => string.Equals(
                    status,
                    contract.Status,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(contract.ServiceLevel))
        {
            ModelState.AddModelError(
                nameof(Contract.ServiceLevel),
                "Service level is required.");
        }
        else
        {
            contract.ServiceLevel = contract.ServiceLevel.Trim();
        }

        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(contract);
        }

        // Update only permitted fields. Preserve the existing PDF filename.
        existing.ClientId = contract.ClientId;
        existing.StartDate = contract.StartDate.Date;
        existing.EndDate = contract.EndDate.Date;
        existing.Status = contract.Status;
        existing.ServiceLevel = contract.ServiceLevel;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Contract updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // Show a confirmation page before deleting a contract.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
        {
            return NotFound();
        }

        return View(contract);
    }

    // Delete the contract only after the user confirms.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var contract = await _context.Contracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
        {
            return NotFound();
        }

        // Retain the filename so the file can be cleaned up after DB deletion.
        var pdfFileName = contract.SignedPdfFileName;

        _context.Contracts.Remove(contract);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "This contract could not be deleted. It may have related service requests.";

            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(pdfFileName))
        {
            try
            {
                await _pdfService.DeleteAsync(pdfFileName);
            }
            catch (IOException)
            {
                // The contract is deleted; a leftover file can be cleaned up later.
            }
        }

        TempData["SuccessMessage"] = "Contract deleted successfully.";

        return RedirectToAction(nameof(Index));
    }


    private async Task LoadClientsAsync()
    {
        ViewBag.Clients = await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
