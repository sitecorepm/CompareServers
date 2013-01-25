using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.CompareServers.domain
{
    public class ItemComparisonInfo
    {
        public ItemComparisonInfo(Item item)
            : this()
        {
            this.ItemPath = item.Paths.Path;
            this.ItemKey = this.ItemPath.ToLower();
        }
        private ItemComparisonInfo()
        {
            this.Status = CompareStatus.Matched;
            this.Messages = new List<string>();
            this.FieldComparisons = new Dictionary<string, FieldComparisonInfo>();
        }

        public string ItemKey { get; set; }
        public string ItemPath { get; set; }
        public CompareStatus Status { get; set; }
        public List<string> Messages { get; set; }

        public Dictionary<string, FieldComparisonInfo> FieldComparisons { get; set; }
    }
}