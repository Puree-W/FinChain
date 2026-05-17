using FinChain.Function;
using FinChain.Model;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FinChain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : Controller
    {
        private readonly IConfigurationProcessor _processor;

        public ConfigurationController(IConfigurationProcessor processor)
        {
            _processor = processor;
        }

        // ─── AI config ───
        [HttpGet("AiConfig")]
        public async Task<IActionResult> GetAiConfigs()
        {
            try
            {
                var result = await _processor.GetAiConfigsAsync();
                return Ok(new ApiReturnModel<AiConfigModel[]>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("AiConfig/{id:long}")]
        public async Task<IActionResult> GetAiConfig(long id)
        {
            try
            {
                var result = await _processor.GetAiConfigAsync(id);
                if (result == null) return NotFound(new { error = $"ai_config {id} not found" });
                return Ok(new ApiReturnModel<AiConfigModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("AiConfig")]
        public async Task<IActionResult> CreateAiConfig([FromBody] AiConfigUpsert payload)
        {
            try
            {
                var result = await _processor.CreateAiConfigAsync(payload);
                return Ok(new ApiReturnModel<AiConfigModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("AiConfig/{id:long}")]
        public async Task<IActionResult> UpdateAiConfig(long id, [FromBody] AiConfigUpsert payload)
        {
            try
            {
                var result = await _processor.UpdateAiConfigAsync(id, payload);
                return Ok(new ApiReturnModel<AiConfigModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("AiConfig/{id:long}")]
        public async Task<IActionResult> DeleteAiConfig(long id)
        {
            try
            {
                await _processor.DeleteAiConfigAsync(id);
                return Ok(new ApiReturnModel<bool>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success"
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ─── Embedding config ───
        [HttpGet("EmbeddingConfig")]
        public async Task<IActionResult> GetEmbeddingConfigs()
        {
            try
            {
                var result = await _processor.GetEmbeddingConfigsAsync();
                return Ok(new ApiReturnModel<EmbeddingConfigModel[]>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("EmbeddingConfig/{id:long}")]
        public async Task<IActionResult> GetEmbeddingConfig(long id)
        {
            try
            {
                var result = await _processor.GetEmbeddingConfigAsync(id);
                if (result == null) return NotFound(new { error = $"embedding_config {id} not found" });
                return Ok(new ApiReturnModel<EmbeddingConfigModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("EmbeddingConfig")]
        public async Task<IActionResult> CreateEmbeddingConfig([FromBody] EmbeddingConfigUpsert payload)
        {
            try
            {
                var result = await _processor.CreateEmbeddingConfigAsync(payload);
                return Ok(new ApiReturnModel<EmbeddingConfigModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("EmbeddingConfig/{id:long}")]
        public async Task<IActionResult> UpdateEmbeddingConfig(long id, [FromBody] EmbeddingConfigUpsert payload)
        {
            try
            {
                var result = await _processor.UpdateEmbeddingConfigAsync(id, payload);
                return Ok(new ApiReturnModel<EmbeddingConfigModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("EmbeddingConfig/{id:long}")]
        public async Task<IActionResult> DeleteEmbeddingConfig(long id)
        {
            try
            {
                await _processor.DeleteEmbeddingConfigAsync(id);
                return Ok(new ApiReturnModel<bool>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success"
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ─── Model template ───
        [HttpGet("ModelTemplate")]
        public async Task<IActionResult> GetModelTemplates()
        {
            try
            {
                var result = await _processor.GetModelTemplatesAsync();
                return Ok(new ApiReturnModel<ModelTemplateModel[]>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("ModelTemplate/Default")]
        public async Task<IActionResult> GetDefaultModelTemplate()
        {
            try
            {
                var result = await _processor.GetDefaultModelTemplateAsync();
                return Ok(new ApiReturnModel<ModelTemplateModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = result == null ? "No default template configured" : "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("ModelTemplate/{id}")]
        public async Task<IActionResult> GetModelTemplate(string id)
        {
            try
            {
                var result = await _processor.GetModelTemplateAsync(id);
                if (result == null) return NotFound(new { error = $"model_template {id} not found" });
                return Ok(new ApiReturnModel<ModelTemplateModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("ModelTemplate")]
        public async Task<IActionResult> CreateModelTemplate([FromBody] ModelTemplateUpsert payload)
        {
            try
            {
                var result = await _processor.CreateModelTemplateAsync(payload);
                return Ok(new ApiReturnModel<ModelTemplateModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("ModelTemplate/{id}")]
        public async Task<IActionResult> UpdateModelTemplate(string id, [FromBody] ModelTemplateUpsert payload)
        {
            try
            {
                var result = await _processor.UpdateModelTemplateAsync(id, payload);
                return Ok(new ApiReturnModel<ModelTemplateModel>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = result
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("ModelTemplate/{id}/Default")]
        public async Task<IActionResult> SetDefaultTemplate(string id)
        {
            try
            {
                await _processor.SetDefaultTemplateAsync(id);
                return Ok(new ApiReturnModel<bool>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success"
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("ModelTemplate/{id}")]
        public async Task<IActionResult> DeleteModelTemplate(string id)
        {
            try
            {
                await _processor.DeleteModelTemplateAsync(id);
                return Ok(new ApiReturnModel<bool>
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success"
                });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
