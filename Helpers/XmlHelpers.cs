using System.Xml.Linq;
using System.Xml;

namespace CouchDbWikipediaArticleUpload.Helpers
{
    public static class XmlHelpers
    {
        public static IEnumerable<XElement> GetElementsByName(XmlReader reader, string elementName)
        {
            reader.MoveToContent();
            reader.Read();

            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
                {
                    var matchedElement = XNode.ReadFrom(reader) as XElement;
                    if (matchedElement != null)
                        yield return matchedElement;
                }
                else
                {
                    reader.Read();
                }
            }
        }
    }
}
