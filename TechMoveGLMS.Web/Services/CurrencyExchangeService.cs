
using System.Net.Http.Json;

namespace TechMoveGLMS.Web.Services;

public class CurrencyExchangeService
{
    private readonly HttpClient _httpClient;

    public CurrencyExchangeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> ConvertUsdToZarAsync(
        decimal amountUsd,
        CancellationToken cancellationToken = default)
    {
        if (amountUsd <= 0 || amountUsd > 100_000_000m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amountUsd),
                "The USD amount must be greater than zero and within the permitted limit.");
        }

        var result = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(
            "latest?from=USD&to=ZAR",
            cancellationToken);

        if (result?.Rates is null ||
            !result.Rates.TryGetValue("ZAR", out var rate) ||
            rate <= 0)
        {
            throw new InvalidOperationException(
                "The exchange-rate service returned an invalid rate.");
        }

        return decimal.Round(
            amountUsd * rate,
            2,
            MidpointRounding.AwayFromZero);
    }

    private sealed class ExchangeRateResponse
    {
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
