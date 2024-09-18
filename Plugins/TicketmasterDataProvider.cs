using LVTourism.Plugins.Utilities;
using MarkMpn.Sql4Cds.Engine;
using MarkMpn.Sql4Cds.Engine.FetchXml;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Metadata;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace LVTourism.Plugins
{
    /// <summary>
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public class TicketmasterDataProvider : PluginBase
    {
        public TicketmasterDataProvider(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(TicketmasterDataProvider))
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
                var id = mapper.MapToVirtualEntityValue(mapper.PrimaryEntityMetadata.PrimaryIdAttribute, context.PluginExecutionContext.PrimaryEntityId);
                Dictionary<string, object> queryParameters = new Dictionary<string, object>();
                queryParameters["id"] = id;
                var entities = EventSearchResults.GetEventsFromTicketmaster(context, mapper, queryParameters, 1, 1);
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
                QueryExpression qe = query as QueryExpression;
                if (qe == null && query is FetchExpression fe)
                {
                    FetchXmlToQueryExpressionRequest fetchXmlToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest()
                    {
                        FetchXml = fe.Query
                    };
                    var fetchXmlToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)context.PluginUserService.Execute(fetchXmlToQueryExpressionRequest);
                    qe = fetchXmlToQueryExpressionResponse.Query;
                }
                var fieldsAndValues = new Dictionary<string, object>();
                this.GetFieldsAndValues(qe.Criteria, fieldsAndValues);
                //Store page info before converting
                int page = (qe.PageInfo != null) ? qe.PageInfo.PageNumber - 1 : -1;
                int count = (qe.PageInfo != null) ? -1 : qe.PageInfo.Count;

                context.Trace($"Calling GetEventsFromTicketmaster");
                collection = EventSearchResults.GetEventsFromTicketmaster(context, mapper, fieldsAndValues, count, page);
                context.Trace($"Records Returned: {collection.Entities.Count}");
                context.PluginExecutionContext.OutputParameters["BusinessEntityCollection"] = collection;
            }
        }

        protected void GetFieldsAndValues(FilterExpression fex, Dictionary<string, object> fieldsAndValues)
        {
            if (fex.Conditions != null)
            {
                foreach (var ce in fex.Conditions)
                {
                    if (ce.Values != null && ce.Values.Count > 0 && ce.Values[0] != null)
                    {
                        fieldsAndValues[ce.AttributeName] = ce.Values[0];
                    }
                }
            }

            if (fex.Filters != null)
            {
                foreach (var fe in fex.Filters)
                {
                    this.GetFieldsAndValues(fe, fieldsAndValues);
                }
            }
        }
    }
}
