namespace chatbot_api.Services
{
    public class CompanyApiService
    {
        private readonly HttpClient _client;

        public CompanyApiService(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient("CompanyAPI");
        }

        public async Task<string> GetProductByIdAsync(string id)
        {
            var response = await _client.GetAsync($"api/products/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetBranchesAsync()
        {
            var response = await _client.GetAsync("api/branches");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
