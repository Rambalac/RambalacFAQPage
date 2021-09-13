using DataStorage.Core;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Text;

namespace RambalacHome.Function.Storage.Models
{
    [Table("UrlRedirectLogs")]
    public class UrlRedirectLog : TableEntity
    {
        public UrlRedirectLog()
            : base()
        {
        }

        public UrlRedirectLog(string ip, string host, string country, string id)
            : base((1000000000000000000L - DateTime.UtcNow.Ticks).ToString("0000000000000000000"), Convert.ToBase64String(Encoding.ASCII.GetBytes(ip)))
        {
            Host = host;
            Ip = ip;
            Id = id;
            Country = country;
        }

        public string Host { get; set; }

        public string Ip { get; set; }

        public string Country { get; set; }
        
        public string Id { get; set; }
    }
}
