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

namespace LVTourism.Plugins.Utilities
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Attributes
    {
        public string Serial_Number { get; set; }
        public string Permit_Number { get; set; }
        public string Restaurant_Name { get; set; }
        public string Location_Name { get; set; }
        public string Category_Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Current_Demerits { get; set; }
        public string Current_Grade { get; set; }
        public string Date_Current { get; set; }
        public string Inspection_Date { get; set; }
        public string Inspection_Time { get; set; }
        public string Employee_ID { get; set; }
        public string Inspection_Type { get; set; }
        public string Inspection_Demerits { get; set; }
        public string Inspection_Grade { get; set; }
        public object Permit_Status { get; set; }
        public string Inspection_Result { get; set; }
        public object Violations { get; set; }
        public string Record_Updated { get; set; }
        public string Location_1 { get; set; }
        public int ObjectId { get; set; }
    }

    public class Feature
    {
        public Attributes attributes { get; set; }
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

    public class RestaurantInspections
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
            client.BaseAddress = new Uri("https://services1.arcgis.com/F1v0ufATbBQScMtY/arcgis/rest/services/Restaurant_Inspections_Open_Data/FeatureServer/0/query");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            context.Trace("URL: " + client.BaseAddress + $"?{query}&outFields=*&outSR=4326&f=json");
            HttpResponseMessage response = client.GetAsync($"?{query}&outFields=*&outSR=4326&f=json").GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var restaurantInspections = JsonConvert.DeserializeObject<RestaurantInspections>(json);
            context.Trace($"Creating records");
            context.Trace($"Found: {restaurantInspections.features.Count}");
            return mapper.CreateEntities(context, restaurantInspections, pageSize, pageNumber);
        }
    }

    public class UniqueIdField
    {
        public string name { get; set; }
        public bool isSystemMaintained { get; set; }
    }


}
