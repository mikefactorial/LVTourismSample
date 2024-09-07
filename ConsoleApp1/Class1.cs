using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
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

    }

    public class UniqueIdField
    {
        public string name { get; set; }
        public bool isSystemMaintained { get; set; }
    }
}
