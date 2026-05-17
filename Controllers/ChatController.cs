using Microsoft.AspNetCore.Mvc;
using FinChain.Function;
using FinChain.Model;
using System.Net;
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
        public async Task ChatPost([FromBody] ThaiLLMRequestModel req, [FromQuery] string model, CancellationToken cancellationToken)
        {
            try
            {
                // Resolve / create topic before headers are flushed so we can hand the id
                // back to the client via X-Topic-Id for subsequent multi-turn requests.
                var topicId = await _chatProcessor.EnsureTopicAsync(req);
                Response.Headers["X-Topic-Id"] = topicId;
                Response.Headers["Access-Control-Expose-Headers"] = "X-Topic-Id";
                Response.ContentType = "text/event-stream";

                await foreach (var chunk in _chatProcessor.StreamChatMessageAsync(req, model, topicId, cancellationToken))
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
        public async Task<IActionResult> GetHistory([FromQuery] string topicId)
        {
            try
            {
                var result = await _chatProcessor.GetHistoryMessage(topicId);
                return Ok(new ApiReturnModel<HistoryLLMModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("GetAllHistory")]
        public async Task<IActionResult> GetAllHistory()
        {
            try
            {
                var result = await _chatProcessor.GetAllHistoryMessage();
                return Ok(new ApiReturnModel<TopicMessage[]>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPut("UpdateTopicName")]
        public async Task<IActionResult> UpdateTopicName(TopicMessage topicMessage)
        {
            try
            {
                await _chatProcessor.UpdateTopicName(topicMessage.Id, topicMessage.TopicName);
                return Ok(new ApiReturnModel<bool>
                { 
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpDelete("DeleteTopic")]
        public async Task<IActionResult> DeleteTopic(string topicId)
        {
            try
            {   
                await _chatProcessor.DeleteTopic(topicId);
                return Ok(new ApiReturnModel<bool>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        
    }
}
