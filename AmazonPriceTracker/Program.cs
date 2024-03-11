using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Playwright;
using File = System.IO.File;

class AmazonPriceTracker
{

    private static async Task<decimal?> GetProductPriceAsync(IPage page, string url)
    {
        await page.GotoAsync(url);
        // Check if the URL is from Amazon
        if (url.Contains("amazon.com.br"))
        {
            var priceElement = await page.QuerySelectorAsync("#apex_desktop .a-price-whole");

            if (priceElement != null)
            {
                var priceText = await priceElement.InnerTextAsync();
                priceText = priceText.Replace("\n", "").Replace(",", "");

                if (decimal.TryParse(priceText, out decimal price))
                {
                    return price;
                }
            }
        }
        // Check if the URL is from Kabum
        else if (url.Contains("kabum.com.br"))
        {
            string? priceElement = null;
            int elements = await page.Locator(".finalPrice").CountAsync();
            if (elements > 0)
            {
                priceElement = await page.Locator(".finalPrice").TextContentAsync();
            }

            if (priceElement != null)
            {
                priceElement = priceElement.Replace("R$", "").Replace(".", "").Trim();

                if (decimal.TryParse(priceElement, out decimal price))
                {
                    return price;
                }
            }
        }
        else if (url.Contains("nike.com"))
        {
            string? priceElement = await page.Locator(".product-price >> nth = 0").TextContentAsync();
            if (priceElement != null)
            {
                priceElement = priceElement.Replace("$", "");
                if (decimal.TryParse(priceElement, out decimal price))
                {
                    return price;
                }
            }
        }
        return null;
    }

    private static (string Email, string Password) ReadEmailCredentials()
    {
        var json = File.ReadAllText("email_credentials.json");
        var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (credentials == null)
        {
            Log("Failed to deserialize credentials.");
            return (string.Empty, string.Empty); // returns a default value
        }
        if (!credentials.ContainsKey("email") || !credentials.ContainsKey("password"))
        {
            Log("Missing 'email' or 'password' in credentials.");
            return (string.Empty, string.Empty); // returns a default value
        }
        return (credentials["email"], credentials["password"]);
    }


    private static List<string> ReadEmailRecipients()
    {
        return File.ReadAllLines("email_recipients.txt").ToList();
    }

    public static async Task SendEmail(string subject, string body)
    {
        var (email, password) = ReadEmailCredentials();
        var recipients = ReadEmailRecipients();
        try
        {
            using (MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(email),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                foreach (var recipient in recipients)
                {
                    mailMessage.To.Add(new MailAddress(recipient));
                }
                using (SmtpClient smtpClient = new SmtpClient("smtp.office365.com", 587)
                {
                    Credentials = new NetworkCredential(email, password),
                    EnableSsl = true
                })
                {
                    try
                    {
                        await smtpClient.SendMailAsync(mailMessage);
                        string recipientsList = string.Join(", ", recipients);
                        Log($"Email sent to: {recipientsList}");
                    }
                    catch (SmtpException ex)
                    {
                        string recipientsList = string.Join(", ", recipients);
                        Log($"Error sending email to {recipientsList}:\nStatus code: {ex.StatusCode}\nError message: {ex.Message}\nInner error message: {ex.InnerException?.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log("Error sending email:");
            Log($"Error message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Log($"Inner error message: {ex.InnerException.Message}");
            }
        }
    }


    private static void Log(string message)
    {
        File.AppendAllText("log.txt", $"{DateTime.Now}: {message}\n");
    }

    private static List<(string, decimal)> ReadProducts()
    {
        return File.ReadAllLines("products.txt")
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .Select(line =>
            {
                var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return (parts[0], decimal.Parse(parts[1]));
            })
            .ToList();
    }

    public static async Task Main()
    {
        while (true)
        {
            var productList = ReadProducts();

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync(new() { UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.4692.99 Safari/537.36" });
            var page = await context.NewPageAsync();
            foreach (var (productUrl, targetPrice) in productList)
            {
                try
                {
                    // Check the product price
                    var currentPrice = await GetProductPriceAsync(page, productUrl);
                    string? textProductName = null;
                    if (productUrl.Contains("amazon.com.br"))
                    {
                        textProductName = await page.Locator("[class='a-size-large product-title-word-break']").TextContentAsync();
                    }
                    else if (productUrl.Contains("kabum.com.br"))
                    {
                        textProductName = await page.Locator("xpath=//div[@id='container-purchase']/div[1]/div/h1").TextContentAsync();
                    }
                    else if (productUrl.Contains("nike.com"))
                    {
                        textProductName = await page.Locator("[id=pdp_product_title] >> nth = 0").TextContentAsync();
                    }
                    string productName = textProductName != null ? textProductName.ToString().Trim() : string.Empty;
                    if (currentPrice.HasValue && currentPrice.Value < targetPrice)
                    {
                        var subject = $"Price Alert: Product {productName} below ${targetPrice}";
                        var body = $"The product {productName} at URL {productUrl} is priced at ${currentPrice.Value}.";
                        await SendEmail(subject, body);
                    }
                    else if (!currentPrice.HasValue)
                    {
                        Log($"The product {productName} at URL {productUrl} has no price (possibly out of stock).");
                    }
                    else
                    {
                        Log($"The price of product {productName} has not reached the desired value of ${targetPrice}. Current price: ${currentPrice.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.Message}");
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var screenshotPath = $"ErrorAmazon{timestamp}.jpg";
                    await System.IO.File.WriteAllBytesAsync(screenshotPath, await page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Jpeg }));
                    Log($"Screenshot of the error saved at {screenshotPath}");
                }
            }
            await browser.CloseAsync();
            await browser.DisposeAsync();
            // Wait for 30 minutes before executing the function again
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }

}
