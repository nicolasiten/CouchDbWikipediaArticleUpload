namespace CouchDbWikipediaArticleUpload.Models
{
    public class ArticleVersion
    {
        public string Id { get; set; } = string.Empty;

        public string? ParentId { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public Contributor[] Contributors { get; set; } = new Contributor[0];

        public string? Comment { get; set; }

        public string? Model { get; set; }

        public string? Format { get; set; }

        public string Text { get; set; } = string.Empty;
    }
}
