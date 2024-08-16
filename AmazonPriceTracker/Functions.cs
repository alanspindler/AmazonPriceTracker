using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Playwright;
using File = System.IO.File;
using System.Text.RegularExpressions;



namespace Functions
{
    public class Functions : PageObjects.PageObjects
    {
        public static async Task<decimal?> GetProductPrice(IPage page, string url)
        {
            await page.GotoAsync(url);
            decimal? price = null;

            if (url.Contains("amazon.com.br"))
            {
                price = await GetPriceAmazon(page, url);
                return price;
            }

            else if (url.Contains("kabum.com.br"))
            {
                price = await GetPriceKabum(page, url);
                return price;
            }

            else if (url.Contains("store.playstation.com/pt-br"))
            {
                price = await GetPricePlaystation(page, url);
                return price;
            }

            else if (url.Contains("store.steampowered.com"))
            {
                int? AppId = returnSteamIdapp(url);
                string FormatedPrice = null;
                if (AppId != null)
                {
                    price = await API.StemAPI.GetSteamPrice(AppId.ToString());
                    if (price != null)
                    {
                        FormatedPrice = price.ToString();
                        FormatedPrice = FormatedPrice.Insert(FormatedPrice.Length - 2, ".");
                    }
                    if (decimal.TryParse(FormatedPrice, out decimal DecimalPrice))
                    {
                        return DecimalPrice;
                    }
                    return null;
                }
            }
            return null;
        }

        public static async Task<string> GetProductName(IPage page, string productUrl)
        {
            string? textProductName;
            if (productUrl.Contains("amazon.com.br"))
            {
                textProductName = await page.Locator(LabelAmazonProductName).TextContentAsync();
                return textProductName;
            }
            else if (productUrl.Contains("kabum.com.br"))
            {
                textProductName = await page.Locator(LabelKabumProductName).TextContentAsync();
                return textProductName;
            }
            else if (productUrl.Contains("store.playstation.com/pt-br/"))
            {
                textProductName = await page.Locator(LabelPlaystationProductName).TextContentAsync();
                return textProductName;
            }
            else if (productUrl.Contains("store.steampowered.com"))
            {
                int? AppID = returnSteamIdapp(productUrl);
                if (AppID != null)
                {
                    textProductName = await API.StemAPI.GetSteamAppName(AppID.ToString());
                    return textProductName;
                }
            }
            return null;
        }

        public static async Task<decimal?> GetPriceAmazon(IPage page, string url)
        {
            var priceElement = await page.QuerySelectorAsync(LabelAmazonPrice);

            if (priceElement != null)
            {
                var priceText = await priceElement.InnerTextAsync();
                priceText = priceText.Replace("\n", "").Replace(",", "");

                if (decimal.TryParse(priceText, out decimal price))
                {
                    return price;
                }
            }
            return null;
        }

        public static async Task<decimal?> GetPriceKabum(IPage page, string url)
        {
            string? priceElement = null;
            int elementos = await page.Locator(LabelKabumPrice).CountAsync();
            if (elementos > 0)
            {
                priceElement = await page.Locator(LabelKabumPrice).TextContentAsync();
            }

            if (priceElement != null)
            {
                priceElement = priceElement.Replace("R$", "").Replace(".", "").Trim();

                if (decimal.TryParse(priceElement, out decimal price))
                {
                    return price;
                }
            }
            return null;
        }

        public static async Task<decimal?> GetPricePlaystation(IPage page, string url)
        {
            string? priceElement = null;
            int elementos = await page.Locator(LabelPlaystationPrice).CountAsync();
            if (elementos > 0)
            {
                priceElement = await page.Locator(LabelPlaystationPrice).TextContentAsync();
            }

            if (priceElement != null)
            {
                priceElement = priceElement.Replace("R$", "").Trim();

                if (decimal.TryParse(priceElement, out decimal price))
                {
                    return price;
                }
            }
            return null;
        }

        public static int? returnSteamIdapp(string url)
        {
            int? idApp = null;
            var uri = new Uri(url);

            var match = Regex.Match(uri.AbsolutePath, @"\/app\/(\d+)\/");

            if (match.Success)
            {
                idApp = int.Parse(match.Groups[1].Value);
                return idApp;
            }
            return null;
        }

        public static (string Email, string Password) ReadEmailCredentials()
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

        public static List<string> ReadEmailRecipients()
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

        public static void Log(string message)
        {
            File.AppendAllText("log.txt", $"{DateTime.Now}: {message}\n");
        }

        public static List<(string, decimal)> ReadProducts()
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
    }
}
