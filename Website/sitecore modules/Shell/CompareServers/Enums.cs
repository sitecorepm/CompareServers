using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CompareServers
{
    public enum CompareStatus
    {
        Matched,
        MissingLeft,
        MissingRight,
        Different
    }

}