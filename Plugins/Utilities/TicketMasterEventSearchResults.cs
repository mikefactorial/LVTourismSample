using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LVTourism.Plugins.Utilities
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    public class EventSearchResults
    {
        public Embedded _embedded { get; set; }
        public Links _links { get; set; }
        public Page page { get; set; }

        public static EntityCollection GetEventsFromTicketmaster(ILocalPluginContext context, GenericMapper mapper, Dictionary<string, object> queryParameters, int pageSize, int pageNumber)
        {
            var retrieverService = (IEntityDataSourceRetrieverService)context.ServiceProvider.GetService(typeof(IEntityDataSourceRetrieverService));
            var sourceEntity = retrieverService.RetrieveEntityDataSource();
            var baseQueryAddress = sourceEntity["exlnts_ticketmasterbaseurl"].ToString();
            var city = sourceEntity["exlnts_searchcity"].ToString();
            var apiKey = sourceEntity["exlnts_ticketmasterapikey"].ToString();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException("exlnts_ticketmasterapikey");
            }
            var url = new StringBuilder($"?apikey={apiKey}");
            var fieldMappings = GetExternalInternalMappings(mapper);

            if (queryParameters.ContainsKey(fieldMappings["id"]))
            {
                url.Append($"&id={queryParameters[fieldMappings["id"]]}");
            }
            else
            {
                if (queryParameters.ContainsKey(fieldMappings["name"]))
                {
                    url.Append($"&keyword={queryParameters[fieldMappings["name"]]}");
                }
                else if (queryParameters.ContainsKey(fieldMappings["url"]))
                {
                    url.Append($"&keyword={queryParameters[fieldMappings["url"]]}");
                }
                else if (queryParameters.ContainsKey(fieldMappings["image"]))
                {
                    url.Append($"&keyword={queryParameters[fieldMappings["image"]]}");
                }
                if (queryParameters.ContainsKey(fieldMappings["start"]))
                {
                    if (DateTime.TryParse(queryParameters[fieldMappings["start"]].ToString(), out DateTime date))
                    {
                        url.Append($"&startDateTime={date.ToString("s") + "Z"}");
                        url.Append($"&endDateTime={date.AddDays(7).ToString("s") + "Z"}");
                    }
                }
                else
                {
                    url.Append($"&startDateTime={DateTime.UtcNow.AddDays(-1).ToString("s") + "Z"}");
                }
                if (!string.IsNullOrEmpty(city))
                {
                    url.Append($"&city={city}");
                }
                if (pageNumber >= 0)
                {
                    url.Append($"&page={pageNumber}");
                }
                if (pageSize > 0)
                {
                    url.Append($"&size={pageSize}");
                }
                url.Append("&sort=date,asc");
            }
            var client = new HttpClient();
            client.BaseAddress = new Uri(baseQueryAddress);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(url.ToString()).GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var results = JsonConvert.DeserializeObject<EventSearchResults>(json);
            context.Trace($"Creating records");
            return CreateEntities(context, mapper, results, pageSize, pageNumber);

        }

        public static EntityCollection CreateEntities(ILocalPluginContext context, GenericMapper mapper, EventSearchResults eventResults, int pageSize, int pageNumber)
        {
            var collection = new EntityCollection();
            if (eventResults != null)
            {
                if (eventResults.page != null)
                {
                    collection.TotalRecordCount = eventResults.page.totalElements;
                    collection.MoreRecords = (collection.TotalRecordCount > (pageSize * pageNumber)) || pageSize == -1;
                }
                if (eventResults._embedded != null && eventResults._embedded.events != null && eventResults._embedded.events.Count > 0)
                {
                    foreach (var row in eventResults._embedded.events)
                    {
                        context.Trace($"Creating record for {row.name}");
                        Entity entity = new Entity(context.PluginExecutionContext.PrimaryEntityName);
                        //Build these up manually for now
                        if (!string.IsNullOrEmpty(row.id))
                        {
                            context.Trace("Converting ID");
                            var id = mapper.ConvertStringToGuid(row.id);
                            if (id != Guid.Empty)
                            {
                                context.Trace("Getting mappings");
                                var mappings = GetExternalInternalMappings(mapper);
                                context.Trace("Setting row columns");
                                entity[mappings["id"]] = id;
                                entity[mappings["name"]] = row.name;
                                entity[mappings["url"]] = row.url;
                                if (row.dates != null && row.dates.start != null && row.dates.start.dateTime != null)
                                {
                                    entity[mappings["start"]] = row.dates.start.dateTime;
                                }
                                entity[mappings["image"]] = row.images.FirstOrDefault()?.url;
                                collection.Entities.Add(entity);
                                context.Trace("Record created");
                            }
                        }
                    }
                }

            }
            return collection;
        }

        public static Dictionary<string, string> GetExternalInternalMappings(GenericMapper mapper)
        {
            var mappings = new Dictionary<string, string>();

            mappings["id"] = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == "id").LogicalName;
            mappings["name"] = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == "name").LogicalName;
            mappings["url"] = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == "url").LogicalName;
            mappings["start"] = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == "start").LogicalName;
            mappings["image"] = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == "image").LogicalName;
            return mappings;
        }
    }


    public class Accessibility
    {
        public int ticketLimit { get; set; }
        public string id { get; set; }
        public string info { get; set; }
    }

    public class Ada
    {
        public string adaPhones { get; set; }
        public string adaCustomCopy { get; set; }
        public string adaHours { get; set; }
    }

    public class Address
    {
        public string line1 { get; set; }
    }

    public class AgeRestrictions
    {
        public bool legalAgeEnforced { get; set; }
        public string id { get; set; }
    }

    public class AllInclusivePricing
    {
        public bool enabled { get; set; }
    }

    public class Attraction
    {
        public string href { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public bool test { get; set; }
        public string url { get; set; }
        public string locale { get; set; }
        public ExternalLinks externalLinks { get; set; }
        public List<string> aliases { get; set; }
        public List<Image> images { get; set; }
        public List<Classification> classifications { get; set; }
        public UpcomingEvents upcomingEvents { get; set; }
        public Links _links { get; set; }
    }

    public class BoxOfficeInfo
    {
        public string phoneNumberDetail { get; set; }
        public string openHoursDetail { get; set; }
        public string acceptedPaymentDetail { get; set; }
        public string willCallDetail { get; set; }
    }

    public class City
    {
        public string name { get; set; }
    }

    public class Classification
    {
        public bool primary { get; set; }
        public Segment segment { get; set; }
        public Genre genre { get; set; }
        public SubGenre subGenre { get; set; }
        public Type type { get; set; }
        public SubType subType { get; set; }
        public bool family { get; set; }
    }

    public class Country
    {
        public string name { get; set; }
        public string countryCode { get; set; }
    }

    public class Dates
    {
        public Start start { get; set; }
        public string timezone { get; set; }
        public Status status { get; set; }
        public bool spanMultipleDays { get; set; }
    }

    public class Dma
    {
        public int id { get; set; }
    }

    public class Embedded
    {
        public List<Event> events { get; set; }
        public List<Venue> venues { get; set; }
        public List<Attraction> attractions { get; set; }
    }

    public class Event
    {
        public string name { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public bool test { get; set; }
        public string url { get; set; }
        public string locale { get; set; }
        public List<Image> images { get; set; }
        public Sales sales { get; set; }
        public Dates dates { get; set; }
        public List<Classification> classifications { get; set; }
        public Promoter promoter { get; set; }
        public List<Promoter> promoters { get; set; }
        public List<PriceRange> priceRanges { get; set; }
        public List<Product> products { get; set; }
        public Seatmap seatmap { get; set; }
        public Accessibility accessibility { get; set; }
        public TicketLimit ticketLimit { get; set; }
        public AgeRestrictions ageRestrictions { get; set; }
        public Ticketing ticketing { get; set; }
        public Links _links { get; set; }
        public Embedded _embedded { get; set; }
        public string info { get; set; }
        public string pleaseNote { get; set; }
    }

    public class ExternalLinks
    {
        public List<Twitter> twitter { get; set; }
        public List<Facebook> facebook { get; set; }
        public List<Wiki> wiki { get; set; }
        public List<Instagram> instagram { get; set; }
        public List<Homepage> homepage { get; set; }
    }

    public class Facebook
    {
        public string url { get; set; }
    }

    public class First
    {
        public string href { get; set; }
    }

    public class GeneralInfo
    {
        public string generalRule { get; set; }
        public string childRule { get; set; }
    }

    public class Genre
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Homepage
    {
        public string url { get; set; }
    }

    public class Image
    {
        public string ratio { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public bool fallback { get; set; }
    }

    public class Instagram
    {
        public string url { get; set; }
    }

    public class Last
    {
        public string href { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
        public List<Attraction> attractions { get; set; }
        public List<Venue> venues { get; set; }
        public First first { get; set; }
        public Next next { get; set; }
        public Last last { get; set; }
    }

    public class Location
    {
        public string longitude { get; set; }
        public string latitude { get; set; }
    }

    public class Market
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Next
    {
        public string href { get; set; }
    }

    public class Page
    {
        public int size { get; set; }
        public int totalElements { get; set; }
        public int totalPages { get; set; }
        public int number { get; set; }
    }

    public class Presale
    {
        public DateTime startDateTime { get; set; }
        public DateTime endDateTime { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class PriceRange
    {
        public string type { get; set; }
        public string currency { get; set; }
        public double min { get; set; }
        public double max { get; set; }
    }

    public class Product
    {
        public string name { get; set; }
        public string id { get; set; }
        public string url { get; set; }
        public string type { get; set; }
        public List<Classification> classifications { get; set; }
    }

    public class Promoter
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class Promoter2
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class Public
    {
        public DateTime startDateTime { get; set; }
        public bool startTBD { get; set; }
        public bool startTBA { get; set; }
        public DateTime endDateTime { get; set; }
    }

    public class SafeTix
    {
        public bool enabled { get; set; }
        public bool inAppOnlyEnabled { get; set; }
    }

    public class Sales
    {
        public Public @public { get; set; }
        public List<Presale> presales { get; set; }
    }

    public class Seatmap
    {
        public string staticUrl { get; set; }
        public string id { get; set; }
    }

    public class Segment
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Social
    {
        public Twitter twitter { get; set; }
    }

    public class Start
    {
        public string localDate { get; set; }
        public string localTime { get; set; }
        public DateTime dateTime { get; set; }
        public bool dateTBD { get; set; }
        public bool dateTBA { get; set; }
        public bool timeTBA { get; set; }
        public bool noSpecificTime { get; set; }
    }

    public class State
    {
        public string name { get; set; }
        public string stateCode { get; set; }
    }

    public class Status
    {
        public string code { get; set; }
    }

    public class SubGenre
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class SubType
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Ticketing
    {
        public SafeTix safeTix { get; set; }
        public AllInclusivePricing allInclusivePricing { get; set; }
        public string id { get; set; }
    }

    public class TicketLimit
    {
        public string info { get; set; }
        public string id { get; set; }
    }

    public class Twitter
    {
        public string handle { get; set; }
        public string url { get; set; }
    }

    public class Type
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class UpcomingEvents
    {
        public int archtics { get; set; }
        public int ticketmaster { get; set; }
        public int _total { get; set; }
        public int _filtered { get; set; }
        public int? tmr { get; set; }
        public int? universe { get; set; }
    }

    public class Venue
    {
        public string href { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public bool test { get; set; }
        public string url { get; set; }
        public string locale { get; set; }
        public List<Image> images { get; set; }
        public string postalCode { get; set; }
        public string timezone { get; set; }
        public City city { get; set; }
        public State state { get; set; }
        public Country country { get; set; }
        public Address address { get; set; }
        public Location location { get; set; }
        public List<Market> markets { get; set; }
        public List<Dma> dmas { get; set; }
        public UpcomingEvents upcomingEvents { get; set; }
        public Links _links { get; set; }
        public BoxOfficeInfo boxOfficeInfo { get; set; }
        public string parkingDetail { get; set; }
        public string accessibleSeatingDetail { get; set; }
        public GeneralInfo generalInfo { get; set; }
        public List<string> aliases { get; set; }
        public Social social { get; set; }
        public Ada ada { get; set; }
    }

    public class Wiki
    {
        public string url { get; set; }
    }


}
