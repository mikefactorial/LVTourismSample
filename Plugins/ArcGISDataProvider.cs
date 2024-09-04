using LVTourism.Plugins.Utilities;
using MarkMpn.Sql4Cds.Engine;
using MarkMpn.Sql4Cds.Engine.FetchXml;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace LVTourism.Plugins
{
    /// <summary>
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public class ArcGISDataProvider : PluginBase
    {
        public ArcGISDataProvider(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ArcGISDataProvider))
        {
            // TODO: Implement your custom configuration handling
            // https://docs.microsoft.com/powerapps/developer/common-data-service/register-plug-in#set-configuration-data
        }
        // Entry point for custom business logic execution
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            localPluginContext.Trace(localPluginContext.PluginExecutionContext.MessageName);
            switch (localPluginContext.PluginExecutionContext.MessageName)
            {
                case "Retrieve":
                    ExecuteRetrieve(localPluginContext);
                    break;
                case "RetrieveMultiple":
                    ExecuteRetrieveMultiple(localPluginContext);
                    break;
                default:
                    throw new NotImplementedException($"The message: {localPluginContext.PluginExecutionContext.MessageName} is not supported");
            }
        }

        protected virtual void ExecuteRetrieve(ILocalPluginContext context)
        {
            var mapper = new GenericMapper(context);
            Entity entity = new Entity(context.PluginExecutionContext.PrimaryEntityName);

            if (mapper != null)
            {
                string query = $"where={mapper.PrimaryEntityMetadata.PrimaryIdAttribute} = '{mapper.MapToVirtualEntityValue(mapper.PrimaryEntityMetadata.PrimaryIdAttribute, context.PluginExecutionContext.PrimaryEntityId)}'";
                query = mapper.MapVirtualEntityAttributes(query);

                var entities = RestaurantInspections.GetDataFromArcGIS(context, mapper, query, 1, 1);
                if (entities.Entities != null && entities.Entities.Count > 0)
                {
                    entity = entities.Entities[0];
                }
            }

            // Set output parameter
            context.PluginExecutionContext.OutputParameters["BusinessEntity"] = entity;
        }

        protected virtual void ExecuteRetrieveMultiple(ILocalPluginContext context)
        {
            var query = context.PluginExecutionContext.InputParameters["Query"];
            if (query != null)
            {
                var mapper = new GenericMapper(context);

                EntityCollection collection = new EntityCollection();
                string fetchXml = string.Empty;
                if (query is QueryExpression qe)
                {
                    var convertRequest = new QueryExpressionToFetchXmlRequest();
                    convertRequest.Query = (QueryExpression)qe;
                    var response = (QueryExpressionToFetchXmlResponse)context.PluginUserService.Execute(convertRequest);
                    fetchXml = response.FetchXml;
                }
                else if (query is FetchExpression fe)
                {
                    fetchXml = fe.Query;
                }
                if (!string.IsNullOrEmpty(fetchXml))
                {
                    context.Trace($"Pre FetchXML: {fetchXml}");

                    var metadata = new AttributeMetadataCache(context.PluginUserService);
                    var fetch = Deserialize(fetchXml);
                    mapper.MapFetchXml(fetch);

                    //Store page info before converting
                    int page = -1;
                    int count = -1;
                    if (!string.IsNullOrEmpty(fetch.page))
                    {
                        page = Int32.Parse(fetch.page);
                        fetch.page = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(fetch.count))
                    {
                        count = Int32.Parse(fetch.count);
                        fetch.count = string.Empty;
                    }

                    var arcQuery = FetchXml2Sql.Convert(context.PluginUserService, metadata, fetch, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
                    //Get the where clause
                    string pattern = @"\bWHERE\b.*?(?=\bORDER\b|\bGROUP\b|$)";
                    Match match = Regex.Match(arcQuery, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // Check if a match is found
                    if (match.Success)
                    {
                        arcQuery = match.Value.Replace("WHERE", "").Trim();
                    }
                    else
                    {
                        arcQuery = "1=1";
                    }

                    arcQuery = mapper.MapVirtualEntityAttributes($"where={arcQuery}");
                    context.Trace($"Query: {arcQuery}");

                    if (page != -1 && count != -1)
                    {
                        collection = RestaurantInspections.GetDataFromArcGIS(context, mapper, arcQuery, count, page);
                    }
                    else
                    {
                        collection = RestaurantInspections.GetDataFromArcGIS(context, mapper, arcQuery, -1, 1);
                    }
                }
                context.Trace($"Records Returned: {collection.Entities.Count}");
                context.PluginExecutionContext.OutputParameters["BusinessEntityCollection"] = collection;
            }
        }



        /// <summary>
        /// Deserializes the fetch XML.
        /// </summary>
        /// <param name="fetchXml">The fetch XML.</param>
        /// <returns>Fetch Object for the FetchXML string</returns>
        private static FetchType Deserialize(string fetchXml)
        {
            var serializer = new XmlSerializer(typeof(FetchType));
            object result;
            using (TextReader reader = new StringReader(fetchXml))
            {
                result = serializer.Deserialize(reader);
            }

            return result as FetchType;
        }
    }
}
