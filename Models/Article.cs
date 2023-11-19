using CouchDB.Driver.Types;

namespace CouchDbWikipediaArticleUpload.Models
{
    public class Article : CouchDocument
    {
        public string Title { get; set; } = string.Empty;

        public string[] Keywords { get; set; } = new string[0];

        public string ArticleId { get; set; } = string.Empty;

        public ArticleVersion[] Versions { get; set; } = new ArticleVersion[0];
    }
}
