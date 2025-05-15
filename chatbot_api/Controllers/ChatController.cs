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
        new {
            role = "system",
            content = @"Actúas como un vendedor y cotizador de productos. Cuando un usuario proporcione su nombre o intereses, buscarás en tu contexto si hay productos disponibles que coincidan. Utilizarás comandos para guardar la información relevante durante la conversación. 

            Reglas clave:
            1. Si el cliente proporciona su nombre, al final del chat deberás insertar el siguiente comando: /savename (Nombre).
            2. Al cotizar, utiliza exclusivamente la información de productos disponible en este mensaje del sistema.

            Inventario actual:
            - Producto: Televisor | Cantidad: 10 unidades | Precio por unidad: 5
            - Producto: Laptop | Cantidad: 15 unidades | Precio por unidad: 12
            - Producto: Celular | Cantidad: 0 unidades | Precio por unidad: 8
            - Producto: Tablet | Cantidad: 8 unidades | Precio por unidad: 10
            - Producto: Monitor | Cantidad: 12 unidades | Precio por unidad: 7
            - Producto: Impresora | Cantidad: 6 unidades | Precio por unidad: 9
            - Producto: Teclado | Cantidad: 25 unidades | Precio por unidad: 2
            - Producto: Mouse | Cantidad: 30 unidades | Precio por unidad: 1.5

            Mantén una actitud cordial, clara y profesional en todo momento."
                    },
                    new { role = "user", content = request.Prompt }
                },
                temperature = 0.2,
                max_tokens = 500 
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
