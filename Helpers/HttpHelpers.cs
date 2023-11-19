using Newtonsoft.Json.Linq;
using System.Net;

namespace CouchDbWikipediaArticleUpload.Helpers
{
    public static class HttpHelpers
    {
        public static async Task<JObject> DownloadJsonAsync(string url)
        {
            using var httpClient = new HttpClient();
            var httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            var json = await httpResponse.Content.ReadAsStringAsync();

            return JObject.Parse(json);
        }

        public static async Task DownloadFileAsync(string url, string savePath, CancellationToken ct)
        {
            using var httpClient = new HttpClient();
            var saveFilePath = Path.Combine(savePath, Path.GetFileName(url));
            using var destinationFileStream = new FileStream(saveFilePath, FileMode.Create);

            using var downloadedFileStream = await httpClient.GetStreamAsync(url, ct);
            await downloadedFileStream.CopyToAsync(destinationFileStream);
        }
    }
}
