
using Microsoft.Playwright;


class AmazonPriceTracker : Functions.Functions
{
    public static async Task Main()
    {
        while (true)
        {
            var productList = ReadProducts();

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync(new() { UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.4692.99 Safari/537.36" });
            var page = await context.NewPageAsync();
            await page.RouteAsync("**/*", (route) =>
            {
                var url = route.Request.Url;
                if (url.Contains("https://aax-us-east-retail-direct.amazon.com/e/xsp/getAd?placementId") || url.Contains("https://unagi.amazon.com.br/1/events/com.amazon.csm.csa.prod") || url.Contains("https://completion.amazon.com.br/api/2017/suggestions") || url.Contains("https://unagi-na.amazon.com/1/events/com.amazon.eel.SponsoredProductsEventTracking.prod"))
                {
                    return route.AbortAsync();
                }
                return route.ContinueAsync();
            });

            await page.RouteAsync("https://www.amazon.com.br/dram/renderLazyLoaded", (route) =>
            {
                return route.AbortAsync();
            });
            await page.RouteAsync("https://aax-us-east-retail-direct.amazon.com/e/xsp/getAd?placementId", (route) =>
            {
                return route.AbortAsync();
            });
            foreach (var (productUrl, targetPrice) in productList)
            {
                try
                {                    
                    var currentPrice = await GetProductPrice(page, productUrl);
                    string? textProductName = null;

                    textProductName = await GetProductName(page, productUrl);

                    string productName = textProductName != null ? textProductName.ToString().Trim() : string.Empty;

                    if (currentPrice.HasValue && currentPrice.Value < targetPrice)
                    {
                        var subject = $"Alerta de preço: Produto {productName} abaixo de R${targetPrice}";
                        var body = $"O produto {productName} na URL {productUrl} está com um preço de R${currentPrice.Value}.";
                        await SendEmail(subject, body);
                    }
                    else if (!currentPrice.HasValue)
                    {
                        Log($"O produto {productName} na URL {productUrl} está sem preço (possivelmente fora de estoque).");
                    }
                    else
                    {
                        Log($"O preço do produto {productName} não atingiu o valor desejado de R${targetPrice}. Preço atual: R${currentPrice.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Erro: {ex.Message}");
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var screenshotPath = $"ErroAmazon{timestamp}.jpg";
                    await System.IO.File.WriteAllBytesAsync(screenshotPath, await page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Jpeg }));
                    Log($"Screenshot do erro salvo em {screenshotPath}");
                }
            }
            await browser.CloseAsync();
            await browser.DisposeAsync();
            // Aguarde 30 minutos antes de executar a função novamente
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }   
}
