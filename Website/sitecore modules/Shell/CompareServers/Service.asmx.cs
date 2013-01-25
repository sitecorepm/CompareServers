using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace Sitecore.SharedSource.CompareServers
{
    /// <summary>
    /// Summary description for CompareServers
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class CompareServers : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod]
        public string TransferItem(string sessionkey, string command, string path)
        {
            var result = string.Empty;
            try
            {
                var compareMgr = CompareSession.FromSessionKey(sessionkey);
                result = compareMgr.TransferItem(command, path);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Failed to transfer item. [direction:{0}][path:{1}]", command, path), ex, typeof(CompareServersForm));
                result = "Error: " + ex.Message + "...\nSee debugging log file for further details.";
            }
            return result;
        }

        [WebMethod]
        public string GetItemComparisonInfo(string sessionkey, string itempath)
        {
            var result = string.Empty;
            try
            {
                var compareMgr = CompareSession.FromSessionKey(sessionkey);
                result = compareMgr.GetItemComparisonInfo(itempath);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Failed to get item comparison information. [itempath:{0}]", itempath), ex, typeof(CompareServersForm));
                result = "Error: " + ex.Message + "...\nSee debugging log file for further details.";
            }
            return result;
        }

        [WebMethod]
        public string RefreshTreeBranch(string sessionkey, string itempath)
        {
            var result = string.Empty;
            try
            {
                var compareMgr = CompareSession.FromSessionKey(sessionkey);
                result = compareMgr.RefreshTreeBranch(itempath);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Failed to refresh tree branch: " + itempath, ex, typeof(CompareServersForm));
                throw ex;
            }
            return result;
        }
    }
}
