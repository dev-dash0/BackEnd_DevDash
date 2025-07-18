using DevDash.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AgentController(HttpClient httpClient, IOptions<AppSettings> appSettings)
        {
            _httpClient = httpClient;
            _baseUrl = appSettings.Value.BaseUrl;
        }
        [Authorize]
        [HttpPost]
        public async Task StreamFromAI([FromBody] dynamic aiRequest)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");

            var content = new StringContent(JsonConvert.SerializeObject(aiRequest), Encoding.UTF8, "application/json");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/llm/agent")
                {
                    Content = content
                };

                var bearerToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        await Response.WriteAsync($"data: {line}\n\n");
                        await Response.Body.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"data: Error occurred - {ex.Message}\n\n");
                await Response.Body.FlushAsync();
            }
        }




    }
}
