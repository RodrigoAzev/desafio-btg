namespace RestServer.Domain;

public class TextExtractedProperties {
    public List<WordOccurrence> wordOccurrences { get; set; }
    public int totalWords { get; set; }
    public int totalBlankSpaces { get; set; }
    public int totalTargetsFound { get; set; }
}

public class OptionalActionResult: TextFunctionConfiguration
{
    public string? result { get; set; }
    public int? totalTargetsFound { get; set; }
}
public class WordOccurrence
{
    public string word { get; set; }
    public int occurrences { get; set; }
}