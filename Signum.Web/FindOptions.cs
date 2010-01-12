﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Engine;

namespace Signum.Web
{
    public class QueryOptions
    {
        public object QueryName { get; set; }

        List<FilterOptions> filterOptions = new List<FilterOptions>();
        public List<FilterOptions> FilterOptions
        {
            get { return filterOptions; }
            set { this.filterOptions = value; }
        }

        public QueryOptions() { }
        public QueryOptions(object queryName)
        {
            this.QueryName = queryName;
        }
    }

    public class FindOptions: QueryOptions
    {
        public FindOptions() { }

        public FindOptions(object queryName)
        {
            this.QueryName = queryName;
        }

        public bool SearchOnLoad { get; set; }
        
        public bool? AllowMultiple { get; set; }
        
        FilterMode filterMode = FilterMode.Visible;
        public FilterMode FilterMode
        {
            get { return filterMode; }
            set { this.filterMode = value; }
        }

        public bool? Create { get; set; }

        public bool? Async { get; set; }

        public string ToString(bool writeQueryUrlName, bool writeAllowMultiple, string firstCharacter)
        {
            StringBuilder sb = new StringBuilder();
            if (writeQueryUrlName)
                sb.Append("&sfQueryUrlName=" + Navigator.Manager.QuerySettings[QueryName].UrlName);

            if (SearchOnLoad)
                sb.Append("&sfSearchOnLoad=true");

            if (Create == false)
                sb.Append("&sfCreate=false");

            if (Async == true)
                sb.Append("$sfAsync=true");

            if (writeAllowMultiple && AllowMultiple.HasValue)
                sb.Append("&sfAllowMultiple="+AllowMultiple.ToString());

            if (FilterOptions != null && FilterOptions.Count > 0)
            {
                for (int i = 0; i < FilterOptions.Count; i++)
                    sb.Append(FilterOptions[i].ToString(i));
            }
            string result = sb.ToString();
            if (result.HasText())
                return firstCharacter + result.RemoveLeft(1);
            else
                return result;
        }
    }

    public class FindUniqueOptions : QueryOptions
    {
        public FindUniqueOptions()
        {
            UniqueType = UniqueType.Single;
        }

        public FindUniqueOptions(object queryName)
        {
            UniqueType = UniqueType.Single;
            QueryName = queryName;
        }

        public UniqueType UniqueType { get; set; }
    }

    public class FilterOptions
    {
        public Column Column { get; set; }
        public string ColumnName { get; set; }
        public bool Frozen { get; set; }
        public FilterOperation Operation { get; set; }
        public object Value { get; set; }

        public FilterOptions(){}

        public FilterOptions(string columnName, object value)
        {
            this.ColumnName = columnName;
            this.Operation = FilterOperation.EqualTo;
            this.Value = value;
        }

        public Filter ToFilter()
        {
            Filter f = new Filter
            {
                Name = Column.Name,
                Type = Column.Type,
                Operation = Operation,
            };
            if (!typeof(Lite).IsAssignableFrom(Value.GetType()) || Value == null)                
                f.Value = Convert(Value, Column.Type);
            else
                f.Value = Lite.Create(Reflector.ExtractLite(Column.Type), Database.Retrieve((Lite)Value));
            return f;
        }

          public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (typeof(Lite).IsAssignableFrom(objType) && type.IsAssignableFrom(((Lite)obj).RuntimeType))
            {
                Lite lite = (Lite)obj;
                return lite.UntypedEntityOrNull ?? Database.RetrieveAndForget(lite);
            }

            if (typeof(Lite).IsAssignableFrom(type))
            {
                Type liteType = Reflector.ExtractLite(type);

                if (typeof(Lite).IsAssignableFrom(objType))
                {
                    Lite lite = (Lite)obj;
                    if (liteType.IsAssignableFrom(lite.RuntimeType))
                    {
                        if (lite.UntypedEntityOrNull != null)
                            return Lite.Create(liteType, lite.UntypedEntityOrNull);
                        else
                            return Lite.Create(liteType, lite.Id, lite.RuntimeType, lite.ToStr);
                    }
                }

                else if (liteType.IsAssignableFrom(objType))
                {
                    return Lite.Create(liteType, (IdentifiableEntity)obj);
                }
            }

            throw new ApplicationException(Properties.Resources.ImposibleConvertObject0From1To2.Formato(obj, objType, type));
        }

        public string ToString(int filterIndex)
        {
            string result = "";
            
            string value = "";
            if (Value != null && typeof(Lite).IsAssignableFrom(Value.GetType()))
            {
                Lite lite = (Lite)Value;
                value = "{0};{1}".Formato(lite.Id.ToString(), lite.RuntimeType.Name);
            }
            else
                value = Value.ToString();

            result = "&cn{0}={1}&sel{0}={2}&val{0}={3}".Formato(filterIndex, ColumnName, Operation.ToString(), value);
            if (Frozen)
                result += "&fz{0}=true".Formato(filterIndex);

            return result;
        }
    }

    public enum FilterMode
    {
        Visible,
        Hidden,
        AlwaysHidden,
    }
}
