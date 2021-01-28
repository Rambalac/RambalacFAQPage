using DataStorage.Azure;
using DataStorage.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace RambalacHome.Function.Storage.Models
{
    [Table("UrlRedirects")]
    public class UrlRecord : DictionaryTableEntity
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
