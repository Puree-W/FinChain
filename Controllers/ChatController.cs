using Microsoft.AspNetCore.Mvc;
using FinChain.Function;
using FinChain.Model;
namespace FinChain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class ChatController : Controller
    {
        private readonly IChatProcessor _chatProcessor;
        public ChatController(IChatProcessor chatProcessor)
        {
            _chatProcessor = chatProcessor;
        }
        [HttpPost("ChatPost")]
        public async Task ChatPost([FromBody] ThaiLLMRequestModel req, [FromQuery] string model,CancellationToken cancellationToken)
        {
            try 
            {
                Response.ContentType = "text/event-stream";

                await foreach (var chunk in _chatProcessor.StreamChatMessageAsync(req, model, cancellationToken))
                {
                    await Response.WriteAsync(chunk, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Response.ContentType = "application/json";
                var errorResponse = new { error = ex.Message };
                await Response.WriteAsJsonAsync(errorResponse, cancellationToken);
            }
        }
        [HttpGet("GetHistory")]
        public async Task<ActionResult<HistoryLLMModel>> GetHistory([FromQuery] string topicId)
        {
            try 
            {
                var result = await _chatProcessor.GetHistoryMessage(topicId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("GetAllHistory")]
        public async Task<ActionResult<HistoryLLMModel>> GetAllHistory()
        {
            try
            {
                var result = await _chatProcessor.GetAllHistoryMessage();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
