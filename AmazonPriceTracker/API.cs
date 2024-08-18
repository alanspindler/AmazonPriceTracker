using RestSharp;
using Newtonsoft.Json;

namespace API
{
    public class PriceOverview
    {
        public int final { get; set; }
        public string currency { get; set; }
    }

    public class Data
    {
        public PriceOverview price_overview { get; set; }

        public string name { get; set; }
    }

    public class AppData
    {
        public bool success { get; set; }
        public Data data { get; set; }
    }

    public static class StemAPI
    {
        public static async Task<int?> GetSteamPrice(string appId)
        {
            var options = new RestClientOptions($"https://store.steampowered.com/api/appdetails?appids={appId}&cc=bra");
            var client = new RestClient(options);
            var request = new RestRequest("", Method.Get);
            RestResponse response = await client.ExecuteAsync(request);
            if (response != null)
            {
                var steamResponse = JsonConvert.DeserializeObject<Dictionary<string, AppData>>(response.Content);
                
                if (steamResponse != null)
                {
                    foreach (var item in steamResponse)
                    {
                        appId = item.Key;
                        var appData = item.Value;
                        if (appData != null && appData.success)
                        {
                            var currency = appData.data.price_overview.currency;
                            if (currency != "BRL")
                            {
                                return null;
                            }
                            else
                            {
                                var finalPrice = appData.data.price_overview.final;
                                finalPrice = finalPrice / 100;
                                return finalPrice;
                            }
                        }
                        return null;
                    }
                    return null;
                }
                return null;
            }
            return null;
        }

        public static async Task<string?> GetSteamAppName(string appId)
        {
            var options = new RestClientOptions($"https://store.steampowered.com/api/appdetails?appids={appId}");
            var client = new RestClient(options);
            var request = new RestRequest("", Method.Get);
            RestResponse response = await client.ExecuteAsync(request);
            if (response != null)
            {
                var steamResponse = JsonConvert.DeserializeObject<Dictionary<string, AppData>>(response.Content);
                if (steamResponse != null)
                {
                    foreach (var item in steamResponse)
                    {
                        appId = item.Key;
                        var appData = item.Value;

                        if (appData != null && appData.success)
                        {
                            var name = appData.data.name;
                            return name;
                        }
                        return null;
                    }
                    return null;
                }
                return null;
            }
            return null;
        }
    }
}

