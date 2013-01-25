using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CompareServers
{
    public class SimpleTreeNode : System.Web.UI.WebControls.WebControl
    {
        public string ImageUrl { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public string CssClass { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public SimpleTreeNode Parent { get; set; }
        public List<SimpleTreeNode> Nodes { get; private set; }

        public SimpleTreeNode()
        {
            this.Nodes = new List<SimpleTreeNode>();
            this.Data = new Dictionary<string, string>();
        }

        public void AddNode(SimpleTreeNode node)
        {
            // Determine level
            node.Parent = this;
            this.Nodes.Add(node);
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            Render(writer, false);
        }
        private void Render(System.Web.UI.HtmlTextWriter writer, bool isLastNode)
        {
            //base.Render(writer);
            if (this.Parent == null)
            {
                writer.AddAttribute("class", "root-node");
                writer.RenderBeginTag("ul");
            }

            if (isLastNode)
                writer.AddAttribute("class", "last");

            writer.RenderBeginTag("li");

            var dataString = "value:'" + this.Value + "'";
            if (this.Data.Keys.Count > 0)
                dataString += "," + Data.Select(kv => kv.Key + ":'" + kv.Value + "'").JoinToString(",");

            writer.AddAttribute("class", (this.CssClass + " { " + dataString + " }").Trim());

            writer.RenderBeginTag("span");
            writer.WriteLine("<img src='" + ImageUrl + "' alt='' />");
            writer.WriteLine(Text);
            writer.RenderEndTag(); // </span>

            if (this.Nodes.Count > 0)
            {
                writer.RenderBeginTag("ul");
                for (var i = 0; i < this.Nodes.Count; i++)
                    this.Nodes[i].Render(writer, i == this.Nodes.Count - 1);
                writer.RenderEndTag(); // </ul>
            }

            writer.RenderEndTag(); // </li>

            if (this.Parent == null)
                writer.RenderEndTag(); // </ul>

        }
    }
}