
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMoveGLMS.Web.Data;
using TechMoveGLMS.Web.Models;

namespace TechMoveGLMS.Web.Controllers;

public class ClientsController : Controller
{
    private readonly GlmsDbContext _context;

    public ClientsController(GlmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var clients = await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(clients);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Client());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Client client)
    {
        NormalizeClient(client);

        if (!ModelState.IsValid)
        {
            return View(client);
        }

        var email = client.Email;

        var emailExists = await _context.Clients
            .AnyAsync(c => c.Email.ToLower() == email.ToLower());

        if (emailExists)
        {
            ModelState.AddModelError(
                nameof(Client.Email),
                "A client with this email address already exists.");

            return View(client);
        }

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Client created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Client client)
    {
        if (id != client.Id)
        {
            return BadRequest();
        }

        NormalizeClient(client);

        if (!ModelState.IsValid)
        {
            return View(client);
        }

        var emailExists = await _context.Clients
            .AnyAsync(c =>
                c.Id != id &&
                c.Email.ToLower() == client.Email.ToLower());

        if (emailExists)
        {
            ModelState.AddModelError(
                nameof(Client.Email),
                "Another client already uses this email address.");

            return View(client);
        }

        var existingClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existingClient is null)
        {
            return NotFound();
        }

        existingClient.Name = client.Name;
        existingClient.Email = client.Email;
        existingClient.ContactNumber = client.ContactNumber;
        existingClient.Region = client.Region;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Client updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _context.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null)
        {
            return NotFound();
        }

        if (client.Contracts.Any())
        {
            TempData["ErrorMessage"] =
                "This client cannot be deleted because contracts are linked to it.";

            return RedirectToAction(nameof(Index));
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Client deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeClient(Client client)
    {
        client.Name = client.Name?.Trim() ?? string.Empty;
        client.Email = client.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;
        client.ContactNumber = client.ContactNumber?.Trim()
            ?? string.Empty;
        client.Region = client.Region?.Trim() ?? string.Empty;
    }
}
