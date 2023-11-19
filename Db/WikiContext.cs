using CouchDB.Driver;
using CouchDB.Driver.Options;
using Microsoft.Extensions.Configuration;

namespace CouchDbWikipediaArticleUpload.Db
{
    public class WikiContext : CouchContext
    {
        public WikiContext(CouchOptions options) : base(options)
        { }
    }
}
