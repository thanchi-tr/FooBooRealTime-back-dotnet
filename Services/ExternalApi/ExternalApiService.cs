namespace FooBooRealTime_back_dotnet.Services.ExternalApi
{
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ExternalApiService
    {
        private readonly HttpClient _httpClient;

        public ExternalApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Get(string url)
        {
            
            try
            {
                // Send the POST request
                var response = await _httpClient.GetAsync(url);

                // Ensure the request was successful
                response.EnsureSuccessStatusCode();

                // Read the response body
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                // Handle HTTP request exceptions
                return $"Request error: {e.Message}";
            }
        }
    }

}
