<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="TestsGrids.aspx.cs" Inherits="APSIM.PerformanceTests.Portal.TestsGrids"
    Title="APSIM PerformanceTests Tests Grid"  %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <table style="width: 100%">
        <tr>
            <td style="width:auto">
                <asp:Label ID="Label4" runat="server" CssClass="SectionTitles" Text="Predicted Observed Tests:"></asp:Label><br />
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
            </td>
        </tr>
        <tr>
            <td colspan="2">&nbsp;</td>
        </tr>
    </table>
    <asp:UpdatePanel ID="upGrids" runat="server">
        <ContentTemplate>
            <asp:PlaceHolder ID="phGrids" runat="server"></asp:PlaceHolder>
        </ContentTemplate>
    </asp:UpdatePanel>


</asp:Content>
