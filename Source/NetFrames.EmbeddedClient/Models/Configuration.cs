namespace NetFrames.EmbeddedClient.Models;

public class Configuration
{
    public int slideshowIntervalSeconds { get; set; } = 10;

    public string slideshowOrder { get; set; } = "sequential"; // or "random"

    public string apiVersion { get; set; } = "v1";
}
