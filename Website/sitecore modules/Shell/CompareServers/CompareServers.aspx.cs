using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DiffMatchPatch;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Links;
using Sitecore.Pipelines.GetMediaCreatorOptions;
using Sitecore.Resources.Media;
using Sitecore.Text;
using Sitecore.Workflows;
using Sitecore.SharedSource.CompareServers.domain;
using System.Text;
using Sitecore.Data.Managers;


namespace Sitecore.SharedSource.CompareServers
{
    public partial class CompareServersForm : BaseUtilityForm
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsPostBack)
            {

                var masterDB = Factory.GetDatabase("master");
                var settings = masterDB.GetItem("/sitecore/system/Modules/CompareServers/Settings");

                if (settings != null)
                {
                    var localDBs = settings["LocalDB"].Split('|');
                    var compareDBs = settings["CompareDB"].Split('|');

                    ddlLocalServer.DataSource = localDBs;
                    ddlLocalServer.DataBind();
                    SetValue(ddlLocalServer, localDBs[0]);
                    ResolveServerName(ddlLocalServer, lblLocalServer);

                    ddlCompareServer.DataSource = compareDBs;
                    ddlCompareServer.DataBind();
                    SetValue(ddlCompareServer, compareDBs[0]);
                    ResolveServerName(ddlCompareServer, lblCompareServer);

                    txtPath.Text = settings["DefaultPath"];
                }
                else
                    throw new Exception("There are no settings configured at: /sitecore/system/Modules/CompareServers/Settings");


            }
        }

        protected void ddlServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ddl = (DropDownList)sender;
            var lbl = (Label)FindControl(ddl.ID.Replace("ddl", "lbl"));
            ResolveServerName(ddl, lbl);
        }

        private void SetValue(DropDownList ddl, string value)
        {
            var item = ddl.Items.FindByValue(value);
            if (item != null)
                ddl.SelectedIndex = ddl.Items.IndexOf(item);
        }

        private void ResolveServerName(DropDownList ddl, Label lbl)
        {
            var serverName = "Unresolved...";
            var cxnName = ddl.SelectedValue;
            var cxnString = Sitecore.Configuration.Settings.GetConnectionString(cxnName);
            if (!string.IsNullOrEmpty(cxnString))
            {
                var ds = cxnString.Split(';').SingleOrDefault(x => x.StartsWith("Data Source"));
                if (ds != null)
                    serverName = ds.Split('=')[1];
            }
            lbl.Text = serverName;
        }

        protected void btnRun_Click(object sender, System.EventArgs e)
        {
            try
            {
                var sessionkey = hdnCompareSessionKey.Value;
                var compareMgr = new CompareSession(sessionkey, 
                                                ddlLocalServer.SelectedValue, 
                                                ddlCompareServer.SelectedValue, 
                                                txtPath.Text.Trim(), 
                                                chkIgnoreMissingVersions.Checked,
                                                chkIgnoreMissingLanguages.Checked);

                // Find items in external that don't exist in local
                var root = compareMgr.BuildCompareTree();
                if (root.Nodes.Count == 0)
                    lblInfo.Text = "No differences were found";
                else
                {
                    root = root.Nodes[0];
                    root.Parent = null;
                    phTree.Controls.Add(root);

                    lblInfo.Text = string.Format("(missing on source server: {0}, missing on compare server: {1}, different: {2})",
                                            compareMgr.Differences.Values.Count(x => x.Status == CompareStatus.MissingLeft),
                                            compareMgr.Differences.Values.Count(x => x.Status == CompareStatus.MissingRight),
                                            compareMgr.Differences.Values.Count(x => x.Status == CompareStatus.Different));
                }

                hdnCompareSessionKey.Value = compareMgr.SaveSession();
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Failed to perform compare: " + txtPath.Text, ex, typeof(CompareServersForm));
                lblInfo.Text = "Error: " + ex.Message + ". See log for more details.";
            }
        }

    }

}