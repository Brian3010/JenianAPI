using JenianAPI.Services.Interfaces;

namespace JenianAPI.Services
{
  public class OllamaParserService : IParserService
  {
    private readonly ILogger<OllamaParserService> _logger;
    private readonly OllamaClient _ollamaClient;
    private readonly IConfiguration _configuration;

    public OllamaParserService(ILogger<OllamaParserService> logger, OllamaClient ollamaClient, IConfiguration configuration) {
      _logger = logger;
      _ollamaClient = ollamaClient;
      _configuration = configuration;
    }

    public Task<string> ExtractShiftAsync(string orcText, string staffName, CancellationToken cancellationToken) {
      throw new NotImplementedException();
    }

    public Task<string> ExtractTextFromPhotoAsync(MemoryStream fileStream, CancellationToken cancellationToken) {
      throw new NotImplementedException();
    }
  }
}


