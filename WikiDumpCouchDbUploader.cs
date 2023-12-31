﻿using CouchDbWikipediaArticleUpload.Db;
using CouchDbWikipediaArticleUpload.Helpers;
using CouchDbWikipediaArticleUpload.Models;
using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CouchDbWikipediaArticleUpload
{
    public class WikiDumpCouchDbUploader
    {
        private readonly IConfiguration _configuration;
        private readonly int? _articleUploadLimit;

        private int _currentArticleCount;
        private readonly object _lock = new object();

        public WikiDumpCouchDbUploader(IConfiguration configuration)
        {
            _configuration = configuration;
            _articleUploadLimit = _configuration.GetValue<int?>("ArticleUploadLimit", null);
        }

        public async Task RunAsync()
        {
            var dumpsPath = _configuration.GetValue<string>("DumpDownloadPath") ?? throw new ArgumentNullException("DumpDownloadPath");
            
            Console.WriteLine("----- Start Wikipedia Dump Insert to CouchDB -----");            

            await using var wikiContext = WikiContextBuilder.Build(_configuration);
            var db = await wikiContext.Client.GetOrCreateDatabaseAsync<Article>("wiki_articles");

            string[] dumpFiles = Directory.GetFiles(dumpsPath, "*.bz2");
            var uploadExceptions = new ConcurrentQueue<Exception>();
            ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 3 };
            await Parallel.ForEachAsync(dumpFiles, parallelOptions, async (dumpFile, ct) =>
            {
                try
                {
                    Console.WriteLine($"Starting upload of {dumpFile}");

                    // check if file should be skipped based on article limit
                    if (_articleUploadLimit.HasValue)
                    {
                        lock (_lock)
                        {
                            if (_currentArticleCount >= _articleUploadLimit.Value)
                            {
                                Console.WriteLine($"Skip {dumpFile} article limit has been reached.");
                                return;
                            }
                        }
                    }

                    // process file
                    var pages = ParseDumpFile(dumpFile);
                    foreach (var pageChunk in pages.Chunk(100))
                    {
                        var pageChunkToProcess = pageChunk;

                        // check article limit
                        if (_articleUploadLimit.HasValue)
                        {
                            lock ( _lock)
                            {
                                var chunkSize = pageChunk.Length;
                                if (_currentArticleCount > _articleUploadLimit.Value)
                                    break;
                                if (_currentArticleCount + chunkSize > _articleUploadLimit.Value)
                                {
                                    var pagesUntilLimit = _articleUploadLimit.Value - _currentArticleCount;
                                    pageChunkToProcess = pageChunkToProcess.Take(pagesUntilLimit).ToArray();
                                }

                                _currentArticleCount += pageChunkToProcess.Length;
                            }
                        }

                        await db.AddOrUpdateRangeAsync(pageChunkToProcess);
                    }

                    Console.WriteLine($"Successfully uploaded {dumpFile}");
                }
                catch (Exception ex)
                {
                    uploadExceptions.Enqueue(ex);
                    Console.WriteLine($"Error uploading {dumpFile}: {ex.Message}");
                }
            });
            if (!uploadExceptions.IsEmpty)
                throw new AggregateException(uploadExceptions);

            Console.WriteLine("----- Finished Wikipedia Dump Insert to CouchDB -----");
        }

        private IEnumerable<Article> ParseDumpFile(string dumpFile)
        {
            using var fileStream = new FileStream(dumpFile, FileMode.Open, FileAccess.Read);
            using var stream = new BZip2InputStream(fileStream);
            using var xmlReader = XmlReader.Create(stream);

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlReader.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.mediawiki.org/xml/export-0.10/");

            var xmlPageElements = XmlHelpers.GetElementsByName(xmlReader, "page");
            var xmlPageElementsFiltered = FilterArticles(xmlPageElements, namespaceManager).ToArray();
            return xmlPageElementsFiltered.Select(xmlPageElement => ParseArticle(xmlPageElement, namespaceManager)).ToArray();
        }

        private IEnumerable<XElement> FilterArticles(IEnumerable<XElement> xmlArticles, XmlNamespaceManager namespaceManager)
        {
            // articles have the namespace-key "0"
            // see: https://en.wikipedia.org/wiki/Wikipedia:Namespace
            return xmlArticles
                .Where(xmlArticle => xmlArticle.XPathSelectElement("/ns:ns", namespaceManager)!.Value == "0");
        }

        private Article ParseArticle(XElement xmlArticle, XmlNamespaceManager namespaceManager)
        {
            return new Article
            {
                Title = xmlArticle.XPathSelectElement("/ns:title", namespaceManager)!.Value,
                Keywords = xmlArticle.XPathSelectElement("/ns:title", namespaceManager)!.Value.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray(),
                ArticleId = xmlArticle.XPathSelectElement("/ns:id", namespaceManager)!.Value,
                Versions = new[]
                {
                    new ArticleVersion
                    {
                        Id = xmlArticle.XPathSelectElement("/ns:revision/ns:id", namespaceManager)!.Value,
                        ParentId = xmlArticle.XPathSelectElement("/ns:revision/ns:parentid", namespaceManager)?.Value,
                        TimeStamp = DateTimeOffset.Parse(xmlArticle.XPathSelectElement("/ns:revision/ns:timestamp", namespaceManager)!.Value),
                        Comment = xmlArticle.XPathSelectElement("/ns:revision/ns:comment", namespaceManager)?.Value,
                        Model = xmlArticle.XPathSelectElement("/ns:revision/ns:model", namespaceManager) ?.Value,
                        Format = xmlArticle.XPathSelectElement("/ns:revision/ns:format", namespaceManager) ?.Value,
                        Text = xmlArticle.XPathSelectElement("/ns:revision/ns:text", namespaceManager)!.Value,
                        Contributors = new[]
                        {
                            new Contributor
                            {
                                Id = xmlArticle.XPathSelectElement("/ns:revision/ns:contributor/ns:id", namespaceManager)?.Value,
                                Username = xmlArticle.XPathSelectElement("/ns:revision/ns:contributor/ns:username", namespaceManager)?.Value,
                                Ip = xmlArticle.XPathSelectElement("/ns:revision/ns:contributor/ns:ip", namespaceManager)?.Value
                            }
                        }
                    }
                }
            };
        }
    }
}
