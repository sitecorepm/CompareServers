using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.CompareServers.domain
{
    public class FieldComparisonInfo
    {
        public FieldComparisonInfo(TemplateFieldItem tfi)
            : this()
        {
            this.ID = tfi.ID.ToString();
            this.Name = tfi.Name;
        }
        public FieldComparisonInfo()
        {
            this.Differences = new List<FieldDifferenceInfo>();
        }
        public List<FieldDifferenceInfo> Differences { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }

        public void AddDifference(string message)
        {
            AddDifference(message, null, null);
        }
        public void AddDifference(string message, string localValue, string compareValue)
        {
            this.Differences.Add(new FieldDifferenceInfo()
            {
                Message = message,
                LocalValue = localValue,
                CompareValue = compareValue
            });
        }
    }
}