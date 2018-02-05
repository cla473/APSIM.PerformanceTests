<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ValuesCharts.aspx.cs" Inherits="APSIM.PerformanceTests.Portal.ValuesCharts"
    Title="APSIM PerformanceTests ValuesCharts"  %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <table style="width: 100%">
        <tr>
            <td style="width:auto">
                <asp:Label ID="Label4" runat="server" CssClass="SectionTitles" Text="Predicted Observed Details:"></asp:Label><br />
                <asp:HiddenField ID="hfPullRequestID" runat="server" />
                <asp:HiddenField ID="hfPredictedObservedID" runat="server" />
            </td>
            <td style="width: 100px">
                <asp:Button ID="btnBack" runat="server" Text="Return to Predicted Observed Tests" OnClick="btnBack_Click" />
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
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <asp:PlaceHolder ID="phGrids" runat="server"></asp:PlaceHolder>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
