using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DiffMatchPatch;
using Sitecore.Data.Items;
using Sitecore.SharedSource.CompareServers.domain;
using Sitecore.Data;
using Sitecore.Data.Fields;
using System.Xml.Linq;

namespace Sitecore.SharedSource.CompareServers
{
    public static class CompareEngine
    {
        /// <summary>
        /// Compares two items or templates
        /// </summary>
        /// <param name="compareDB">The compare database</param>
        /// <param name="item">The local item being compared against</param>
        /// <param name="info">Comparison results are stored in this object</param>
        /// <param name="missingDirection">Enum that indicates the appropriate missing flag ('left' or 'right')</param>
        public static void RunCompare(Database compareDB, Item item, ItemComparisonInfo info, CompareStatus missingDirection, bool ignoreMissingVersions, bool ignoreMissingLanguages)
        {
            // See if item exists on other server
            var compare = compareDB.GetItem(item.Paths.Path);
            if (compare == null)
            {
                info.Messages.Add(string.Format("Item does not exist on database '{0}': {1}", compareDB.Name, item.Paths.Path));
                info.Status = missingDirection;

                compare = compareDB.GetItem(item.ID);
                if (compare != null)
                {
                    if (!info.FieldComparisons.ContainsKey("@Path"))
                    {
                        //info.Messages.Add(string.Format("ID matches different item path!!: {0}", compare.Paths.Path));
                        info.ItemPath += "*";
                        info.Messages.Add("Item moved and/or renamed.");
                        var infoF = new FieldComparisonInfo() { Name = "@Path" };
                        infoF.AddDifference("Path different", item.Paths.Path, compare.Paths.Path);
                        info.FieldComparisons.Add("@Path", infoF);
                        info.Status = CompareStatus.Different;
                    }
                }
            }
            else
            {
                // Languages
                foreach (var lang in item.Languages)
                {
                    if (!compare.Languages.Any(x => x.Name == lang.Name))
                    {
                        if (!ignoreMissingLanguages)
                        {
                            info.Messages.Add(string.Format("Language version missing on database '{0}': {1}", compareDB.Name, lang.Name));
                            info.Status = CompareStatus.Different;
                        }
                    }
                    else
                    {
                        foreach (var versionItem in item.Versions.GetVersions(true).Where(x => x.Language.Name == lang.Name))
                        {
                            var compareVersionItem = compare.Versions.GetVersions(true).SingleOrDefault(x => x.Language.Name == lang.Name 
                                                                             && x.Version.Number == versionItem.Version.Number);
                            if (compareVersionItem == null)
                            {
                                if (!ignoreMissingVersions)
                                {
                                    info.Messages.Add(string.Format("Version '{0}' missing on database '{1}'", versionItem.Version.Number, compareDB.Name));
                                    info.Status = CompareStatus.Different;
                                }
                            }
                            else
                            {
                                if (Sitecore.Data.Managers.TemplateManager.IsTemplate(item))
                                    CompareTwoTemplates(compareDB, info, (TemplateItem)versionItem, (TemplateItem)compareVersionItem);
                                else
                                    CompareTwoItems(compareDB, info, versionItem, compareVersionItem);
                            }
                        }

                        
                    }
                }
            }
        }

        private static void CompareTwoItems(Database compareDB, ItemComparisonInfo info, Item item, Item compare)
        {
            var infoF = (FieldComparisonInfo)null;

            if (item.ID.ToString() != compare.ID.ToString())
            {
                if (!info.FieldComparisons.ContainsKey("@ID"))
                {
                    info.ItemPath += "*";
                    info.Messages.Add("Items have same path but difference IDs!!");
                    infoF = new FieldComparisonInfo() { Name = "@ID" };
                    infoF.AddDifference("IDs different", item.ID.ToString(), compare.ID.ToString());
                    info.FieldComparisons.Add("@ID", infoF);
                    info.Status = CompareStatus.Different;
                }
            }

            if (item.TemplateID.ToString() != compare.TemplateID.ToString())
            {
                if (!info.FieldComparisons.ContainsKey("@TemplateID"))
                {
                    info.ItemPath += "*";
                    info.Messages.Add("Items have different templates!!");
                    infoF = new FieldComparisonInfo() { Name = "@TemplateID" };
                    infoF.AddDifference("TemplateIDs different",
                                        item.TemplateName + "<br/>" + item.TemplateID.ToString(),
                                        compare.TemplateName + "<br/>" + compare.TemplateID.ToString());
                    info.FieldComparisons.Add("@TemplateID", infoF);
                    info.Status = CompareStatus.Different;
                }
                return;
            }


            foreach (Field f in item.Fields)
            {
                var tfi = item.Template.GetField(f.ID);
                if (tfi != null && !tfi.Name.StartsWith("__"))
                {
                    if (!info.FieldComparisons.ContainsKey(tfi.Name))
                    {
                        infoF = new FieldComparisonInfo(tfi);

                        var compareF = compare.Fields[tfi.ID];
                        if (compareF == null)
                            infoF.AddDifference(string.Format("Field does not exist on database: {0}", compareDB.Name));
                        else if (item[tfi.ID] != compare[tfi.ID])
                        {
                            var msg = "Value mismatch.";

                            msg += MoreFieldMismatchMessageInfo(f);
                            msg += MoreFieldMismatchMessageInfo(compare.Fields[f.ID]);

                            infoF.AddDifference(msg, item[tfi.ID], compare[tfi.ID]);
                        }

                        if (infoF.Differences.Count > 0)
                        {
                            info.Status = CompareStatus.Different;
                            info.FieldComparisons.Add(tfi.Name, infoF);
                        }
                    }
                }
            }
        }

        private static string MoreFieldMismatchMessageInfo(Field f)
        {
            var msg = string.Empty;
            if (f.IsDefaultValue())
                msg += "<br/>DefaultValue used in '" + f.Database.Name + "'";
            if (f.ContainsStandardValue)
                msg += "<br/>StandardValue used in '" + f.Database.Name + "'";
            return msg;
        }

        private static void CompareTwoTemplates(Database compareDB, ItemComparisonInfo info, TemplateItem template, TemplateItem compareTemplate)
        {
            var infoF = (FieldComparisonInfo)null;
            foreach (var localF in template.OwnFields)
            {
                if (!localF.Name.StartsWith("__"))
                {
                    if (!info.FieldComparisons.ContainsKey(localF.Name))
                    {
                        infoF = new FieldComparisonInfo(localF);

                        var compareF = compareTemplate.GetField(localF.ID);
                        if (compareF == null)
                            infoF.AddDifference(string.Format("Field does not exist on database: {0}", compareDB.Name));
                        else
                        {
                            if (localF.Type != compareF.Type)
                                infoF.AddDifference("Type mismatch.", localF.Type, compareF.Type);
                            if (localF.Source != compareF.Source)
                                infoF.AddDifference("Source mismatch.", localF.Source, compareF.Source);
                            if (localF.Name != compareF.Name)
                                infoF.AddDifference("Name mismatch.", localF.Name, compareF.Name);


                            //foreach (var lang in localF.InnerItem.Languages)
                            //{
                            //    if (!compareF.InnerItem.Languages.Any(x => x.Name == lang.Name))
                            //        infoF.AddDifference("Language version missing", lang.Name, string.Empty);
                            //    else
                            //    {
                            //        var xmlLocal = GetSlimXml(localF.InnerItem.Database.GetItem(localF.ID, lang));
                            //        var xmlCompare = GetSlimXml(compareDB.GetItem(compareF.ID, lang));
                            //        if (xmlLocal != xmlCompare)
                            //            infoF.AddDifference(string.Format("({0}) Serialized field XML mismatch.", lang.Name), xmlLocal, xmlCompare);
                            //    }
                            //}

                            //foreach (var lang in compareF.InnerItem.Languages)
                            //{
                            //    if (!localF.InnerItem.Languages.Any(x => x.Name == lang.Name))
                            //        infoF.AddDifference("Language version missing", string.Empty, lang.Name);
                            //}

                            var xmlLocal = GetSlimXml(localF.InnerItem);
                            var xmlCompare = GetSlimXml(compareF.InnerItem);
                            if (xmlLocal != xmlCompare)
                                infoF.AddDifference("Serialized field XML mismatch.", xmlLocal, xmlCompare);
                        }

                        if (infoF.Differences.Count > 0)
                        {
                            info.Status = CompareStatus.Different;
                            info.FieldComparisons.Add(localF.Name, infoF);
                            info.Messages.Add("Field differences.");
                        }
                    }
                }
            }
        }

        private static string GetSlimXml(Item i)
        {
            var doc = XDocument.Parse(i.GetOuterXml(true));
            var systemFields = doc.Descendants("field").Where(x => x.Attribute("key").Value.StartsWith("__")
                                                            || string.IsNullOrEmpty(x.Attribute("key").Value));
            systemFields.Remove();

            // Remove other languages
            var otherLanguages = doc.Descendants("version").Where(x => x.Attribute("language").Value != i.Language.Name).ToArray();
            foreach (var v in otherLanguages)
                v.Remove();

            // Remove other versions
            var otherVersions = doc.Descendants("version").Where(x => x.Attribute("version").Value != i.Version.Number.ToString()).ToArray();
            foreach (var v in otherVersions)
                v.Remove();

            foreach (var version in doc.Descendants("version"))
            {
                var sortedFields = version.Element("fields")
                                          .Elements("field")
                                          .OrderBy(x => x.Attribute("tfid").Value)
                                          .ToArray();

                foreach (var f in sortedFields)
                    f.Remove();
                foreach (var f in sortedFields)
                    version.Element("fields").Add(f);
            }
            return doc.ToString();
        }

        private static TemplateItem GetTemplate(Item item)
        {
            TemplateItem template = null;
            if (Sitecore.Data.Managers.TemplateManager.IsTemplate(item))
                template = (TemplateItem)item;
            else
                template = item.Template;
            return template;
        }

    }
}