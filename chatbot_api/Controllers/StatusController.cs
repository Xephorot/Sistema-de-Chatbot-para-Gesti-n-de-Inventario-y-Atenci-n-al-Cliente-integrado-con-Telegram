using chatbot_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace chatbot_api.Controllers
{
    [ApiController]
    [Route("api/status")]
    public class StatusController : ControllerBase
    {
        private readonly MongoService _mongoService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelegramService _telegramService;

        public StatusController(
            MongoService mongoService,
            IHttpClientFactory httpClientFactory,
            TelegramService telegramService)
        {
            _mongoService = mongoService;
            _httpClientFactory = httpClientFactory;
            _telegramService = telegramService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            var result = new Dictionary<string, string>();

            // MongoDB
            try
            {
                await _mongoService.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                result["MongoDB"] = "OK";
            }
            catch (Exception ex)
            {
                result["MongoDB"] = $"Error: {ex.Message}";
            }

            // OpenAI
            try
            {
                var openAiClient = _httpClientFactory.CreateClient("OpenAI");
                var response = await openAiClient.GetAsync("models");
                result["OpenAI"] = response.IsSuccessStatusCode ? "OK" : $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                result["OpenAI"] = $"Error: {ex.Message}";
            }

            // Company API
            try
            {
                var companyClient = _httpClientFactory.CreateClient("CompanyAPI");
                var response = await companyClient.GetAsync("api/health");
                result["CompanyAPI"] = response.IsSuccessStatusCode ? "OK" : $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                result["CompanyAPI"] = $"Error: {ex.Message}";
            }

            // Telegram
            try
            {
                var telegramClient = _httpClientFactory.CreateClient();
                var response = await telegramClient.GetAsync(
                    $"https://api.telegram.org/bot{_telegramService.BotToken}/getMe");

                result["Telegram"] = response.IsSuccessStatusCode ? "OK" : $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                result["Telegram"] = $"Error: {ex.Message}";
            }

            return Ok(result);
        }
    }
}
