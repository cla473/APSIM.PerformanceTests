﻿using APSIM.PerformanceTests.Portal.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Reflection;


namespace APSIM.PerformanceTests.Portal
{
    public partial class Default : System.Web.UI.Page
    {
        #region Constants and variables
        private List<vApsimFile> ApsimFileList;
        private List<vSimFile> SimFilesList;

        private DataTable ApsimFileDT;
        private DataTable SimFilesDT;

        System.Web.UI.WebControls.Image sortImage_ApsimFileList = new System.Web.UI.WebControls.Image();
        System.Web.UI.WebControls.Image sortImage_SimFilesList = new System.Web.UI.WebControls.Image();

        public string SortDireaction_ApsimFileList
        {
            get
            {
                if (ViewState["SortDireaction_ApsimFileList"] == null)
                    return string.Empty;
                else
                    return ViewState["SortDireaction_ApsimFileList"].ToString();
            }
            set
            {
                ViewState["SortDireaction_ApsimFileList"] = value;
            }
        }
        private string _sortDirection_ApsimFileList;

        public string SortDireaction_SimFilesList
        {
            get
            {
                if (ViewState["SortDireaction_SimFilesList"] == null)
                    return string.Empty;
                else
                    return ViewState["SortDireaction_SimFilesList"].ToString();
            }
            set
            {
                ViewState["SortDireaction_SimFilesList"] = value;
            }
        }
        private string _sortDirection_SimFilesList;
        #endregion


        #region Page and Control Events

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                BindApsimFilesGrid();

                if (Request.QueryString["PULLREQUEST"] != null)
                {
                    
                    int pullRequestId = int.Parse(Request.QueryString["PULLREQUEST"].ToString());
                    hfPullRequestId.Value = pullRequestId.ToString();

                    BindSimFilesGrid(pullRequestId);
                }
            }
            //if the Simulation File grid has data (ie after postback, then need to make sure the scolling will work
            //if (gvSimFiles.Rows.Count > 0)
            //{
            //    ClientScript.RegisterStartupScript(this.GetType(), "CreateGridHeader", "<script>CreateGridHeader('GridDataDiv_SimFiles', 'ContentPlaceHolder1_gvSimFiles', 'GridHeaderDiv_SimFiles');</script>");
            //}
        }

        protected void btnCompare_Click(object sender, EventArgs e)
        {
            Response.Redirect("Compare.aspx");
        }


        protected void btnOk_Click(object sender, EventArgs e)
        {
            AcceptStatsLog acceptlog = new AcceptStatsLog();
            acceptlog.PullRequestId = int.Parse(txtPullRequestID.Text);
            acceptlog.SubmitDate = DateTime.ParseExact(txtSubmitDate.Text, "dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);

            acceptlog.SubmitPerson = txtSubmitPerson.Text;

            string fileInfo = txtFileCount.Text.Trim();
            int posn = fileInfo.IndexOf('.');  
            if (posn > 0)
            {
                acceptlog.FileCount = int.Parse(fileInfo.Substring(0, posn));
            }
            else
            {
                acceptlog.FileCount = int.Parse(txtFileCount.Text);
            }

            acceptlog.LogPerson = txtName.Text;
            acceptlog.LogReason = txtDetails.Text;
            acceptlog.LogAcceptDate = DateTime.ParseExact(txtAcceptDate.Text, "dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
            acceptlog.LogStatus = true;

            bool doAcceptStats = false;
            bool doUpdateStats = false;
            if (lblTitle.Text.StartsWith("Update"))
            {
                int pullRequestId2 = 0;
                if (int.TryParse(txtPullRequestId2.Text, out pullRequestId2) == true)
                {
                    acceptlog.StatsPullRequestId = pullRequestId2;
                    acceptlog.LogReason = "Update 'Accepted' Stats to Pull Request Id: " + pullRequestId2.ToString();
                    acceptlog.LogStatus = false;
                    doUpdateStats = true;
                }
            }
            else
            {
                doAcceptStats = true;
            }

            txtName.Text = string.Empty;
            txtDetails.Text = string.Empty;
            txtPullRequestId2.Text = string.Empty;
            this.ModalPopupExtender1.Hide();

            if (doAcceptStats == true)
            {
                UpdatePullRequestStats("Accept", acceptlog);
            }
            else if (doUpdateStats == true)
            {
                UpdatePullRequestStats("Update", acceptlog);
            }

            Response.Redirect(Request.RawUrl);
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
        }

        protected void btnDifferences_Click(object sender, EventArgs e)
        {
            string pullrequestId = hfPullRequestId.Value.ToString();
            Response.Redirect(string.Format("Difference.aspx?PULLREQUEST={0}", pullrequestId));
        }

        protected void btnTests_Click(object sender, EventArgs e)
        {
            string pullrequestId = hfPullRequestId.Value.ToString();
            Response.Redirect(string.Format("Tests.aspx?PULLREQUEST={0}", pullrequestId));
        }


        protected void gvApsimFiles_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvApsimFiles.PageIndex = e.NewPageIndex;
            BindApsimFilesGrid();
        }

        protected void gvApsimFiles_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Don't interfere with other commands.  We may not have any now, but this is another safe-code strategy.
            if (e.CommandName == "CellSelect" || e.CommandName == "AcceptStats" || e.CommandName == "UpdateStats")
            {
                // Unpack the arguments.
                String[] arguments = ((String)e.CommandArgument).Split(new char[] { ',' });

                // More safe coding: Don't assume there are at least 2 arguments. (And ignore when there are more.)
                if (arguments.Length >= 2)
                {
                    // And even more safe coding: Don't assume the arguments are proper int values.
                    int rowIndex = -1, cellIndex = -1;
                    bool canUpdate = false;
                    int.TryParse(arguments[0], out rowIndex);
                    int.TryParse(arguments[1], out cellIndex);
                    bool.TryParse(arguments[2], out canUpdate);

                    // Use the rowIndex to select the Row, like Select would do.
                    if (rowIndex > -1 && rowIndex < gvApsimFiles.Rows.Count)
                    {
                        gvApsimFiles.SelectedIndex = rowIndex;
                    }

                    //here we either update the Update Panel (if the user clicks only anything OTHER THAN our'Button'
                    //or we process the UpdatePullRequest as Merged
                    if (e.CommandName == "AcceptStats" && cellIndex == 8 && canUpdate == true)
                    {
                        lblTitle.Text = "Accept Stats Request";
                        lblPullRequestId2.Visible = false;
                        txtPullRequestId2.Visible = false;
                        lblDetails.Visible = true;
                        txtDetails.Visible = true;
                        lblFileCount.Visible = true;
                        txtFileCount.Visible = true;

                        txtPullRequestID.Text = gvApsimFiles.Rows[rowIndex].Cells[0].Text;
                        DateTime subDate = DateTime.Parse(gvApsimFiles.Rows[rowIndex].Cells[1].Text);
                        txtSubmitDate.Text = subDate.ToString("dd/MM/yyyy HH:mm");
                        txtSubmitPerson.Text = gvApsimFiles.Rows[rowIndex].Cells[2].Text;
                        txtAcceptDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                        int acceptedFileCount = Int32.Parse(hfAcceptedFileCount.Value.ToString());
                        int currentFilecount = Int32.Parse(gvApsimFiles.Rows[rowIndex].Cells[5].Text);
                        if (acceptedFileCount != currentFilecount)
                        {
                            txtFileCount.Text = string.Format("{0}. This does not match 'Accepted' file count of {1}.", currentFilecount.ToString(), acceptedFileCount.ToString());
                            txtFileCount.CssClass = "FailedTests";
                            txtFileCount.Width = Unit.Pixel(320);
                            pnlpopup.Height = Unit.Pixel(300);
                        }
                        else
                        {
                            txtFileCount.Text = currentFilecount.ToString();
                            //txtFileCount.CssClass = "Reset";
                            txtFileCount.Width = Unit.Pixel(200);
                            pnlpopup.Height = Unit.Pixel(270);
                        }
                        this.ModalPopupExtender1.Show();
                    }
                    else if (e.CommandName == "UpdateStats")
                    {
                        lblTitle.Text = "Update Accepted Stats for this Pull Request";
                        lblPullRequestId2.Visible = true;
                        txtPullRequestId2.Visible = true;
                        lblDetails.Visible = false;
                        txtDetails.Visible = false;
                        lblFileCount.Visible = false;
                        txtFileCount.Visible = false;

                        txtPullRequestID.Text = gvApsimFiles.Rows[rowIndex].Cells[0].Text;
                        DateTime subDate = DateTime.Parse(gvApsimFiles.Rows[rowIndex].Cells[1].Text);
                        txtSubmitDate.Text = subDate.ToString("dd/MM/yyyy HH:mm");
                        txtSubmitPerson.Text = gvApsimFiles.Rows[rowIndex].Cells[2].Text;
                        txtAcceptDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                        pnlpopup.Height = Unit.Pixel(260);
                        this.ModalPopupExtender1.Show();

                    }
                    else if (e.CommandName == "CellSelect")
                    {
                        int pullRequestId = int.Parse(gvApsimFiles.Rows[rowIndex].Cells[0].Text);
                        DateTime subDate = DateTime.Parse(gvApsimFiles.Rows[rowIndex].Cells[1].Text);
                        int acceptedPullRequestId = int.Parse(gvApsimFiles.Rows[rowIndex].Cells[6].Text);
                        int passPercent = int.Parse(gvApsimFiles.Rows[rowIndex].Cells[4].Text);
                        BindSimFilesGrid(pullRequestId, subDate, acceptedPullRequestId, passPercent);
                    }
                }
            }
        }

        protected void gvApsimFiles_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                //Row.Cells[4] = PercentPassed
                if (e.Row.Cells[4].Text.Equals("100"))
                {
                    e.Row.ForeColor = Color.Green;
                }
                //Row.Cells[3] = StatsAccepted
                if (e.Row.Cells[3].Text.ToLower().Equals("true"))
                {
                    e.Row.ForeColor = Color.Green;
                    e.Row.Font.Bold = true;
                }

                //Active cell click events on individual cells, instead of the row
                foreach (TableCell cell in e.Row.Cells)
                {
                    // Although we already know this should be the case, make safe code. Makes copying for reuse a lot easier.
                    if (cell is DataControlFieldCell)
                    {
                        int cellIndex = e.Row.Cells.GetCellIndex(cell);
                        bool canUpdate = false;
                        // if we are binding the 'Button' column, and the "StatsAccepted' is false, then whe can Update the Merge Status.
                        if (cellIndex == 8)
                        {
                            //Row.Cells[3] = StatsAccepted
                            if (e.Row.Cells[3].Text.ToLower().Equals("false"))
                            {
                                canUpdate = true;
                                Button db = (Button)e.Row.Cells[cellIndex].FindControl("btnAcceptStats");
                                if (db != null)
                                {
                                    db.OnClientClick = "return confirm('Are you certain you want to Accept the Stats for this Pull Request?');";
                                    db.CommandName = "AcceptStats";
                                    db.CommandArgument = String.Format("{0},{1},{2}", e.Row.RowIndex, cellIndex, canUpdate);
                                }
                            }
                        }
                        else if (cellIndex == 9)
                        {
                            canUpdate = true;
                            Button db = (Button)e.Row.Cells[cellIndex].FindControl("btnUpdateStats");
                            if (db != null)
                            {
                                db.OnClientClick = "return confirm('Are you certain you want to Update the Stats for this Pull Request?');";
                                db.CommandName = "UpdateStats";
                                db.CommandArgument = String.Format("{0},{1},{2}", e.Row.RowIndex, cellIndex, canUpdate);
                            }
                        }
                        else
                        {
                            // Put the link on the cell.
                            cell.Attributes["onclick"] = Page.ClientScript.GetPostBackClientHyperlink(gvApsimFiles, String.Format("CellSelect${0},{1},{2}", e.Row.RowIndex, cellIndex, canUpdate));
                            e.Row.Attributes["style"] = "cursor:pointer";
                            // Register for event validation: This will keep ASP from giving nasty errors from getting events from controls that shouldn't be sending any.
                            Page.ClientScript.RegisterForEventValidation(gvApsimFiles.UniqueID, String.Format("CellSelect${0},{1},{2}", e.Row.RowIndex, cellIndex, canUpdate));
                        }
                    }
                }
            }
        }

        protected void gvApsimFiles_Sorting(object sender, GridViewSortEventArgs e)
        {
            SetSortDirection("gvApsimFiles", SortDireaction_ApsimFileList);

            if (ApsimFileDT == null) 
            {
                if (Session["ApsimFileDT"] != null)
                {
                    ApsimFileDT = (DataTable)Session["ApsimFileDT"];
                }
            }

            if (ApsimFileDT != null)
            {
                //Sort the data.
                ApsimFileDT.DefaultView.Sort = e.SortExpression + " " + _sortDirection_ApsimFileList;
                gvApsimFiles.DataSource = ApsimFileDT;

                gvApsimFiles.DataBind();
                SortDireaction_ApsimFileList = _sortDirection_ApsimFileList;

                int sortColumnIndex = 0;
                foreach (DataControlFieldHeaderCell headerCell in gvApsimFiles.HeaderRow.Cells)
                {
                    //Make sure we are displaying the correct header for all columns
                    switch (headerCell.ContainingField.SortExpression)
                    {
                        case "PullRequestId":
                            gvApsimFiles.Columns[0].HeaderText = "Pull<br />Req. Id";
                            break;
                        case "RunDate":
                            gvApsimFiles.Columns[1].HeaderText = "Run Date";
                            break;
                        case "SubmitDetails":
                            gvApsimFiles.Columns[2].HeaderText = "Submit<br />Persons";
                            break;
                        case "StatsAccepted":
                            gvApsimFiles.Columns[3].HeaderText = "Stats<br />Accepted";
                            break;
                        case "PercentPassed":
                            gvApsimFiles.Columns[4].HeaderText = "Percent<br />Passed";
                            break;
                        case "Total":
                            gvApsimFiles.Columns[5].HeaderText = "Total<br />Files";
                            break;
                        case "AcceptedPullRequestId":
                            gvApsimFiles.Columns[6].HeaderText = "Accepted<br />PR Id";
                            break;
                        case "AcceptedRunDate":
                            gvApsimFiles.Columns[7].HeaderText = "Accepted<br />Run Date";
                            break;
                    }
                    //get the index and details for the column we are sorting
                    if (headerCell.ContainingField.SortExpression == e.SortExpression)
                    {
                        sortColumnIndex = gvApsimFiles.HeaderRow.Cells.GetCellIndex(headerCell);
                    }
                }
                if (_sortDirection_ApsimFileList == "ASC")
                {
                    gvApsimFiles.Columns[sortColumnIndex].HeaderText = gvApsimFiles.Columns[sortColumnIndex].HeaderText + "  ▲";
                }
                else
                {
                    gvApsimFiles.Columns[sortColumnIndex].HeaderText = gvApsimFiles.Columns[sortColumnIndex].HeaderText + "  ▼";
                }
                gvApsimFiles.DataBind();

            }
        }


        protected void gvSimFiles_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvSimFiles.PageIndex = e.NewPageIndex;
            if (SimFilesDT == null)
            {
                if (Session["SimFilesDT"] != null)
                {
                    SimFilesDT = (DataTable)Session["SimFilesDT"];
                }
            }

            if (SimFilesDT != null)
            {
                //Sort the data.
                gvSimFiles.DataSource = SimFilesDT;
                gvSimFiles.DataBind();
            }
        }


        protected void gvSimFiles_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if (e.Row.Cells[3].Text.Equals("100"))
                {
                    e.Row.ForeColor = Color.Green;
                }
                //Activate the row click event
                e.Row.Attributes["onclick"] = Page.ClientScript.GetPostBackClientHyperlink(gvSimFiles, "Select$" + e.Row.RowIndex);
                e.Row.Attributes["style"] = "cursor:pointer";
            }
        }

        protected void gvSimFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = gvSimFiles.SelectedIndex;
            int predictedObservedtId = int.Parse(gvSimFiles.Rows[index].Cells[0].Text);
            Response.Redirect("Details.aspx?PO_Id=" + predictedObservedtId);
        }

        protected void gvSimFiles_Sorting(object sender, GridViewSortEventArgs e)
        {
            SetSortDirection("gvSimFiles", SortDireaction_SimFilesList);

            if (SimFilesDT == null)
            {
                if (Session["SimFilesDT"] != null)
                {
                    SimFilesDT = (DataTable)Session["SimFilesDT"];
                }
            }

            if (SimFilesDT != null)
            {
                //Sort the data.
                SimFilesDT.DefaultView.Sort = e.SortExpression + " " + _sortDirection_SimFilesList;
                gvSimFiles.DataSource = SimFilesDT;

                gvSimFiles.DataBind();
                SortDireaction_SimFilesList = _sortDirection_SimFilesList;

                int sortColumnIndex = 0;
                foreach (DataControlFieldHeaderCell headerCell in gvSimFiles.HeaderRow.Cells)
                {
                    //Make sure we are displaying the correct header for all columns
                    switch (headerCell.ContainingField.SortExpression)
                    {
                        case "PredictedObservedID":
                            gvSimFiles.Columns[0].HeaderText = "PO ID";
                            break;
                        case "FileName":
                            gvSimFiles.Columns[1].HeaderText = "File Name";
                            break;
                        case "PredictedObservedTableName":
                            gvSimFiles.Columns[2].HeaderText = "Predicted Observed<br />TableName";
                            break;
                        case "PassedTests":
                            gvSimFiles.Columns[3].HeaderText = "Passed<br />Tests";
                            break;
                        case "FullFileName":
                            gvSimFiles.Columns[4].HeaderText = "Full FileName";
                            break;
                        case "AcceptedPredictedObservedDetailsID":
                            gvSimFiles.Columns[5].HeaderText = "Accepted<br />PO ID";
                            break;
                    }
                    //get the index and details for the column we are sorting
                    if (headerCell.ContainingField.SortExpression == e.SortExpression)
                    {
                        sortColumnIndex = gvSimFiles.HeaderRow.Cells.GetCellIndex(headerCell);
                    }
                }
                if (_sortDirection_SimFilesList == "ASC")
                {
                    gvSimFiles.Columns[sortColumnIndex].HeaderText = gvSimFiles.Columns[sortColumnIndex].HeaderText + "  ▲";
                }
                else
                {
                    gvSimFiles.Columns[sortColumnIndex].HeaderText = gvSimFiles.Columns[sortColumnIndex].HeaderText + "  ▼";
                }
                gvSimFiles.DataBind();

            }

        }

        #endregion


        #region Data Retreval and Binding

        private void BindApsimFilesGrid()
        {
            ApsimFileList = ApsimFilesDS.GetPullRequestsWithStatus();
            ApsimFileDT = Genfuncs.ToDataTable(ApsimFileList);

            Session["ApsimFileDT"] = ApsimFileDT;
            gvApsimFiles.DataSource = ApsimFileDT;
            gvApsimFiles.DataBind();

            AcceptStatsLog acceptedPR = AcceptStatsLogDS.GetLatestAcceptedStatsLog();
            if (acceptedPR != null)
            {
                lblAcceptedDetails.Text = string.Format("Current Accepted Stats are for Pull Request Id {0}, submitted by {1}, accepted on {2}.", acceptedPR.PullRequestId, acceptedPR.SubmitPerson, acceptedPR.LogAcceptDate.ToString("dd-MMM-yyyy HH:MM tt"));
                hfAcceptedFileCount.Value = acceptedPR.FileCount.ToString();
            }
        }


        private void BindSimFilesGrid(int pullRequestId)
        {
            lblMissing.Text = string.Empty;
            hfPullRequestId.Value = pullRequestId.ToString();

            lblPullRequestId.Text = "Simulation Files for Pull Request Id: " + pullRequestId.ToString();

            btnDifferences.Visible = true;
            btnDifferences.Text = "View Tests' Differences for Pull Request Id: " + pullRequestId.ToString();

            btnTests.Visible = true;
            btnTests.Text = "View Tests for Pull Request Id: " + pullRequestId.ToString() + " (Charts)";

            SimFilesList = ApsimFilesDS.GetSimFilesByPullRequestID(pullRequestId);
            SimFilesDT = Genfuncs.ToDataTable(SimFilesList);

            Session["SimFilesDT"] = SimFilesDT;
            gvSimFiles.DataSource = SimFilesDT;
            gvSimFiles.DataBind();

            //ClientScript.RegisterStartupScript(this.GetType(), "CreateGridHeader", "<script>CreateGridHeader('GridDataDiv_SimFiles', 'ContentPlaceHolder1_gvSimFiles', 'GridHeaderDiv_SimFiles');</script>");
        }

        private void BindSimFilesGrid(int pullRequestId, DateTime runDate, int acceptPullRequestId, int PercentPassed)
        {
            //how many files are in the accepted Pull Request Set
            //what happens if they do not match
            lblMissing.Text = string.Empty;
            hfPullRequestId.Value = pullRequestId.ToString();
            if (acceptPullRequestId > 0)
            {
                string errorMessage = ApsimFilesDS.GetFileCountDetails(pullRequestId, acceptPullRequestId);
                if (errorMessage.Length > 0)
                {
                    lblMissing.Text = "Missing FileName.TableName(s): " + errorMessage + ".";
                }
            }

            lblPullRequestId.Text = "Simulation Files for Pull Request Id: " + pullRequestId.ToString();
            if (PercentPassed == 100)
            {
                btnDifferences.Visible = false;
                btnTests.Visible = true;
            }
            else
            {
                btnDifferences.Visible = true;
                btnDifferences.Text = "View Tests' Differences for Pull Request Id: " + pullRequestId.ToString();

                btnTests.Visible = true;
                btnTests.Text = "View Tests for Pull Request Id: " + pullRequestId.ToString() + " (Charts)";
            }

            SimFilesList = ApsimFilesDS.GetSimFilesByPullRequestIDandDate(pullRequestId, runDate);
            SimFilesDT = Genfuncs.ToDataTable(SimFilesList);

            Session["SimFilesDT"] = SimFilesDT;

            gvSimFiles.DataSource = SimFilesDT;
            gvSimFiles.DataBind();

            //ClientScript.RegisterStartupScript(this.GetType(), "CreateGridHeader", "<script>CreateGridHeader('GridDataDiv_SimFiles', 'ContentPlaceHolder1_gvSimFiles', 'GridHeaderDiv_SimFiles');</script>");
        }


        protected void SetSortDirection(string gridname, string sortDirection)
        {
            if (sortDirection == "ASC")
            {
                if (gridname == "gvApsimFiles")
                {
                    _sortDirection_ApsimFileList = "DESC";
                }
                else if (gridname == "gvSimFiles")
                {
                    _sortDirection_SimFilesList = "DESC";
                }
            }
            else
            {
                if (gridname == "gvApsimFiles")
                {
                    _sortDirection_ApsimFileList = "ASC";
                }
                else if (gridname == "gvSimFiles")
                {
                    _sortDirection_SimFilesList = "ASC";
                }
            }
        }
        #endregion


        #region WebAPI Interaction

        private void UpdatePullRequestStats(string updateType, AcceptStatsLog apsimLog)
        {
            HttpClient httpClient = new HttpClient();

            string serviceUrl = ConfigurationManager.AppSettings["serviceAddress"].ToString() + "APSIM.PerformanceTests.Service/";
            httpClient.BaseAddress = new Uri(serviceUrl);
            //httpClient.BaseAddress = new Uri("http://www.apsim.info/APSIM.PerformanceTests.Service/");
#if DEBUG
            httpClient.BaseAddress = new Uri("http://localhost:53187/");
#endif
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = new HttpResponseMessage();
            if (updateType == "Accept")
            {
                response = httpClient.PostAsJsonAsync("api/acceptStats", apsimLog).Result;
            }
            else if (updateType == "Update")
            {
                response = httpClient.PostAsJsonAsync("api/updateStats", apsimLog).Result;
            }

            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
            }
        }
        #endregion

    }


}