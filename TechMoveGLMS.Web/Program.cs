using Microsoft.EntityFrameworkCore;
using TechMoveGLMS.Web.Data;
using TechMoveGLMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<GlmsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the private contract PDF storage service.
builder.Services.AddSingleton(
    new ContractPdfService(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "App_Data",
            "ContractPdfs")));

// Register the currency exchange service BEFORE builder.Build().
builder.Services.AddHttpClient<CurrencyExchangeService>(client =>
{
    client.BaseAddress = new Uri("https://api.frankfurter.app/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
