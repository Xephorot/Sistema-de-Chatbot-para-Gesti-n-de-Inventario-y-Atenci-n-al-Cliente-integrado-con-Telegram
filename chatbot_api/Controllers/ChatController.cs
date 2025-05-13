using chatbot_api.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace chatbot_api.Controllers
{
    [ApiController]
    [Route("api/status")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = request.Prompt }
                },
                temperature = 0.7,
                max_tokens = 200
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, responseText);

            using var doc = JsonDocument.Parse(responseText);
            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return Ok(new { response = message });
        }
    }
}
