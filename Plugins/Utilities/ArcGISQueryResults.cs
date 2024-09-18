using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace LVTourism.Plugins.Utilities
{
    public class Feature
    {
        public JObject attributes { get; set; }
    }

    public class Field
    {
        public string name { get; set; }
        public string type { get; set; }
        public string alias { get; set; }
        public string sqlType { get; set; }
        public object length { get; set; }
        public object domain { get; set; }
        public object defaultValue { get; set; }
    }

    public class ArcGISQueryResults
    {
        public string objectIdFieldName { get; set; }
        public UniqueIdField uniqueIdField { get; set; }
        public string globalIdFieldName { get; set; }
        public List<Field> fields { get; set; }
        public List<Feature> features { get; set; }

        public static EntityCollection GetDataFromArcGIS(ILocalPluginContext context, GenericMapper mapper, string query, int pageSize, int pageNumber)
        {
            context.Trace($"Query: {query}");
            HttpClient client = new HttpClient();

            var retrieverService = (IEntityDataSourceRetrieverService)context.ServiceProvider.GetService(typeof(IEntityDataSourceRetrieverService));
            var sourceEntity = retrieverService.RetrieveEntityDataSource();
            var baseQueryAddress = sourceEntity["exlnts_arcgisbaseurl"].ToString();

            client.BaseAddress = new Uri(baseQueryAddress);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            context.Trace("URL: " + client.BaseAddress + $"?{query}&outFields=*&outSR=4326&f=json");
            HttpResponseMessage response = client.GetAsync($"?{query}&outFields=*&outSR=4326&f=json").GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var restaurantInspections = JsonConvert.DeserializeObject<ArcGISQueryResults>(json);
            context.Trace($"Creating records");
            context.Trace($"Found: {restaurantInspections.features.Count}");
            return CreateEntities(context, mapper, restaurantInspections, pageSize, pageNumber);
        }

        public static EntityCollection CreateEntities(ILocalPluginContext context, GenericMapper mapper, ArcGISQueryResults inspections, int pageSize, int pageNumber)
        {
            var collection = new EntityCollection();
            collection.TotalRecordCount = inspections.features.Count;
            collection.MoreRecords = (collection.TotalRecordCount > (pageSize * pageNumber)) || pageSize == -1;
            if (inspections.features.Count > 0)
            {
                var rows = (pageSize > -1) ? inspections.features.Skip(pageSize * (pageNumber - 1)).Take(pageSize) : inspections.features;
                context.Trace($"Creating {rows.Count()} records");
                foreach (var row in rows)
                {
                    Entity entity = new Entity(context.PluginExecutionContext.PrimaryEntityName);
                    foreach (var col in inspections.fields)
                    {
                        context.Trace(col.name);
                        if (col != null && !string.IsNullOrEmpty(col.name) && row != null && row.attributes != null && row.attributes.ContainsKey(col.name) && row.attributes[col.name] != null)
                        {
                            var entityAttribute = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == col.name);
                            if (entityAttribute != null)
                            {
                                context.Trace("Setting: " + col.name);
                                entity[entityAttribute.LogicalName] = mapper.MapToVirtualEntityValue(entityAttribute, row.attributes[col.name]);
                            }
                        }
                    }
                    collection.Entities.Add(entity);
                }
            }

            return collection;
        }
    }

    public class UniqueIdField
    {
        public string name { get; set; }
        public bool isSystemMaintained { get; set; }
    }


}
