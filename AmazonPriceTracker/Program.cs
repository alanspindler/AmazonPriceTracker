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

        // Substitua ".price-selector" pelo seletor CSS correto do preço do produto na Amazon
        var priceElement = await page.QuerySelectorAsync("[class=a-price-whole] >> nth=1");
        if (priceElement != null)
        {
            var priceText = await priceElement.InnerTextAsync();

            // Analise o texto do preço para obter o valor decimal
            if (decimal.TryParse(priceText, out decimal price))
            {
                return price;
            }
        }
        return null;
    }

    private static (string Email, string Password) ReadEmailCredentials()
    {
        var json = File.ReadAllText("email_credentials.json");
        var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

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

            using SmtpClient smtpClient = new("smtp.office365.com", 587)
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
            var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            foreach (var (productUrl, targetPrice) in productList)
            {
                try
                {
                    // Verificar o preço do produto
                    var currentPrice = await GetProductPriceAsync(page, productUrl);

                    if (currentPrice.HasValue && currentPrice.Value <= targetPrice)
                    {
                        var subject = $"Alerta de preço: Produto abaixo de R${targetPrice}";
                        var body = $"O produto na URL {productUrl} está com um preço de R${currentPrice.Value}.";
                        await SendEmail(subject, body);
                    }
                    else if (!currentPrice.HasValue)
                    {
                        Log($"O produto na URL {productUrl} está sem preço (possivelmente fora de estoque).");
                    }
                    else
                    {
                        Log($"O preço do produto não atingiu o valor desejado. Preço atual: R${currentPrice.Value}");
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
