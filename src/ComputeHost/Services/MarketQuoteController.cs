using Microsoft.AspNetCore.Mvc;

namespace ComputeHost.Services;

[ApiController]
[Route("api/[controller]")]
public class MarketQuoteController : ControllerBase
{
    [HttpGet("{instrumentId}")]
    public IActionResult GetQuotes(string instrumentId)
    {
        // Return a small synthetic set of call quotes; in real app this hits an external service
        var now = DateTime.UtcNow;
        var maturities = new[] { now.AddDays(30), now.AddDays(60), now.AddDays(90) };
        var strikes = Enumerable.Range(90, 9).Select(i => (double)i * 1.0).ToArray();

        var quotes = new List<object>();
        var rnd = new Random(instrumentId.GetHashCode());
        foreach (var m in maturities)
        {
            foreach (var k in strikes)
            {
                double mid = 1.0 + (k - 100) * 0.02 + (Array.IndexOf(maturities, m) * 0.1);
                double bid = Math.Max(0.01, mid - 0.05 - rnd.NextDouble() * 0.02);
                double ask = bid + 0.1 + rnd.NextDouble() * 0.02;
                quotes.Add(new {
                    side = "C",
                    strike = k,
                    bid,
                    ask,
                    maturity = m.ToString("yyyy-MM-dd")
                });
            }
        }
        return Ok(quotes);
    }
}
