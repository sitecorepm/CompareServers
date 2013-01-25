using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using Sitecore.Data;
using Sitecore.SharedSource.CompareServers.domain;
using Sitecore.Configuration;
using System.IO;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using System.Text;
using System.Web.UI;

namespace Sitecore.SharedSource.CompareServers
{
    public class CompareSession
    {
        private Dictionary<string, ItemComparisonInfo> _differences = null;
        private Database _localDB = null;
        private Database _compareDB = null;
        private string _comparepath = null;
        private bool _ignoreMissingVersions = true;
        private bool _ignoreMissingLanguages = true;

        public string SessionKey { get; set; }
        public Dictionary<string, ItemComparisonInfo> Differences { get { return _differences; } }

        public CompareSession(string sessionKey, string localDBName, string compareDBName, string path, bool ignoreMissingVersions, bool ignoreMissingLanguages)
        {
            _localDB = Factory.GetDatabase(localDBName);
            _compareDB = Factory.GetDatabase(compareDBName);
            _comparepath = path;
            _differences = new Dictionary<string, ItemComparisonInfo>();
            _ignoreMissingVersions = ignoreMissingVersions;
            _ignoreMissingLanguages = ignoreMissingLanguages;

            if (string.IsNullOrEmpty(sessionKey))
                SessionKey = "CompareManagerSession_" + Guid.NewGuid().ToString();
            else
                SessionKey = sessionKey;
        }

        public string SaveSession()
        {
            System.Web.HttpContext.Current.Cache.Insert(this.SessionKey, this, null, 
                                System.Web.Caching.Cache.NoAbsoluteExpiration,
                                TimeSpan.FromMinutes(10.0),
                                CacheItemPriority.Default, null);
            return this.SessionKey;
        }
        public static CompareSession FromSessionKey(string cachekey)
        {
            var mgr = System.Web.HttpContext.Current.Cache[cachekey] as CompareSession;
            if (mgr == null)
                throw new Exception("Unable to resolve CompareManager session. [" + cachekey + "]");
            return mgr;
        }

        public string TransferItem(string command, string path)
        {
            var result = string.Empty;


            if (_localDB == null || _compareDB == null)
                return "Database connections are NULL.";

            switch (command)
            {
                case "import-all":
                    var importItems = _differences.Where(x => x.Key.StartsWith(path)).Select(x => x.Key).OrderByDescending(x => x.Length);
                    foreach (var key in importItems)
                        result += ImportItem(_compareDB, _localDB, key, false) + "\n";
                    break;
                case "import":
                case "import-overwrite":
                    if (!_differences.ContainsKey(path))
                        return "Could not resolve item comparison record for: '" + path + "'";

                    result = ImportItem(_compareDB, _localDB, path, command == "import-overwrite");
                    break;
                default:
                    result = "The '" + command + "' command has not been implemented.";
                    break;
            }

            if (result.Length > 800)
                result = result.Substring(0, 800) + "...\nSee debugging log file for further details.";

            return result;
        }

        private string ImportItem(Database sourceDB, Database targetDB, string path, bool overwrite)
        {
            var result = string.Empty;

            if (!overwrite)
            {
                if (_differences[path].FieldComparisons.ContainsKey("@ID"))
                    return Path.GetFileName(path) + " NOT imported. Choose import (OVERWRITE) to import '@ID' differences.";

                if (_differences[path].FieldComparisons.ContainsKey("@TemplateID"))
                    return Path.GetFileName(path) + " NOT imported. Choose import (OVERWRITE) to import '@TemplateID' differences.";
            }

            if (_differences[path].FieldComparisons.ContainsKey("@Path"))
                return Path.GetFileName(path) + " NOT imported. '@Path' differences must be handled manually.";


            var source = sourceDB.GetItem(path);

            if (source == null)
                result = "Unable to find item in '" + sourceDB.Name + "' database: " + path;
            else
            {
                var targetParent = targetDB.GetItem(source.Parent.Paths.Path);
                if (targetParent == null)
                    result = "Failed to resolve destination path in '" + targetDB.Name + "' database: " + source.Parent.Paths.Path;
                else
                {
                    var target = targetDB.GetItem(source.Paths.Path);

                    try
                    {
                        if (overwrite
                            || target == null
                            || Sitecore.Data.Managers.TemplateManager.IsTemplate(source))
                        {

                            if (target != null)
                            {
                                // Make sure the source template exists on the target db
                                var targetTemplate = targetDB.GetItem(source.Template.InnerItem.Paths.Path);
                                if (targetTemplate == null || targetTemplate.ID != source.Template.ID)
                                    throw new Exception(string.Format("The import item's template ('{0}') does not exist or has a different ID on the '{1}' DB", source.TemplateName, targetDB.Name));

                                if (overwrite)
                                {
                                    if (target.HasChildren)
                                        throw new Exception("Cannot overwrite an item that has children. Delete the children first and try again.");

                                    Sitecore.Diagnostics.Log.Info("Overwriting item: " + target.Paths.Path + "\n" + target.GetOuterXml(true), typeof(CompareServersForm));
                                    target.Delete();
                                }

                            }

                            var xml = source.GetOuterXml(true);
                            var newItem = targetParent.PasteItem(xml, false, PasteMode.Overwrite);
                        }
                        else
                        {
                            //
                            // Update the fields on an existing item
                            //
                            var sourceFields = source.Fields;
                            sourceFields.ReadAll();
                            foreach (Field f in sourceFields)
                            {
                                if (!string.IsNullOrEmpty(f.Name) && !f.Name.StartsWith("__"))
                                {
                                    using (new EditContext(target))
                                    {
                                        if (!f.HasValue && f.ContainsStandardValue || f.IsDefaultValue())
                                            target.Fields[f.ID].Reset();
                                        else
                                            target[f.ID] = source[f.ID];
                                    }

                                }
                            }
                        }


                        result = source.Name + " imported.";
                        Sitecore.Diagnostics.Log.Debug(string.Format("Item imported from '{0}' to '{1}': {2}", sourceDB.Name, targetDB.Name, source.Paths.Path));
                    }
                    catch (Exception ex)
                    {
                        Sitecore.Diagnostics.Log.Error(string.Format("Failed to import item: [sourceDB:{0}][targetDB:{1}][item:{2}]", sourceDB.Name, targetDB.Name, source.Paths.Path), ex, typeof(CompareServersForm));
                        result = ex.Message;
                    }
                }
            }
            return result;
        }

        public string RefreshTreeBranch(string branchpath)
        {
            var html = string.Empty;


            // Remove all differences below this path...
            var keys = _differences.Where(x => x.Key.StartsWith(branchpath)).Select(x => x.Key).ToArray();
            foreach (var k in keys)
                _differences.Remove(k);


            // Find items in external that don't exist in local
            var root = BuildCompareTree(branchpath);

            if (root.Nodes.Count > 0)
            {
                root = root.Nodes[0];

                StringBuilder sb = new StringBuilder();
                HtmlTextWriter htw = new HtmlTextWriter(new System.IO.StringWriter(sb));
                root.RenderControl(htw);

                html = sb.ToString();
            }


            return html;
        }

        public string GetItemComparisonInfo(string itempath)
        {
            var result = string.Empty;

            if (_differences.ContainsKey(itempath))
            {
                var info = _differences[itempath];

                var tbl = "<table class='diff'>{0}</table>";
                var rows = new List<string>();

                rows.Add(RowHtml(new string[] { "Item Path", info.ItemPath }, true));
                rows.Add(RowHtml(new string[] { "Compare Status", Enum.GetName(typeof(CompareStatus), info.Status) }, true));
                rows.Add(RowHtml(new string[] { "Messages", info.Messages.JoinToString("<br/>") }, true));

                var local = _localDB.GetItem(itempath);
                if (local != null)
                    rows.Add(RowHtml(new string[] { "Local Referrers", Sitecore.Globals.LinkDatabase.GetReferrerCount(local).ToString() }, true));

                result = string.Format(tbl, rows.JoinToString());

                if (info.FieldComparisons.Count > 0)
                {
                    var fieldRows = info.FieldComparisons.Values.Select(delegate(FieldComparisonInfo infoF)
                    {
                        return infoF.Differences.Select(delegate(FieldDifferenceInfo fdi)
                        {
                            var tabData = new Dictionary<string, string>();
                            tabData.Add("Diff", fdi.Diff);
                            tabData.Add("Local", HttpUtility.HtmlEncode(fdi.LocalValue));
                            tabData.Add("Compare", HttpUtility.HtmlEncode(fdi.CompareValue));
                            return RowHtml(new string[] { infoF.Name + "<br/><pre>" + infoF.ID + "</pre>", fdi.Message, TabsHtml(tabData) });
                        }).JoinToString();
                    }).ToArray();

                    var headerRow = HeadRowHtml(new string[] { "Field", "Message", "Field Value" });

                    result += "<br />" + string.Format(tbl, headerRow + fieldRows.JoinToString());
                }
            }

            return result;
        }

        public SimpleTreeNode BuildCompareTree()
        {
            return BuildCompareTree(_comparepath);
        }
        public SimpleTreeNode BuildCompareTree(string path)
        {
            var root = new SimpleTreeNode();

            // Find items in local that don't exist in external
            var localRoot = _localDB.GetItem(path);

            if (localRoot == null)
                throw new Exception("Path does not exist in local DB: " + path);

            RunCompare(root, localRoot, _compareDB, CompareStatus.MissingRight);

            var compareRoot = _compareDB.GetItem(path);
            if (compareRoot != null)
                RunCompare(root, compareRoot, _localDB, CompareStatus.MissingLeft);

            return root;
        }


        private bool RunCompare(SimpleTreeNode parentnode, Item item, Database compareDB, CompareStatus missingDirection)
        {
            var dirty = false;

            try
            {
                var info = new ItemComparisonInfo(item);
                if (_differences.ContainsKey(info.ItemKey))
                    info = _differences[info.ItemKey];

                CompareEngine.RunCompare(compareDB, item, info, missingDirection, _ignoreMissingVersions, _ignoreMissingLanguages);

                // Create the tree node
                var nodeIsNew = false;
                //var node = parentnode.ChildNodes.Cast<TreeNode>().SingleOrDefault(x => x.Value == item.Paths.Path);
                var node = parentnode.Nodes.SingleOrDefault(x => x.Value == info.ItemKey);
                if (node == null)
                {
                    node = CreateNode(info);
                    nodeIsNew = true;
                }

                if (info.Status != CompareStatus.Matched)
                {
                    dirty = true;
                    if (!_differences.ContainsKey(info.ItemKey))
                        _differences.Add(info.ItemKey, info);
                }

                if (Sitecore.Data.Managers.TemplateManager.IsTemplate(item))
                {
                    var t = (TemplateItem)item;
                    if (t.StandardValues != null)
                        dirty = RunCompare(node, t.StandardValues, compareDB, missingDirection) | dirty;
                }
                else
                {
                    foreach (var child in item.GetChildren().InnerChildren)
                    {
                        dirty = RunCompare(node, child, compareDB, missingDirection) | dirty;
                    }
                }

                if (dirty && nodeIsNew)
                    parentnode.AddNode(node);

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("RunCompare failed. [parentnode:{0}][item:{1}][compareDB:{2}]", new object[]{
                    parentnode == null? "NULL!" : parentnode.Value,
                    item == null ? "NULL!" : item.Paths.Path,
                    compareDB == null ? "NULL!" : compareDB.Name
                }), ex);
            }

            return dirty;
        }



        private const string _IMG_PATH = "/sitecore modules/Shell/CompareServers/images/";
        private const string _IMG_MATCHED = _IMG_PATH + "matched.png";
        private const string _IMG_MISSING_LEFT = _IMG_PATH + "missing-left.png";
        private const string _IMG_MISSING_RIGHT = _IMG_PATH + "missing-right.png";
        private const string _IMG_DIFFERENT = _IMG_PATH + "mismatch.png";
        private static SimpleTreeNode CreateNode(ItemComparisonInfo info)
        {
            var imageUrl = _IMG_MATCHED;
            var cssClass = string.Empty;
            var statusName = Enum.GetName(typeof(CompareStatus), info.Status);
            switch (info.Status)
            {
                case CompareStatus.Different:
                    imageUrl = _IMG_DIFFERENT;
                    cssClass = "comparison-node-different";
                    break;
                case CompareStatus.MissingLeft:
                    imageUrl = _IMG_MISSING_LEFT;
                    cssClass = "comparison-node-missing-left";
                    break;
                case CompareStatus.MissingRight:
                    imageUrl = _IMG_MISSING_RIGHT;
                    cssClass = "comparison-node-missing-right";
                    break;
            }

            var node = new SimpleTreeNode()
            {
                CssClass = cssClass,
                Text = Path.GetFileName(info.ItemPath),
                Value = info.ItemKey,
                ImageUrl = imageUrl
            };

            node.Data.Add("type", statusName);

            return node;
        }

        /// <summary>
        /// Creates Html suitable for jQuery UI tabs..
        /// </summary>
        private static string TabsHtml(Dictionary<string, string> tabData)
        {
            var tabs = string.Empty;
            var tabContent = string.Empty;
            var i = 1;
            foreach (var entry in tabData)
            {
                var id = "tab-" + i.ToString();
                tabs += "<li><a href='#" + id + "'>" + entry.Key + "</a></li>";
                tabContent += "<div id='" + id + "'>" + entry.Value + "</div>";
                i++;
            }
            return "<div class='tabs'><ul>" + tabs + "</ul>" + tabContent + "</div>";
        }
        private static string RowHtml(IEnumerable<string> values)
        {
            return "<tr>" + string.Join(string.Empty, values.Select(x => "<td>" + x + "</td>").ToArray()) + "</tr>\n";
        }
        private static string RowHtml(IEnumerable<string> values, bool firstColumnIsHeader)
        {
            return "<tr><th>" + values.First() + "</th>" + string.Join(string.Empty, values.Skip(1).Select(x => "<td>" + x + "</td>").ToArray()) + "</tr>\n";
        }
        private static string HeadRowHtml(IEnumerable<string> values)
        {
            return "<tr>" + string.Join(string.Empty, values.Select(x => "<th>" + x + "</th>").ToArray()) + "</tr>\n";
        }
    }
}