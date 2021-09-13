using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DataStorage.Azure;
using RambalacHome.Function.Storage.Models;
using System.Threading;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using EnumerableExtionsions;
using System.IO;
using System.Text;

namespace RambalacHome.Function
{
    public class UrlRedirect
    {
        private static HashSet<string> ignoreId = new HashSet<string>() { "robots.txt", "favicon.ico" };

        private static T GetService<T>(FunctionContext context)
        {
            return (T)context.InstanceServices.GetService(typeof(T));
        }

        private static void SetServices(FunctionContext context)
        {

        }

        [Function("UrlRedirect")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*id}")] HttpRequestData req,
            string id,
            FunctionContext context)
        {
            var log = context.GetLogger<UrlRedirect>();
            var storage = GetService<ITableStorage>(context);
            var http = GetService<HttpClient>(context);
            var settings = GetService<FunctionSettings>(context);
            var cache = GetService<IMemoryCache>(context);
            var cancellation = CancellationToken.None;

            if (id == null || ignoreId.Contains(id))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var host = req.Headers.TryGetValues("Host", out var hosts) ? hosts.FirstOrDefault()?.ToString() : null;
            log.LogInformation($"UrlRedirect for {host} {id} ");

            try
            {
                var ip = req.Headers.TryGetValues("X-Forwarded-For", out var ipval) ? ipval.FirstOrDefault()?.ToString() : "";
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
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                if (record.Fields.Count == 1)
                {
                    logRecord.Country = "Ignore";
                    await storage.InsertAsync(logRecord, cancellation);
                    var response = req.CreateResponse(HttpStatusCode.PermanentRedirect);
                    response.Headers.Add("Location", new[] { record.Fields.First().Value.ToString() });
                    return response;
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
                var newUrl = new UriBuilder(link);
                var newLinkCollection = HttpUtility.ParseQueryString(newUrl.Query);
                var oldLinkCollection = HttpUtility.ParseQueryString(req.Url.Query);
                oldLinkCollection.Add(newLinkCollection);

                newUrl.Query = oldLinkCollection.ToString();

                var newLink = req.CreateResponse(HttpStatusCode.PermanentRedirect);
                newLink.Headers.Add("Location", new[] { newUrl.ToString() });

                return newLink;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Body = new MemoryStream(Encoding.UTF8.GetBytes(ex.Message));
                return response;
            }
        }
    }
}
