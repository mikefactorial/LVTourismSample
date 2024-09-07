﻿using MarkMpn.Sql4Cds.Engine.FetchXml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVTourism.Plugins.Utilities
{
    public class GenericMapper
    {
        protected ILocalPluginContext context { get; set; }
        private EntityMetadata primaryEntityMetadata = null;

        public GenericMapper(ILocalPluginContext context)
        {
            this.context = context;
        }
        public EntityMetadata PrimaryEntityMetadata
        {
            get
            {
                if(primaryEntityMetadata == null)
                {
                    //Create RetrieveEntityRequest
                    RetrieveEntityRequest retrievesEntityRequest = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,
                        LogicalName = context.PluginExecutionContext.PrimaryEntityName
                    };

                    //Execute Request
                    RetrieveEntityResponse retrieveEntityResponse = (RetrieveEntityResponse)context.PluginUserService.Execute(retrievesEntityRequest);
                    primaryEntityMetadata = retrieveEntityResponse.EntityMetadata;
                }

                return primaryEntityMetadata;
            }
        }

        public virtual void MapFetchXml(object fetch)
        {
            if (fetch is condition cond)
            {
                if(!string.IsNullOrEmpty(cond.value))
                {
                    cond.value = MapToVirtualEntityValue(cond.attribute, cond.value).ToString();
                }
                else if (cond.Items.Length > 0)
                {
                    for (int i = 0; i < cond.Items.Length; i++)
                    {
                        context.Trace($"PreConvert: {cond.Items[i].Value}");
                        cond.Items[i].Value = MapToVirtualEntityValue(cond.attribute, cond.Items[i].Value).ToString();
                        context.Trace($"PostConvert: {cond.Items[i].Value}");
                    }
                }
            }

            if (fetch is FetchType ft)
            {
                for (int i = 0; i < ft.Items.Length; i++)
                {
                    object item = ft.Items[i];
                    MapFetchXml(item);
                }
            }
            else if (fetch is FetchEntityType fet)
            {
                for (int i = 0; i < fet.Items.Length; i++)
                {
                    object item = fet.Items[i];
                    MapFetchXml(item);
                }
            }
            else if (fetch is FetchLinkEntityType felt)
            {
                for (int i = 0; i < felt.Items.Length; i++)
                {
                    object item = felt.Items[i];
                    MapFetchXml(item);
                }
            }
            else if (fetch is filter filt)
            {
                for (int i = 0; i < filt.Items.Length; i++)
                {
                    object item = filt.Items[i];
                    MapFetchXml(item);
                }
            }

        }

        public virtual string MapVirtualEntityAttributes(string sql)
        {
            var iEnum = this.GetCustomMappings().GetEnumerator();
            while (iEnum.MoveNext())
            {
                sql = sql.Replace(iEnum.Current.Key, iEnum.Current.Value);
            }

            return sql;
        }

        public virtual EntityCollection CreateEntities(ILocalPluginContext context, ArcGISQueryResults inspections, int pageSize, int pageNumber)
        {
            var collection = new EntityCollection();
            collection.TotalRecordCount = inspections.features.Count;
            collection.MoreRecords = (collection.TotalRecordCount > (pageSize * pageNumber)) || pageSize == -1;
            if(inspections.features.Count > 0)
            {
                var rows = (pageSize > -1) ? inspections.features.Skip(pageSize * (pageNumber - 1)).Take(pageSize) : inspections.features;
                context.Trace($"Creating {rows.Count()} records");
                foreach (var row in rows)
                {
                    Entity entity = new Entity(context.PluginExecutionContext.PrimaryEntityName);
                    foreach(var col in inspections.fields)
                    {
                        context.Trace(col.name);
                        if(col != null && !string.IsNullOrEmpty(col.name) && row != null && row.attributes != null && row.attributes.ContainsKey(col.name) && row.attributes[col.name] != null)
                        {
                            var entityAttribute = this.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == col.name);
                            if(entityAttribute != null)
                            {
                                context.Trace("Setting: " + col.name);
                                entity[entityAttribute.LogicalName] = MapToVirtualEntityValue(entityAttribute, row.attributes[col.name]);
                            }
                        }
                    }
                    collection.Entities.Add(entity);
                }
            }

            return collection;
        }

        public virtual Dictionary<string, string> GetCustomMappings()
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>();

            foreach (var att in PrimaryEntityMetadata.Attributes)
            {
                if(!string.IsNullOrEmpty(att.ExternalName))
                {
                    mappings.Add(att.LogicalName, att.ExternalName);
                }
            }
            mappings.Add(PrimaryEntityMetadata.LogicalName, PrimaryEntityMetadata.ExternalName);

            return mappings;
        }

        public virtual object MapToVirtualEntityValue(string attributeName, object value)
        {
            var att = this.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == attributeName);
            return MapToVirtualEntityValue(att, value);
        }

        public virtual object MapToVirtualEntityValue(AttributeMetadata entityAttribute, object value)
        {
            if (value == null)
            {
                return null;
            }
            else if(entityAttribute.LogicalName == this.PrimaryEntityMetadata.PrimaryIdAttribute && Int32.TryParse(value.ToString(), out int keyInt))
            {
                //This is a generic method of creating a guid from an int value if no guid is available in the database
                return new Guid(keyInt.ToString().PadLeft(32, 'a'));
            }
            else if (entityAttribute is LookupAttributeMetadata lookupAttr && Int32.TryParse(value.ToString(), out int lookupInt))
            {
                var lookup = new EntityReference(lookupAttr.Targets[0], new Guid(lookupInt.ToString().PadLeft(32, 'a')));
                return lookup;
            }
            else if ((entityAttribute is StatusAttributeMetadata || entityAttribute is StateAttributeMetadata || entityAttribute is PicklistAttributeMetadata) && Int32.TryParse(value.ToString(), out int picklistInt))
            {
                return new OptionSetValue(picklistInt);
            }
            else if (Int32.TryParse(value.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("a", string.Empty).Replace("A", string.Empty).Replace("-", string.Empty), out int intValue))
            {
                //This converts the generated guid back to an int. 
                return intValue.ToString();
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
