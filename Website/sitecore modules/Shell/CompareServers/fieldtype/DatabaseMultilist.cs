using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Shell.Applications.ContentEditor;
using System.Web.UI;
using System.Collections;
using Sitecore.Diagnostics;
using Sitecore.Text;
using Sitecore.Configuration;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.SharedSource.CompareServers.FieldType
{
    public class DatabaseMultilist : MultilistEx
    {
        protected override void DoRender(HtmlTextWriter output)
        {

            IDictionary dictionary;
            ArrayList list;
            Assert.ArgumentNotNull(output, "output");
            var current = Sitecore.Context.ContentDatabase.GetItem(this.ItemID);
            //Item[] items = this.GetItems(current);
            //this.GetSelectedItems(items, out list, out dictionary);

            var allDBs = Factory.GetDatabaseNames();
            var selectedDBs = new ListString(this.Value).ToArray();
            var unselectedDBs = allDBs.Except(selectedDBs);

            base.ServerProperties["ID"] = this.ID;
            string str = string.Empty;
            if (this.ReadOnly)
            {
                str = " disabled=\"disabled\"";
            }
            output.Write("<input id=\"" + this.ID + "_Value\" type=\"hidden\" value=\"" + Sitecore.StringUtil.EscapeQuote(this.Value) + "\" />");
            output.Write("<table" + this.GetControlAttributes() + ">");
            output.Write("<tr>");
            output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">" + Translate.Text("All") + "</td>");
            output.Write("<td width=\"20\">" + Images.GetSpacer(20, 1) + "</td>");
            output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">" + Translate.Text("Selected") + "</td>");
            output.Write("<td width=\"20\">" + Images.GetSpacer(20, 1) + "</td>");
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write("<select id=\"" + this.ID + "_unselected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\" size=\"10\"" + str + " ondblclick=\"javascript:scContent.multilistMoveRight('" + this.ID + "')\" onchange=\"javascript:document.getElementById('" + this.ID + "_all_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''\" >");

            foreach (var db in unselectedDBs)
                output.Write("<option value=\"" + db + "\">" + db + "</option>");

            //foreach (DictionaryEntry entry in dictionary)
            //{
            //    Item item = entry.Value as Item;
            //    if (item != null)
            //    {
            //        output.Write("<option value=\"" + this.GetItemValue(item) + "\">" + item.DisplayName + "</option>");
            //    }
            //}
            output.Write("</select>");
            output.Write("</td>");
            output.Write("<td valign=\"top\">");
            this.RenderButton(output, "Core/16x16/arrow_blue_right.png", "javascript:scContent.multilistMoveRight('" + this.ID + "')");
            output.Write("<br />");
            this.RenderButton(output, "Core/16x16/arrow_blue_left.png", "javascript:scContent.multilistMoveLeft('" + this.ID + "')");
            output.Write("</td>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write("<select id=\"" + this.ID + "_selected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\" size=\"10\"" + str + " ondblclick=\"javascript:scContent.multilistMoveLeft('" + this.ID + "')\" onchange=\"javascript:document.getElementById('" + this.ID + "_selected_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''\">");

            foreach (var db in selectedDBs)
            {
                if (!allDBs.Contains(db))
                    output.Write("<option value=\"" + db + "\">" + db + ' ' + Translate.Text("[Not in the selection List]") + "</option>");
                else
                    output.Write("<option value=\"" + db + "\">" + db + "</option>");
            }
            //for (int i = 0; i < list.Count; i++)
            //{
            //    Item item3 = list[i] as Item;
            //    if (item3 != null)
            //    {
            //        output.Write("<option value=\"" + this.GetItemValue(item3) + "\">" + item3.DisplayName + "</option>");
            //    }
            //    else
            //    {
            //        string path = list[i] as string;
            //        if (path != null)
            //        {
            //            string str3;
            //            Item item4 = Context.ContentDatabase.GetItem(path);
            //            if (item4 != null)
            //            {
            //                str3 = item4.DisplayName + ' ' + Translate.Text("[Not in the selection List]");
            //            }
            //            else
            //            {
            //                str3 = path + ' ' + Translate.Text("[Item not found]");
            //            }
            //            output.Write("<option value=\"" + path + "\">" + str3 + "</option>");
            //        }
            //    }
            //}
            output.Write("</select>");
            output.Write("</td>");
            output.Write("<td valign=\"top\">");
            this.RenderButton(output, "Core/16x16/arrow_blue_up.png", "javascript:scContent.multilistMoveUp('" + this.ID + "')");
            output.Write("<br />");
            this.RenderButton(output, "Core/16x16/arrow_blue_down.png", "javascript:scContent.multilistMoveDown('" + this.ID + "')");
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"" + this.ID + "_all_help\"></div>");
            output.Write("</td>");
            output.Write("<td></td>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"" + this.ID + "_selected_help\"></div>");
            output.Write("</td>");
            output.Write("<td></td>");
            output.Write("</tr>");
            output.Write("</table>");
        }

        private void RenderButton(HtmlTextWriter output, string icon, string click)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(icon, "icon");
            Assert.ArgumentNotNull(click, "click");
            ImageBuilder builder = new ImageBuilder
            {
                Src = icon,
                Width = 0x10,
                Height = 0x10,
                Margin = "2px"
            };
            if (!this.ReadOnly)
            {
                builder.OnClick = click;
            }
            output.Write(builder.ToString());
        }

 

 

    }
}