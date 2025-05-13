using chatbot_api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using chatbot_api.Model;

namespace chatbot_api.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelegramService _telegramService;
        private readonly MongoService _mongoService;

        public TelegramController(
            IHttpClientFactory httpClientFactory,
            TelegramService telegramService,
            MongoService mongoService)
        {
            _httpClientFactory = httpClientFactory;
            _telegramService = telegramService;
            _mongoService = mongoService;
        }

        [HttpPost("update")]
        public async Task<IActionResult> PostUpdate([FromBody] JsonElement update)
        {
            if (!update.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("text", out var textProp) ||
                !message.TryGetProperty("chat", out var chatProp))
            {
                return Ok(); // Ignorar actualizaciones no relevantes
            }

            var userMessage = textProp.GetString();
            var chatId = chatProp.GetProperty("id").GetInt64();
            var lower = userMessage?.ToLower() ?? "";

            bool isRegisterIntent = lower.Contains("registrarme") ||
                                    lower.Contains("cómo me registro") ||
                                    lower.Contains("quiero registrarme");

            var openAiClient = _httpClientFactory.CreateClient("OpenAI");

            var messages = new List<object>
            {
                new { role = "system", content = isRegisterIntent
                    ? "Eres un asistente que registra usuarios. Pide los siguientes datos uno por uno: Nombre, Apellido, CI, Teléfono y Fecha de nacimiento. Cuando tengas todos, responde con el siguiente formato:\n\nRegistro:\nNombre: ...\nApellido: ...\nCI: ...\nTeléfono: ...\nNacimiento: YYYY-MM-DD"
                    : "You are a helpful assistant." },
                new { role = "user", content = userMessage }
            };

            var openAiBody = new
            {
                model = "gpt-4o",
                messages = messages,
                temperature = 0.6,
                max_tokens = 300
            };

            var content = new StringContent(JsonSerializer.Serialize(openAiBody), Encoding.UTF8, "application/json");
            var openAiResponse = await openAiClient.PostAsync("chat/completions", content);
            var openAiJson = await openAiResponse.Content.ReadAsStringAsync();

            string botReply;
            string? firstName = null, lastName = null, ci = null, phone = null;
            DateOnly? birthdate = null;

            try
            {
                using var doc = JsonDocument.Parse(openAiJson);
                botReply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "I couldn't generate a response.";

                // Intenta extraer datos si el formato incluye "Registro:"
                if (botReply.Contains("Registro:"))
                {
                    foreach (var line in botReply.Split('\n'))
                    {
                        if (line.Contains("Nombre:")) firstName = line.Split("Nombre:")[1].Trim();
                        if (line.Contains("Apellido:")) lastName = line.Split("Apellido:")[1].Trim();
                        if (line.Contains("CI:")) ci = line.Split("CI:")[1].Trim();
                        if (line.Contains("Teléfono:")) phone = line.Split("Teléfono:")[1].Trim();
                        if (line.Contains("Nacimiento:"))
                        {
                            var bday = line.Split("Nacimiento:")[1].Trim();
                            if (DateOnly.TryParse(bday, out var parsed))
                                birthdate = parsed;
                        }
                    }

                    if (firstName != null && lastName != null && phone != null && birthdate.HasValue)
                    {
                        var erpClient = _httpClientFactory.CreateClient("CompanyAPI");
                        var person = new
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            CI = ci,
                            Phone = phone,
                            Birthdate = birthdate.Value.ToString("yyyy-MM-dd")
                        };

                        var erpBody = new StringContent(JsonSerializer.Serialize(person), Encoding.UTF8, "application/json");
                        var result = await erpClient.PostAsync("people", erpBody);

                        if (result.IsSuccessStatusCode)
                            botReply += "\n✅ Te has registrado exitosamente.";
                        else
                            botReply += "\n⚠️ Hubo un error al registrar tus datos.";
                    }

                }

                // Guardar conversación
                var collection = _mongoService.GetCollection<ChatMessage>("conversations");
                var conversation = new ChatMessage
                {
                    ChatId = chatId,
                    UserMessage = userMessage ?? "",
                    BotResponse = botReply ?? "",
                    Timestamp = DateTime.UtcNow
                };
                await collection.InsertOneAsync(conversation);
            }
            catch
            {
                botReply = "❌ Error al procesar la respuesta del asistente.";
            }

            // Enviar respuesta a Telegram
            var telegramClient = _httpClientFactory.CreateClient();
            var telegramContent = new StringContent(JsonSerializer.Serialize(new
            {
                chat_id = chatId,
                text = botReply.Trim()
            }), Encoding.UTF8, "application/json");

            await telegramClient.PostAsync(_telegramService.GetSendMessageUrl(), telegramContent);
            return Ok();
        }
    }
}
