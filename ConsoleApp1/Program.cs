// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

Console.WriteLine("Hello, World!");
var client = new HttpClient();
client.BaseAddress = new Uri("https://services1.arcgis.com/F1v0ufATbBQScMtY/arcgis/rest/services/Restaurant_Inspection_Violation_Codes/FeatureServer/0/query");
client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
HttpResponseMessage response = client.GetAsync($"?where=1=1&outFields=*&outSR=4326&f=json").GetAwaiter().GetResult();
var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
var restaurantInspections = JsonConvert.DeserializeObject<ArcGISQueryResults>(json);
var test = restaurantInspections;
