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
    string baseUrl = "http://192.168.50.226:5000";

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
}