using CouchDbWikipediaArticleUpload.Helpers;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace CouchDbWikipediaArticleUpload
{
    public class WikiDumpDownloader
    {
        private readonly IConfiguration _configuration;

        public WikiDumpDownloader(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public async Task RunAsync()
        {
            var baseUrl = _configuration["DumpBaseUrl"] ?? throw new ArgumentNullException("DumpBaseUrl");
            var dumpVersion = _configuration["DumpVersionToDownload"] ?? throw new ArgumentNullException("DumpVersionToDownload");
            var downloadPath = _configuration["DumpDownloadPath"] ?? throw new ArgumentNullException("DumpDownloadPath");
            var dumpInformationJson = $@"{baseUrl}/enwiki/{dumpVersion}/dumpstatus.json";

            if (Directory.GetFiles(downloadPath).Any())
            {
                Console.WriteLine("Wikipedia Dump Destination path not empty --> Skip Wikipedia Dump Download");
                return;
            }

            Console.WriteLine("----- Start Wikipedia Dump Download -----");

            var dumpUrls = await GetDumpUrlsAsync(baseUrl, dumpInformationJson);
            await DownloadDumpsAsync(dumpUrls, downloadPath);

            Console.WriteLine("----- Finished Wikipedia Dump Download -----");
        }

        private async Task<string[]> GetDumpUrlsAsync(string baseUrl, string dumpInformationJson)
        {
            var jsonDump = await HttpHelpers.DownloadJsonAsync(dumpInformationJson);
            return jsonDump
                .SelectToken(".jobs.articlesdump.files")
                .Select(f => (string)f.SelectToken("..url"))
                .Where(u => !u.Contains("-index"))
                .Select(u => $"{baseUrl}{u}")
                .Take(3)
                .ToArray();
        }

        private async Task DownloadDumpsAsync(string[] dumpUrls, string destinationPath)
        {
            var downloadExceptions = new ConcurrentQueue<Exception>();
            ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 3 };
            await Parallel.ForEachAsync(dumpUrls, parallelOptions, async (dumpUrl, ct) =>
            {
                try
                {
                    Console.WriteLine($"Start downloading file {dumpUrl}");
                    await HttpHelpers.DownloadFileAsync(dumpUrl, destinationPath, ct);
                    Console.WriteLine($"Finished downloading file {dumpUrl}");
                }
                catch (Exception ex)
                {
                    downloadExceptions.Enqueue(ex);
                    Console.WriteLine($"Error downloading {dumpUrl}: {ex.Message}");
                }
            });
            if (!downloadExceptions.IsEmpty)
                throw new AggregateException(downloadExceptions);
        }
    }
}
