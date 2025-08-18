using Meadow;
using Meadow.Foundation.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient.Controllers;

public class RestClientController
{
    // Base URL for the REST API (IP Address:Port)
    string baseUrl = "http://192.168.1.1:5000";

    public async Task<List<string>> GetImageFilenamesAsync()
    {
        var imageFilenames = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync($"{baseUrl}/images/list");
                string json = await response.Content.ReadAsStringAsync();
                var filenames = MicroJson.Deserialize<string[]>(json);
                if (filenames != null)
                {
                    imageFilenames = filenames.Cast<string>().ToList();
                }
            }
            catch (Exception ex)
            {
                Resolver.Log.Error($"Error fetching images: {ex.Message}");
            }

            return imageFilenames;
        }
    }

    public async Task<byte[]> GetImageAsync(string id)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync($"{baseUrl}/images/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    Resolver.Log.Error($"Failed to fetch image {id}: {response.ReasonPhrase}");
                    return Array.Empty<byte>();
                }
            }
            catch (Exception ex)
            {
                Resolver.Log.Error($"Error fetching image {id}: {ex.Message}");
                return Array.Empty<byte>();
            }
        }
    }
}
