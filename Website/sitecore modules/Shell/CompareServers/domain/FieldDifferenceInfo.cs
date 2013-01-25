using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DiffMatchPatch;

namespace Sitecore.SharedSource.CompareServers.domain
{
    public class FieldDifferenceInfo
    {
        public string Message { get; set; }
        public string LocalValue { get; set; }
        public string CompareValue { get; set; }
        public string Diff
        {
            get
            {
                return DiffToHtml(LocalValue, CompareValue);
            }
        }

        private string DiffToHtml(string s1, string s2)
        {
            var dmp = new diff_match_patch();
            var diffs = dmp.diff_main(s1 ?? string.Empty, s2 ?? string.Empty);
            dmp.diff_cleanupSemantic(diffs);
            return dmp.diff_prettyHtml(diffs);
        }
    }
}