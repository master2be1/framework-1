﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryResult
    {
        public List<Column> Columns { get; set; }
        public object[][] Data{get;set;}

        public List<Column> VisibleColums
        {
            get
            {
                return Columns.Where(c => c.Visible).ToList();
            }
        } 

        public DataTable ToDataTable()
        {
            DataTable dt = new DataTable("Tabla");
            dt.Columns.AddRange(Columns.Select(c => new DataColumn(c.Name, c.Type)).ToArray());
            Data.ForEach(arr => dt.Rows.Add(arr));
            return dt; 
        }
    }


    [Serializable]
    public class QueryDescription
    {
        public List<Column> Columns { get; set; }
    }

    [Serializable]
    public class Column
    {
        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string DisplayName { get; set; }
        public bool Filterable { get; set; }
        public bool Visible { get; set; }

        [field:NonSerialized]
        internal Meta Meta{ get; set; }

        public PropertyInfo TwinProperty { get; set; }

        public string Format { get; set; }

        public const string Entity = "Entity";
        public bool IsEntity
        {
            get { return this.Name == Entity;  }
        }

        public Column(MemberInfo mi, Meta meta)
        {
            Name = mi.Name;
            Type = mi.ReturningType();
            Meta = meta;

            if (typeof(IdentifiableEntity).IsInstanceOfType(Type))
                Debug.Write("{0} column returns subtype of IdentifiableEntity, use a Lazy instead!!".Formato(mi.MemberName()));

            TwinProperty = (meta as CleanMeta).TryCC(cm => (PropertyInfo)cm.Member);

            if (TwinProperty != null && mi.Name == TwinProperty.Name)
            {
                DisplayName = TwinProperty.NiceName();
                Format = Reflector.FormatString(TwinProperty);
            }
            else
            {
                DisplayName = Name
                   .Replace("_nf_", "")
                   .Replace("_nv_", "")
                   .Replace("_p_", ".")
                   .Replace("_", " ");
                Format = Reflector.FormatString(Type); 
            }

            if (IsEntity)
            {
                Visible = false;
                Filterable = false;
            }
            else
            {
                Visible = !mi.Name.Contains("_nv_") && !IsEntity;
                Filterable = !mi.Name.Contains("_nf_") && !IsEntity;
            }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(Type.TypeName(), Name); ;
        }
    }
}