using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DataStorage.Azure;
using RambalacHome.Function.Storage.Models;
using System.Threading;
using System.Net.Http;
using System.Text.Json;
using System.Web.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using EnumerableExtionsions;

namespace RambalacHome.Function
{
    public class UrlRedirect
    {
        private readonly ITableStorage storage;
        private readonly HttpClient http;
        private readonly FunctionSettings settings;
        private readonly IMemoryCache cache;
        private readonly HashSet<string> ignoreId = new HashSet<string>() { "robots.txt", "favicon.ico" };

        public UrlRedirect(ITableStorage storage, HttpClient http, FunctionSettings settings, IMemoryCache cache)
        {
            this.storage = storage;
            this.http = http;
            this.settings = settings;
            this.cache = cache;
        }

        [FunctionName("UrlRedirect")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*id}")] HttpRequest req,
            string id,
            ILogger log,
            CancellationToken cancellation)
        {
            if (id == null||ignoreId.Contains(id))
            {
                return new NotFoundResult();
            }

            var host = req.Headers["Host"].ToString();
            log.LogInformation($"UrlRedirect for {host} {id} ");

            try
            {
                var ip = req.Headers.TryGetValue("X-Forwarded-For", out var ipval) ? ipval.ToString() : req.HttpContext.Connection.RemoteIpAddress.ToString();
                ip = ip.Split(":")[0];

                var logRecord = new UrlRedirectLog(ip, host, "ID N/A", id);

                var record = await cache.GetOrCreateAsync($"url_{host}_{id}", async entity =>
                  {
                      entity.SetSize(0);
                      return await storage.RetrieveAsync<UrlRecord>(host, id, cancellation);
                  });

                if (record == null)
                {
                    log.LogWarning($"Not found |{host}/{id}|");
                    await storage.InsertAsync(logRecord, cancellation);
                    return new NotFoundResult();
                }

                if (record.Fields.Count==1)
                {
                    logRecord.Country = "Ignore";
                    await storage.InsertAsync(logRecord, cancellation);
                    return new RedirectResult(record.Fields.First().Value.ToString(), true);
                }

                var country = await cache.GetOrCreateAsync("ip_" + ip, async entity =>
                {
                    entity.SetSize(1);
                    var response = await http.GetStringAsync($"https://atlas.microsoft.com/geolocation/ip/json?subscription-key={settings.AzureMapsApiKey}&api-version=1.0&ip={ip}");
                    var model = JsonSerializer.Deserialize<IpGeocode>(response);
                    return model?.CountryRegion?.IsoCode;
                });
                logRecord.Country = country ?? "C N/A";

                await storage.InsertAsync(logRecord, cancellation);

                var link = record.Fields.GetFirstOf(new[] { country, "default", "US" }) ?? record.Fields.First().Value;
                return new RedirectResult(link.ToString(), true);
            }
            catch (Exception ex)
            {
                return new ContentResult()
                {
                    Content = ex.Message,
                    StatusCode = 500,
                };
            }
        }
    }
}
