using DataStorage.Azure;
using DataStorage.Core;

namespace RambalacHome.Function.Storage.Models
{
    [Table("UrlRedirects")]
    public class UrlRecord : DictionaryTableEntity<string>
    {
        public UrlRecord()
            : base()
        {
        }

        public UrlRecord(string urlId)
            : base(urlId, urlId)
        {
        }
    }
}
