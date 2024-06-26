﻿using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Playwright;
using File = System.IO.File;

class AmazonPriceTracker
{

    private static async Task<decimal?> GetProductPriceAsync(IPage page, string url)
    {
        await page.GotoAsync(url);
        // Verifica se o URL é da Amazon
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
        // Verifica se o URL é da Kabum
        else if (url.Contains("kabum.com.br"))
        {
            string? priceElement = null;
            int elementos = await page.Locator(".finalPrice").CountAsync();
            if (elementos > 0)
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

        else if (url.Contains("store.playstation.com/pt-br"))
        {
            string? priceElement = null;
            int elementos = await page.Locator("[class='psw-l-line-left psw-l-line-wrap']").CountAsync();
            if (elementos > 0)
            {
                priceElement = await page.Locator("[class='psw-l-line-left psw-l-line-wrap']").TextContentAsync();
            }

            if (priceElement != null)
            {
                priceElement = priceElement.Replace("R$", "").Trim();

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
            Log("Falha ao deserializar as credenciais.");
            return (string.Empty, string.Empty); // retorna um valor padrão
        }
        if (!credentials.ContainsKey("email") || !credentials.ContainsKey("password"))
        {
            Log("Faltando 'email' ou 'password' nas credenciais.");
            return (string.Empty, string.Empty); // retorna um valor padrão
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
            using MailMessage mailMessage = new()
            {
                From = new MailAddress(email),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(new MailAddress(recipient));
            }
            using SmtpClient smtpClient = new SmtpClient("smtp.office365.com", 587)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true
            };
            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                string recipientsList = string.Join(", ", recipients);
                Log($"E-mail enviado para: {recipientsList}");
            }
            catch (SmtpException ex)
            {
                string recipientsList = string.Join(", ", recipients);
                Log($"Erro ao enviar e-mail para {recipientsList}:\nCódigo de status: {ex.StatusCode}\nMensagem de erro: {ex.Message}\nMensagem de erro interna: {ex.InnerException?.Message}");
            }
        }
        catch (Exception ex)
        {
            Log("Erro ao enviar e-mail:");
            Log($"Mensagem de erro: {ex.Message}");
            if (ex.InnerException != null)
            {
                Log($"Mensagem de erro interna: {ex.InnerException.Message}");
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
            var context = await browser.NewContextAsync(new() { UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36" });
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
            await page.RouteAsync("**/*.{png,jpg,jpeg}", (route) =>
                {
                    return route.AbortAsync();
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
                    // Verificar o preço do produto
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
                    else if (productUrl.Contains("store.playstation.com/pt-br/"))
                    {
                        textProductName = await page.Locator("[class='psw-m-b-5 psw-t-title-l psw-t-size-7 psw-l-line-break-word']").TextContentAsync();
                    }
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
