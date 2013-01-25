<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="CompareServers.aspx.cs"
    Inherits="Sitecore.SharedSource.CompareServers.CompareServersForm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Compare Sitecore Database Servers</title>
    <script src="/sitecore modules/Shell/CompareServers/js/jquery-1.7.1.min.js" type="text/javascript"></script>
    <script type="text/javascript">
        var $j = jQuery.noConflict();
    </script>
    <script src="/sitecore modules/Shell/CompareServers/js/jquery.contextmenu.js?t=2"
        type="text/javascript"></script>
    <script src="/sitecore modules/Shell/CompareServers/js/jquery.ui.position.js" type="text/javascript"></script>
    <script src="/sitecore modules/Shell/CompareServers/js/jquery-ui-1.8.18.custom.min.js"
        type="text/javascript"></script>
    <script src="/sitecore modules/Shell/CompareServers/js/jquery.metadata.js" type="text/javascript"></script>
    <link href="/sitecore modules/Shell/CompareServers/css/smoothness/jquery-ui-1.8.18.custom.css"
        rel="stylesheet" type="text/css" />
    <link href="/sitecore modules/Shell/CompareServers/css/simple.tree.css<%= "?t=" + DateTime.Now.Ticks.ToString() %>"
        rel="stylesheet" type="text/css" />
    <link href="/sitecore modules/Shell/CompareServers/css/compare.servers.css<%= "?t=" + DateTime.Now.Ticks.ToString() %>"
        rel="stylesheet" type="text/css" />
    <link href="/sitecore modules/Shell/CompareServers/js/jquery.contextMenu.css" rel="stylesheet"
        type="text/css" />
    <script type="text/javascript">
        var _allComparisonNodesSelector = '.comparison-node-different,.comparison-node-missing-left,.comparison-node-missing-right';
        var _$HelpDialog = {};

        function HandleAjaxFailure(jqXHR, textStatus, errorThrown) {
            alert(errorThrown + "\nCheck log file.");
        }
        function GetItemComparisonInfo(itempath) {
            $j.ajax({
                type: "POST",
                url: '<%= ResolveUrl("Service.asmx/GetItemComparisonInfo") %>',
                data: "{sessionkey:'" + $j('#hdnCompareSessionKey').val() + "', itempath:'" + itempath + "'}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                    $j('#selected-item-comparison-info').html(msg.d);
                    $j('.tabs').tabs();
                },
                error: HandleAjaxFailure
            });
        }
        function RefreshTreeBranch($node) {
            var data = $node.metadata();
            $j.ajax({
                type: "POST",
                url: '<%= ResolveUrl("Service.asmx/RefreshTreeBranch") %>',
                data: "{sessionkey:'" + $j('#hdnCompareSessionKey').val() + "', itempath:'" + data.value + "'}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                    var li = $node.parent("li");
                    var parent = li.parent("ul");
                    li.replaceWith(msg.d);
                    while (parent != null && parent.find(_allComparisonNodesSelector).length == 0) {
                        var terminate = parent;
                        parent = parent.parent();
                        terminate.remove();
                    }
                },
                error: HandleAjaxFailure
            });
        }
        function ContextMenuAction(key, options) {
            var trigger = options.$trigger;
            var data = trigger.metadata();
            var $dialog = $j('<div></div>')
		                .html("Are you sure you want to " + options.items[key].name + " <b>'" + $j.trim(options.$trigger.text()) + "'</b>?<br/><br/><br/><br/>" + $j('#' + key + '-moreinfo').html())
		                .dialog({
		                    autoOpen: false,
		                    title: options.items[key].name,
		                    resizable: false,
		                    width: 500,
		                    modal: true,
		                    buttons: {
		                        "OK": function () {
		                            $j.ajax({
		                                type: "POST",
		                                url: '<%= ResolveUrl("Service.asmx/TransferItem") %>',
		                                data: "{sessionkey:'" + $j('#hdnCompareSessionKey').val() + "', command:'" + key + "',path:'" + data.value + "'}",
		                                contentType: "application/json; charset=utf-8",
		                                dataType: "json",
		                                success: function (msg) {
		                                    alert(msg.d);
		                                    RefreshTreeBranch(trigger);
		                                },
		                                error: HandleAjaxFailure
		                            });
		                            $j(this).dialog("close");
		                        },
		                        Cancel: function () {
		                            $j(this).dialog("close");
		                        }
		                    }
		                });

            $dialog.dialog('open');
        }
        function AdjustContainerSizes() {
            // Set the height of the tree container
            $j('.tree-container').height($j(window).height() - $j('.tree-container').offset().top);
            $j('.comparison-info').height($j(window).height() - $j('.comparison-info').offset().top);
            $j('.comparison-info').width($j(window).width() - $j('.comparison-info').offset().left - 10);
        }

        $j(function () {

            AdjustContainerSizes();
            $j(window).resize(AdjustContainerSizes);

            $j(_allComparisonNodesSelector).click(function () {
                $j(_allComparisonNodesSelector).removeClass('selected');
                $j(this).addClass('selected');
                var data = $j(this).metadata();
                GetItemComparisonInfo(data.value);
                return false;
            });

            // Setup help dialog
            _$HelpDialog = $j('#help-content').dialog({ autoOpen: false, title: 'Help', width: 900 });
            $j('#help').click(function () { _$HelpDialog.dialog('open'); });


            $j.contextMenu({
                selector: '.root-node li>span',
                build: function ($trigger, e) {
                    var options = {
                        callback: ContextMenuAction,
                        items: {}
                    };
                    var hasItems = false;
                    if ($trigger.hasClass('comparison-node-different') || $trigger.hasClass('comparison-node-missing-left')) {
                        options.items["import"] = { name: "Import (copy field values)", icon: "import" };
                        hasItems = true;
                    }

                    if ($trigger.hasClass('comparison-node-different') || $trigger.hasClass('comparison-node-missing-left')) {
                        options.items["import-overwrite"] = { name: "Import (OVERWRITE existing)", icon: "import" };
                        hasItems = true;
                    }

                    //                    if ($trigger.hasClass('comparison-node-different') || $trigger.hasClass('comparison-node-missing-right')) {
                    //                        options.items["export"] = { name: "Export item", icon: "export" };
                    //                        hasItems = true;
                    //                    }

                    if ($trigger.parent().find(".comparison-node-different,.comparison-node-missing-left").length > 0) {
                        options.items["import-all"] = { name: "Import ALL in branch (copy field values)", icon: "import-all" };
                        hasItems = true;
                    }

                    if (!hasItems)
                        return false;
                    return options;
                }
            });
        });

    </script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:HiddenField ID="hdnCompareSessionKey" runat="server" />
    <div id="import-moreinfo" class="hidden">
        <div class="dialog-content">
            <div class="fieldset">
                <div class="fieldset-legend">
                    more information</div>
                Copies all the field values from the COMPARE DB to the LOCAL DB for the selected
                item.
            </div>
        </div>
    </div>
    <div id="import-overwrite-moreinfo" class="hidden">
        <div class="dialog-content">
            <div class="fieldset">
                <div class="fieldset-legend">
                    more information</div>
                Deletes the selected item from the LOCAL DB and import a new item from the COMPARE
                DB. NOTE: the XML for the deleted item is written to the sitecore log file
            </div>
        </div>
    </div>
    <div id="import-all-moreinfo" class="hidden">
        <div class="dialog-content">
            <div class="fieldset">
                <div class="fieldset-legend">
                    more information</div>
                Copies all the field values from the COMPARE DB to the LOCAL DB for the selected
                item and all its children in the DIFF tree.
            </div>
        </div>
    </div>
    <div id="help-content" class="hidden">
        <div class="dialog-content">
            <p>
                For more information check out the <a href="http://trac.sitecore.net/CompareServers"
                    target="_blank">shared source page</a></p>
            <div class="fieldset">
                <div class="fieldset-legend">
                    Purpose</div>
                <p>
                    The purpose of this tool is to help developers/administrators detect and help correct
                    differences between Sitecore server environments. It is geared towards synchronizing
                    items in a DEV or TEST environment with a PROD environment.</p>
                <p>
                    No Export? It would be easy to implement an "Export" feature but due to the risks
                    of copying items (broken links, orphaned items/fields) I left this out intentionally.
                    Sitecore packages should be used for moving items from low-risk to high-risk environments.</p>
            </div>
            <div class="fieldset">
                <div class="fieldset-legend">
                    Treeview Results</div>
                Each node in the treeview represents a comparison between that item on the Local
                DB and the same item (BY PATH) on the Compare DB. The compare status is denoted
                by the node icon as follows:
                <ul>
                    <li>
                        <img src="/sitecore modules/Shell/CompareServers/images/matched.png" alt="matched" />
                        - no differences</li>
                    <li>
                        <img src="/sitecore modules/Shell/CompareServers/images/missing-left.png" alt="missing on local db" />
                        - item is missing on local DB</li>
                    <li>
                        <img src="/sitecore modules/Shell/CompareServers/images/missing-right.png" alt="missing on compare db" />
                        - item is missing on compare DB</li>
                    <li>
                        <img src="/sitecore modules/Shell/CompareServers/images/mismatch.png" alt="different" />
                        - item has data differences between local and compare DBs</li>
                    <li>* - items marked with an asterisk differ by PATH, ID or TEMPLATE and might not be
                        importable with this tool</li>
                </ul>
                <p>
                    Left-clicking on a compare node will retrieve more detailed information about the
                    particular item comparison and display it in the right pane.
                </p>
                <p>
                    Right-clicking on a compare node will open a context menu with applicable import
                    options.
                </p>
            </div>
            <div class="fieldset">
                <div class="fieldset-legend">
                    Import Action</div>
                The Import function has different results based on the following circumstances:
                <ol>
                    <li>If the item is a template, the item AND ITS CHILDREN are imported to the LOCAL DB.
                        Any existing items are OVERWRITTEN.</li>
                    <li>If the item is missing on the LOCAL DB, the item AND ITS CHILDREN are imported to
                        the LOCAL DB.</li>
                    <li>If the item is different<ul>
                        <li>Import: ALL FIELDS ARE COPIED for that item from the COMPARE DB to the LOCAL DB</li>
                        <li>Import (OVERWRITE): Deletes the selected item from the LOCAL DB and import a new
                            item from the COMPARE DB</li>
                    </ul>
                    </li>
                    <li>Items that differ by @Path WILL NOT BE IMPORTED and must be handled outside of this
                        tool</li>
                </ol>
            </div>
        </div>
    </div>
    <div class="criteria">
        <table cellpadding="5" cellspacing="5" width="800px">
            <tr>
                <td>
                    <div class="fieldset">
                        <div class="fieldset-legend">
                            LOCAL SERVER</div>
                        <table>
                            <tr>
                                <td>
                                    Database
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlLocalServer" runat="server" OnSelectedIndexChanged="ddlServer_SelectedIndexChanged"
                                        AutoPostBack="true">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    Server
                                </td>
                                <td>
                                    <asp:Label ID="lblLocalServer" runat="server" Text="Label"></asp:Label>
                                </td>
                            </tr>
                        </table>
                    </div>
                </td>
                <td>
                    <div class="fieldset">
                        <div class="fieldset-legend">
                            COMPARE SERVER</div>
                        <table>
                            <tr>
                                <td>
                                    Database
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlCompareServer" runat="server" OnSelectedIndexChanged="ddlServer_SelectedIndexChanged"
                                        AutoPostBack="true">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    Server
                                </td>
                                <td>
                                    <asp:Label ID="lblCompareServer" runat="server" Text="Label"></asp:Label>
                                </td>
                            </tr>
                        </table>
                    </div>
                </td>
            </tr>
            <tr>
                <td colspan="2">
                    <div class="fieldset">
                        <div class="fieldset-legend">
                            PATH and OPTIONS</div>
                        <asp:TextBox ID="txtPath" runat="server" Width="100%"></asp:TextBox>
                        <div class="fieldset options">
                            <asp:CheckBox ID="chkIgnoreMissingVersions" runat="server" Checked="true" Text="Ignore missing versions" />&nbsp;&nbsp;&nbsp;
                            <asp:CheckBox ID="chkIgnoreMissingLanguages" runat="server" Checked="true"
                                Text="Ignore missing languages" />
                        </div>
                    </div>
                </td>
            </tr>
            <tr>
                <td colspan="2">
                    <img id="help" src="/sitecore modules/Shell/CompareServers/images/help2.png" alt="help" />
                    <asp:Button ID="btnRun" runat="server" Text="Run Compare" OnClick="btnRun_Click">
                    </asp:Button>
                    <asp:Label ID="lblInfo" runat="server"></asp:Label>
                </td>
            </tr>
        </table>
    </div>
    <div class="results-container">
        <div class="tree-container">
            <asp:PlaceHolder ID="phTree" runat="server"></asp:PlaceHolder>
        </div>
        <div id="selected-item-comparison-info" class="comparison-info">
        </div>
    </div>
    </form>
</body>
</html>
