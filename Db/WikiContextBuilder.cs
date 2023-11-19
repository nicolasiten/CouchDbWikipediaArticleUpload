using CouchDB.Driver.Options;
using Microsoft.Extensions.Configuration;

namespace CouchDbWikipediaArticleUpload.Db
{
    public static class WikiContextBuilder
    {
        public static WikiContext Build(IConfiguration configuration)
        {
            var dbEndpoint = configuration.GetValue<string>("DbEndpoint") ?? throw new ArgumentNullException("DbEndpoint");
            var user = configuration.GetValue<string>("DbUser") ?? throw new ArgumentNullException("DbUser");
            var password = configuration.GetValue<string>("DbPassword") ?? throw new ArgumentNullException("DbPassword");

            var optionsBuilder = new CouchOptionsBuilder()
                .UseEndpoint(dbEndpoint)
                .UseBasicAuthentication(user, password);

            return new WikiContext(optionsBuilder.Options);
        }
    }
}
