<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PODetails.aspx.cs" Inherits="APSIM.PerformanceTests.Portal.PODetails"
    Title="APSIM PerformanceTests Details"  %>

<%@ Register Assembly="System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" Namespace="System.Web.UI.DataVisualization.Charting" TagPrefix="asp" %>


<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <script type="text/javascript">
        function scrollToDiv() {
            document.getElementById('<%=ddlVariables.ClientID%>').scrollIntoView();
        }

    </script>

    <table style="width: 100%">
        <tr>
            <td style="width:auto">
                <asp:Label ID="Label4" runat="server" CssClass="SectionTitles" Text="Predicted Observed Details:"></asp:Label><br />
                <asp:HiddenField ID="hfPullRequestID" runat="server" />
                <asp:HiddenField ID="hfPredictedObservedID" runat="server" />
            </td>
            <td style="width: 100px">
                <asp:Button ID="btnBack" runat="server" Text="Return to Pull Request / Simulation Files" OnClick="btnBack_Click" />
            </td>
        </tr>
        <tr>
            <td colspan="2" style="padding-left: 25px;">
                <asp:Label ID="lblPullRequest" runat="server" CssClass="SectionTitles" Text="lblPullRequest"></asp:Label><br />
                <asp:Label ID="lblApsimFile" runat="server" CssClass="SectionTitles" Text="lblApsimFilelblApsimFile"></asp:Label><br />
                <asp:Label ID="lblPOTableName" runat="server" CssClass="SectionTitles" Text="lblPOTableName"></asp:Label>
            </td>
        </tr>
        <tr>
            <td colspan="2"></td>
        </tr>
    </table>
    <asp:UpdatePanel ID="UpdatePanel3" runat="server">
        <ContentTemplate>

            <table style="width: 100%">
                <tr>
                    <td style="width: 800px;">
                        <asp:Label ID="lblTests" runat="server" CssClass="ScreenText" Text="Predicted Observed Tests:"></asp:Label>
                    </td>
                    <td style="width: auto">
                        <asp:Label ID="Label3" runat="server" CssClass="ScreenLabel" Text="Filter"></asp:Label>
                        <asp:TextBox ID="txtSearch_POTests" CssClass="ScreenText" runat="server" AutoPostBack="true" OnTextChanged="txtSearch_POTests_TextChanged" ></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td colspan="2">
                        <asp:GridView ID="gvPOTests" runat="server" AutoGenerateColumns="false" PageSize="20" AllowPaging="true" AllowSorting="true" 
                            DataKeyNames="Variable, Test"  
                            CssClass="Grid2" AlternatingRowStyle-CssClass="alt" PagerStyle-CssClass="pgr"        
                            OnPageIndexChanging="gvPOTests_PageIndexChanging"
                            OnRowDataBound="gvPOTests_RowDataBound"
                            OnSorting="gvPOTests_Sorting" >
                            <Columns>
                                <asp:BoundField DataField="Variable" HeaderText="Variable" HeaderStyle-Width="300px" SortExpression="Variable" />
                                <asp:BoundField DataField="Test" HeaderText="Test" HeaderStyle-Width="100px" SortExpression="Test" />
                                <asp:BoundField DataField="Accepted" HtmlEncode="false" HeaderText="Accepted" HeaderStyle-Width="120px" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:0.000000}" SortExpression="Accepted" />
                                <asp:BoundField DataField="Current" HtmlEncode="false" HeaderText="Current" HeaderStyle-Width="120px" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:0.000000}" SortExpression="Current" />
                                <asp:BoundField DataField="Difference" HeaderText="Difference" HeaderStyle-Width="120px" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:0.000000}"  SortExpression="Difference" />
                                <asp:BoundField DataField="DifferencePercent" HtmlEncode="false" HeaderText="Difference<br />Percent" ItemStyle-HorizontalAlign="Right" HeaderStyle-Width="120px" DataFormatString="{0:0.000000}%" SortExpression="DifferencePercent" />
                                <asp:BoundField DataField="PassedTest" HtmlEncode="False" HeaderText="Passed<br />Test" HeaderStyle-Width="70px" SortExpression="PassedTest"  />
                                <asp:BoundField DataField="IsImprovement" HtmlEncode="False" HeaderText="Is<br />Improvement" HeaderStyle-Width="100px" SortExpression="IsImprovement" />
                            </Columns>
                        </asp:GridView>
                        <br />
                    </td>
                </tr>
            </table>

        </ContentTemplate>
    </asp:UpdatePanel>

    <table style="width: 95%; padding-top: 15px; background-color: lightgray; border: 1px, solid, darkgray; " >
        <tr>
            <td>
                <asp:Label ID="lblDrilldown" runat="server" CssClass="SectionTitles" Text="Predicted Observed Simulation Values Drilldown"></asp:Label>&nbsp;&nbsp;
                <br />
                &nbsp;
            </td>
        </tr>
    </table>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <table style="padding-top: 15px; " >
                <tr>
                    <td>
                        <asp:Timer ID="LoadTimer" runat="server" OnTick="LoadTimer_Tick" Interval="600" ></asp:Timer>
                        <asp:Label ID="Label2" runat="server" CssClass="ScreenText" Text="Select a Variable: " ></asp:Label>&nbsp;&nbsp;
                    </td>
                    <td>
                        <asp:DropDownList ID="ddlVariables" runat="server" AutoPostBack="True" width="300px"
                            DataTextField ="Name" DataValueField="Value" CssClass="ScreenLabel"
                            OnSelectedIndexChanged="ddlVariables_SelectedIndexChanged" >
                        </asp:DropDownList>
                    </td>
                </tr>
            </table>
        </ContentTemplate>
    </asp:UpdatePanel>
    <asp:UpdatePanel ID="UpdatePanel4" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <br />
            <asp:Label ID="lblError" runat="server" Text="lblError" Visible="false" ></asp:Label>
            <asp:Chart ID="chartPODetails" runat="server" Height="500px" Width="900px" >
                <Series>
                    <asp:Series Name="Accepted" ChartArea="ChartArea1" 
                        ChartType="Point" MarkerColor="255, 128, 0" MarkerSize="12" MarkerStyle="Circle"
                        IsVisibleInLegend="true" Tooltip="Observed: #VALX, Predicted: #VALY" Legend="Legend1" ></asp:Series>
                    <asp:Series Name="Current" ChartArea="ChartArea1" 
                        ChartType="Point" MarkerColor="Navy" MarkerSize="6" MarkerStyle="Diamond"
                        IsVisibleInLegend="true" Tooltip="Observed: #VALX, Predicted: #VALY" Legend="Legend1" ></asp:Series>
                    <asp:Series Name="Slope" ChartArea="ChartArea1" ChartType="Line" Color="Black" IsVisibleInLegend="false"  ></asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1"></asp:ChartArea>
                </ChartAreas>
                <Legends>
                    <asp:Legend DockedToChartArea="ChartArea1" Font="Microsoft Sans Serif, 10pt" IsDockedInsideChartArea="False" IsTextAutoFit="False" Name="Legend1">
                    </asp:Legend>
                </Legends>
                <Titles>
                    <asp:Title Name="chartTitle" Text="Current vs Accepted" Font="Microsoft Sans Serif, 13pt, style=Bold" ></asp:Title>
                    <asp:Title Name="xAxisTitle" Text="Observed Values" Docking="Bottom" Font="Microsoft Sans Serif, 11pt, style=Bold" ></asp:Title>
                    <asp:Title Name="yAxisTitle" Text="Predicted Values" Docking="Left" Font="Microsoft Sans Serif, 11pt, style=Bold" ></asp:Title>
                </Titles>
            </asp:Chart>
        </ContentTemplate>
    </asp:UpdatePanel>

    <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional" >
        <ContentTemplate>
        <br />
            <table style="width: 100%">
                <tr>
                    <td style="width: 450px;">
                        <asp:Label ID="lblValues" runat="server" CssClass="ScreenText" Text="Current and Accepted values for Variable:" ></asp:Label>
                    </td>
                    <td style="width: auto">
                        <asp:Label ID="Label6" runat="server" CssClass="ScreenLabel" Text="Page Size"></asp:Label>
                        <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" 
                            OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged" >
                            <asp:ListItem Text="50" Value="50" ></asp:ListItem>
                            <asp:ListItem Selected="True" Text="100" Value="100" ></asp:ListItem>
                            <asp:ListItem Text="150" Value="150" ></asp:ListItem>
                            <asp:ListItem Text="200" Value="200" ></asp:ListItem>
                        </asp:DropDownList>
                        &nbsp;&nbsp;
                    </td>
                    <td style="width: auto">
                        <asp:Label ID="Label5" runat="server" CssClass="ScreenLabel" Text="Filter"></asp:Label>
                        <asp:TextBox ID="txtSearch_POValues" CssClass="ScreenText" runat="server" AutoPostBack="true" OnTextChanged="txtSearch_POValues_TextChanged" ></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td colspan="3">
                        <asp:GridView ID="gvPOValues" runat="server" AutoGenerateColumns="false" AllowPaging="true" AllowSorting="true" 
                            DataKeyNames="ID"  
                            CssClass="Grid2" AlternatingRowStyle-CssClass="alt" PagerStyle-CssClass="pgr"
                            OnPageIndexChanging="gvPOValues_PageIndexChanging"
                            OnRowDataBound="gvPOValues_RowDataBound"
                            OnSorting="gvPOValues_Sorting"  >
                            <Columns>
                                <asp:BoundField DataField="SimulationName" HeaderStyle-Width="200px"  HeaderText="Simulation Name" SortExpression="SimulationName" />
                                <asp:BoundField DataField="CurrentPredictedValue" HtmlEncode="False" HeaderText ="Current<br />Predicted<br />Value" HeaderStyle-Width="100px" 
                                    DataFormatString="{0:0.000000}" ItemStyle-HorizontalAlign="Right" SortExpression="CurrentPredictedValue" />
                                <asp:BoundField DataField="AcceptedPredictedValue" HtmlEncode="False" HeaderText ="Accepted<br />Predicted<br />Value" HeaderStyle-Width="100px" 
                                    DataFormatString="{0:0.000000}" ItemStyle-HorizontalAlign="Right" SortExpression="AcceptedPredictedValue"  />
                                <asp:BoundField DataField="CurrentObservedValue" HtmlEncode="False" HeaderText ="Current<br />Observed<br />Value" HeaderStyle-Width="100px" 
                                    DataFormatString="{0:0.000000}" ItemStyle-HorizontalAlign="Right" SortExpression="CurrentObservedValue" />
                                <asp:BoundField DataField="AcceptedObservedValue" HtmlEncode="False" HeaderText ="Accepted<br />Observed<br />Value" HeaderStyle-Width="100px" 
                                    DataFormatString="{0:0.000000}" ItemStyle-HorizontalAlign="Right" SortExpression="AcceptedObservedValue" />
                                <asp:BoundField DataField="TableName" HeaderText ="Table Name"  HeaderStyle-Width="120px" SortExpression="TableName" />
                                <asp:BoundField DataField="MatchNames" HeaderText ="Match Names"  HeaderStyle-Width="100px" SortExpression="MatchNames" />
                                <asp:BoundField DataField="MatchValues" HeaderText ="Match Values"  HeaderStyle-Width="100px" SortExpression="MatchValues" />
                                <asp:BoundField DataField="ID" HeaderText ="ID"  HeaderStyle-Width="200px" Visible="false" />
                            </Columns>
                        </asp:GridView>
                    </td>
                </tr>
            </table>
        </ContentTemplate>
    </asp:UpdatePanel>

</asp:Content>
