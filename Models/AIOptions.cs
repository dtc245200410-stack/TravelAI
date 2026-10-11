namespace TravelAI.Models;

public sealed class AIOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen3.5:4b";
    public int ResponseTimeoutSeconds { get; set; } = 15;
    public int GenerationTimeoutSeconds { get; set; } = 11;
    public int ContextLength { get; set; } = 4096;
    public int MaxResponseTokens { get; set; } = 320;
    public int HistoryMessages { get; set; } = 8;
    public string KeepAlive { get; set; } = "30m";
    public bool WarmupOnStartup { get; set; } = true;
}
