using RestServer.Domain;

namespace RestServer.Services;
public interface ITextSummarizerService {
    Task<TextExtractedProperties> SummarizeText(TextFunctionConfiguration textFunctionConfiguration);
}


