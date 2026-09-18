using FieldStack;
using FieldStack.Serialization;
using System.Net.Http;

namespace NetFrames.YoshiPiClient.Controllers;

public class RestClientController
{
    private readonly string baseUrl;

    public RestClientController(string baseUrl)
    {
        this.baseUrl = baseUrl;

        Resolver.Log.Info($"NETFRAMES: Using base URL: {baseUrl}");
    }

    public async Task<List<string>> GetImageFilenamesAsync()
    {
        var imageFilenames = new List<string>();

        using var client = new HttpClient();

        try
        {
            var response = await client.GetAsync($"{baseUrl}/images/list");
            var json = await response.Content.ReadAsStringAsync();
            var filenames = MicroJson.Deserialize<string[]>(json);
            if (filenames != null)
            {
                imageFilenames = filenames.ToList();
            }
        }
        catch (Exception ex)
        {
            Resolver.Log.Error($"Error fetching images: {ex.Message}");
        }

        return imageFilenames;
    }

    public async Task<byte[]> GetImageAsync(string id, int? width, int? height)
    {
        using var client = new HttpClient();

        try
        {
            var response = await client.GetAsync($"{baseUrl}/images/{id}?width={width}&height={height}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            Resolver.Log.Error($"Failed to fetch image {id}: {response.ReasonPhrase}");
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            Resolver.Log.Error($"Error fetching image {id}: {ex.Message}");
            return Array.Empty<byte>();
        }
    }
}
