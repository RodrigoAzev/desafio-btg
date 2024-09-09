using Microsoft.AspNetCore.Mvc;
using RestServer.Domain;
using RestServer.Services;

namespace RestServer.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TextController : ControllerBase
{

    private readonly ILogger<TextController> _logger;
    private readonly ITextSummarizerService _summarizerService;

    public TextController(ILogger<TextController> logger, ITextSummarizerService summarizerService)
    {
        _logger = logger;
        _summarizerService = summarizerService;
    }

    [HttpPost("summarizer")]
    public async Task<TextExtractedProperties> GetTextProperies([FromBody] TextFunctionConfiguration textFunctionConfiguration)
    {
        return await _summarizerService.SummarizeText(textFunctionConfiguration);
    }
}
