using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CompareServers
{
    public class BaseUtilityForm : System.Web.UI.Page
	{
        protected override void OnLoad(EventArgs e)
        {
            if (!Sitecore.Context.User.IsAdministrator)
                throw new Exception("Sorry, only administrators can run utilities.");

            base.OnLoad(e);
        }
	}
}