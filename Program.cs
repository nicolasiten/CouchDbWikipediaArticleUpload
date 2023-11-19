using CouchDbWikipediaArticleUpload;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

await new WikiDumpDownloader(configuration).RunAsync();
await new WikiDumpCouchDbUploader(configuration).RunAsync();