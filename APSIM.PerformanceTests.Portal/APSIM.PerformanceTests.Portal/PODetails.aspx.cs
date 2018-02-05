using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using APSIM.PerformanceTests.Portal.Models;
using System.Web.UI.DataVisualization.Charting;
using System.Drawing;
using System.Data;

namespace APSIM.PerformanceTests.Portal
{
    public partial class PODetails : System.Web.UI.Page
    {
        #region Constants and variables

        //Constants used for the gvPOTests
        const int colVariable = 0;
        const int colTest = 1;
        const int colAccepted = 2;
        const int colCurrent = 3;
        const int colDifference = 4;
        const int colDifferencePercent = 5;
        const int colPassedTest = 6;
        const int colIsImprovement = 7;

        //Constants used for the gvPOValues
        const int colSimulationName = 0;
        const int colCurrentPredictedValue = 1;
        const int colAcceptedPredictedValue = 2;
        const int colCurrentObservedValue = 3;
        const int colAcceptedObservedValue = 4;
        const int colTableName = 5;
        const int colMatchNames = 6;
        const int colMatchValues = 7;
        const int colID = 8;


        private List<vVariable> VariableList;
        private List<PredictedObservedTest> POTestsList;
        private DataTable POTestsDT;

        private List<vCurrentAndAccepted> POValuesList;
        private DataTable POValuesDT;

        public string SortDireaction_POTestsList
        {
            get
            {
                if (ViewState["SortDireaction_POTestsList"] == null)
                    return string.Empty;
                else
                    return ViewState["SortDireaction_POTestsList"].ToString();
            }
            set
            {
                ViewState["SortDireaction_POTestsList"] = value;
            }
        }
        private string _sortDirection_POTestsList;

        public string SortDireaction_POValuesList
        {
            get
            {
                if (ViewState["SortDireaction_POValuesList"] == null)
                    return string.Empty;
                else
                    return ViewState["SortDireaction_POValuesList"].ToString();
            }
            set
            {
                ViewState["SortDireaction_POValuesList"] = value;
            }
        }
        private string _sortDirection_POValuesList;

        #endregion


        #region Page and Control Events

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                int predictedObservedId = 0;
                string variable = string.Empty;
                if (!string.IsNullOrEmpty(Request.QueryString["PO_Id"]))
                {
                    predictedObservedId = int.Parse(Request.QueryString["PO_Id"]);
                }

                if (predictedObservedId > 0)
                {
                    hfPredictedObservedID.Value = predictedObservedId.ToString();
                    PredictedObservedDetail currPODetails = PredictedObservedDS.GetByPredictedObservedID(predictedObservedId);
                    ApsimFile apsim = ApsimFilesDS.GetByID(currPODetails.ApsimFilesID);
                    hfPullRequestID.Value = apsim.PullRequestId.ToString();

                    Session["POTestsDT"] = null;
                    BindCurrentAcceptedTestsDataTable();

                    lblPullRequest.Text = "Pull Request: " + apsim.PullRequestId.ToString();
                    lblApsimFile.Text = "Apsim File: " + apsim.FileName;
                    lblPOTableName.Text = "Table: " + currPODetails.TableName + " (PO Id: " + currPODetails.ID.ToString() + ")";

                    if (!string.IsNullOrEmpty(Request.QueryString["Variable"]))
                    {
                        //LoadTimer.Enabled = false;
                        variable = Request.QueryString["Variable"].ToString();
                        BindPredictedObservedVariables(currPODetails.ID);
                        ddlVariables.SelectedValue = variable;

                        ClientScript.RegisterStartupScript(this.GetType(), "ScrollScript", "scrollToDiv();", true);
                    }
                    else
                    {
                        BindPredictedObservedVariables(currPODetails.ID);
                    }

                    //if (gvPOTests.Rows.Count > 0)
                    //{
                    //    //NOTE:  This is registered using the ClientScript (not ScriptManager), with different parameters, as this grid is NOT in an update panel
                    //    ClientScript.RegisterStartupScript(this.GetType(), "CreateGridHeader", "<script>CreateGridHeader('GridDataDiv_POTests', 'ContentPlaceHolder1_gvPOTests', 'GridHeaderDiv_POTests');</script>");
                    //}
                }
            }
            //if (gvPOValues.Rows.Count > 0)
            //{
            //    //NOTE:  This is registered using the ScriptManager (not ClientScript), with different parameters, as this grid is nested in an update panel
            //    ScriptManager.RegisterStartupScript(this, GetType(), "CreateGridHeader_POValues", "CreateGridHeader('GridDataDiv_POValues', 'ContentPlaceHolder1_gvPOValues', 'GridHeaderDiv_POValues');", true);
            //}
        }


        /// <summary>
        /// return to the first screen/page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnBack_Click(object sender, EventArgs e)
        {
            string pullrequestId = hfPullRequestID.Value.ToString();
            Response.Redirect(string.Format("Default.aspx?PULLREQUEST={0}", pullrequestId));
        }


        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            //gvPOValues.PageSize = Convert.ToInt32(ddlPageSize.SelectedItem.Text);
            //gvPOValues.DataBind();
            gvPOValues.PageIndex = 0;
            BindCurrentAcceptedValuesDataTable(); 
        }

        /// <summary>
        /// Update the Predicted Observed values graph and grid when the variable changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlVariables_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                lblError.Visible = false;
                //What Variable/Value Name are we working with
                //string variable = ddlVariables.Items[0].Text;

                //retrieve our predicted observed value
                BindCurrentAcceptedChartAndValues(true);
                //PredictedObservedDetail currPODetails = PredictedObservedDS.GetByPredictedObservedID(predictedObservedId);
                //BindCurrentAcceptedValues(variable, currPODetails);
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message.ToString();
                lblError.Visible = true;
            }
        }

        protected void txtSearch_POTests_TextChanged(object sender, EventArgs e)
        {
            FilterCurrentAcceptedTests(txtSearch_POTests.Text);
        }


        protected void txtSearch_POValues_TextChanged(object sender, EventArgs e)
        {
            FilterCurrentAcceptedValues(txtSearch_POValues.Text);
        }



        protected void gvPOTests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvPOTests.PageIndex = e.NewPageIndex;
            BindCurrentAcceptedTestsDataTable();
        }

        protected void gvPOTests_Sorting(object sender, GridViewSortEventArgs e)
        {
            SetSortDirection("gvPOTests", SortDireaction_POTestsList);
            BindCurrentAcceptedTestsDataTable();

            if (POTestsDT != null)
            {
                if (e.SortExpression == "DifferencePercent")
                {
                    POTestsDT.DefaultView.Sort = e.SortExpression + " " + _sortDirection_POTestsList;
                }
                else if (e.SortExpression == "PassedTest")
                {
                    POTestsDT.DefaultView.Sort = e.SortExpression + " " + _sortDirection_POTestsList + ", DifferencePercent DESC";
                }
                else
                {
                    POTestsDT.DefaultView.Sort = e.SortExpression + " " + _sortDirection_POTestsList + ", PassedTest, DifferencePercent DESC";
                }

                //Sort the data.
                gvPOTests.DataSource = POTestsDT;
                gvPOTests.DataBind();
                SortDireaction_POTestsList = _sortDirection_POTestsList;

                int sortColumnIndex = 0;
                foreach (DataControlFieldHeaderCell headerCell in gvPOTests.HeaderRow.Cells)
                {
                    //Make sure we are displaying the correct header for all columns
                    switch (headerCell.ContainingField.SortExpression)
                    {
                        case "Variable":
                            gvPOTests.Columns[colVariable].HeaderText = "Variable";
                            break;
                        case "Test":
                            gvPOTests.Columns[colTest].HeaderText = "Test";
                            break;
                        case "Accepted":
                            gvPOTests.Columns[colAccepted].HeaderText = "Accepted";
                            break;
                        case "Current":
                            gvPOTests.Columns[colCurrent].HeaderText = "Current";
                            break;
                        case "Difference":
                            gvPOTests.Columns[colDifference].HeaderText = "Difference";
                            break;
                        case "DifferencePercent":
                            gvPOTests.Columns[colDifferencePercent].HeaderText = "Difference<br />Percent";
                            break;
                        case "PassedTest":
                            gvPOTests.Columns[colPassedTest].HeaderText = "Passed<br />Test";
                            break;
                        case "IsImprovement":
                            gvPOTests.Columns[colIsImprovement].HeaderText = "Is<br />Improvement";
                            break;
                    }
                    //get the index and details for the column we are sorting
                    if (headerCell.ContainingField.SortExpression == e.SortExpression)
                    {
                        sortColumnIndex = gvPOTests.HeaderRow.Cells.GetCellIndex(headerCell);
                    }
                }
                //gvApsimFiles.HeaderRow.Cells[columnIndex].Controls.Add(sortImage_ApsimFileList);
                if (_sortDirection_POTestsList == "ASC")
                {
                    gvPOTests.Columns[sortColumnIndex].HeaderText = gvPOTests.Columns[sortColumnIndex].HeaderText + "  ▲";
                }
                else
                {
                    gvPOTests.Columns[sortColumnIndex].HeaderText = gvPOTests.Columns[sortColumnIndex].HeaderText + "  ▼";
                }
                gvPOTests.DataBind();
            }

        }

        /// <summary>
        /// Set the colour of the rows based on the PassedTests value (true/false)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvPOTests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                //this is the true/false PassedTest column
                if (e.Row.Cells[colPassedTest].Text.Trim().ToLower() == "false")
                {
                    e.Row.ForeColor = Color.Red;
                }
                //this is the true/false IsImprovement column
                if (e.Row.Cells[colIsImprovement].Text.Trim().ToLower() == "true")
                {
                    e.Row.ForeColor = Color.Green;
                }
            }
        }



        protected void gvPOValues_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvPOValues.PageIndex = e.NewPageIndex;
            BindCurrentAcceptedValuesDataTable();

        }
        /// <summary>
        /// Update the cell background colour of the Predicted Observed data, so that the 'Accepted'
        /// and 'Current' data is highlighted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvPOValues_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                //this refers to the "DifferenceDifference" column
                //if (double.Parse(e.Row.Cells[7].Text.Trim()) != 0)
                //{
                //    e.Row.ForeColor = Color.Red;
                //}

                e.Row.Cells[colCurrentPredictedValue].BackColor = Color.LightCyan;
                e.Row.Cells[colAcceptedPredictedValue].BackColor = Color.Gainsboro;

                e.Row.Cells[colCurrentObservedValue].BackColor = Color.LightCyan;
                e.Row.Cells[colAcceptedObservedValue].BackColor = Color.Gainsboro;

                //Compare 'Current' Predicted Value (1) against 'Accepted' Predicted (2) Value
                if ((e.Row.Cells[colCurrentPredictedValue].Text.Trim() != "&nbsp;") && (e.Row.Cells[colAcceptedPredictedValue].Text.Trim() != "&nbsp;"))
                {
                    if ((e.Row.Cells[colCurrentPredictedValue].Text.Trim().Length > 0) && (e.Row.Cells[colAcceptedPredictedValue].Text.Trim().Length > 0))
                    {
                        if ((Double?)Convert.ToDouble(e.Row.Cells[colCurrentPredictedValue].Text.Trim()) != (Double?)Convert.ToDouble(e.Row.Cells[colAcceptedPredictedValue].Text.Trim()))
                        {
                            //e.Row.ForeColor = Color.Red;
                            e.Row.Cells[colSimulationName].ForeColor = Color.Red;
                            e.Row.Cells[colCurrentPredictedValue].ForeColor = Color.Red;
                            e.Row.Cells[colAcceptedPredictedValue].ForeColor = Color.Red;
                        }
                    }
                }
                //Compare 'Current' Observed Value (3) against 'Accepted' Observed (4) Value
                if ((e.Row.Cells[colCurrentObservedValue].Text.Trim() != "&nbsp;") && (e.Row.Cells[colAcceptedObservedValue].Text.Trim() != "&nbsp;"))
                {
                    if ((e.Row.Cells[colCurrentObservedValue].Text.Trim().Length > 0) && (e.Row.Cells[colAcceptedObservedValue].Text.Trim().Length > 0))
                    {
                        if ((Double?)Convert.ToDouble(e.Row.Cells[colCurrentObservedValue].Text.Trim()) != (Double?)Convert.ToDouble(e.Row.Cells[colAcceptedObservedValue].Text.Trim()))
                        {
                            //e.Row.ForeColor = Color.Red;
                            e.Row.Cells[colSimulationName].ForeColor = Color.Red;
                            e.Row.Cells[colCurrentObservedValue].ForeColor = Color.Red;
                            e.Row.Cells[colAcceptedObservedValue].ForeColor = Color.Red;
                        }
                    }
                }
            }
        }

        protected void gvPOValues_Sorting(object sender, GridViewSortEventArgs e)
        {
            SetSortDirection("gvPOValues", SortDireaction_POValuesList);
            BindCurrentAcceptedValuesDataTable();

            if (POValuesDT != null)
            {
                //Sort the data.
                POValuesDT.DefaultView.Sort = e.SortExpression + " " + _sortDirection_POValuesList;
                gvPOValues.PageSize = Convert.ToInt32(ddlPageSize.SelectedValue);
                gvPOValues.DataSource = POValuesDT;
                gvPOValues.DataBind();
                SortDireaction_POValuesList = _sortDirection_POValuesList;

                int sortColumnIndex = 0;
                foreach (DataControlFieldHeaderCell headerCell in gvPOValues.HeaderRow.Cells)
                {
                    //Make sure we are displaying the correct header for all columns
                    switch (headerCell.ContainingField.SortExpression)
                    {
                        case "SimulationName":
                            gvPOValues.Columns[colSimulationName].HeaderText = "SimulationName";
                            break;
                        case "CurrentPredictedValue":
                            gvPOValues.Columns[colCurrentPredictedValue].HeaderText = "Current<br />Predicted<br />Value";
                            break;
                        case "AcceptedPredictedValue":
                            gvPOValues.Columns[colAcceptedPredictedValue].HeaderText = "Current<br />Observed<br />Value";
                            break;
                        case "CurrentObservedValue":
                            gvPOValues.Columns[colCurrentObservedValue].HeaderText = "Accepted<br />Predicted<br />Value";
                            break;
                        case "AcceptedObservedValue":
                            gvPOValues.Columns[colAcceptedObservedValue].HeaderText = "Accepted<br />Observed<br />Value";
                            break;
                        case "TableName":
                            gvPOValues.Columns[colTableName].HeaderText = "TableName";
                            break;
                        case "MatchNames":
                            gvPOValues.Columns[colMatchNames].HeaderText = "MatchNames";
                            break;
                        case "MatchValues":
                            gvPOValues.Columns[colMatchValues].HeaderText = "MatchValues";
                            break;
                    }
                    //get the index and details for the column we are sorting
                    if (headerCell.ContainingField.SortExpression == e.SortExpression)
                    {
                        sortColumnIndex = gvPOValues.HeaderRow.Cells.GetCellIndex(headerCell);
                    }
                }
                //gvApsimFiles.HeaderRow.Cells[columnIndex].Controls.Add(sortImage_ApsimFileList);
                if (_sortDirection_POValuesList == "ASC")
                {
                    gvPOValues.Columns[sortColumnIndex].HeaderText = gvPOValues.Columns[sortColumnIndex].HeaderText + "  ▲";
                }
                else
                {
                    gvPOValues.Columns[sortColumnIndex].HeaderText = gvPOValues.Columns[sortColumnIndex].HeaderText + "  ▼";
                }
                gvPOValues.DataBind();
            }
        }


        /// <summary>
        /// This is used to ensure that the variable data (for the graph and grid) is load after the page_load event. (Lazy Loading)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void LoadTimer_Tick(object sender, EventArgs e)
        {
            //perhaps load the first item
            if (ddlVariables.Items.Count > 0)
            {
                //int predictedObservedId = int.Parse(Convert.ToString(hfPredictedObservedID.Value));
                //string variable = ddlVariables.Items[0].Text;
                //BindCurrentAcceptedValues(variable, predictedObservedId);
                BindCurrentAcceptedChartAndValues(true);
                LoadTimer.Enabled = false;
            }
        }


        #endregion

        #region Data Retreval and Binding
        /// <summary>
        /// Get the variables that are being reported on for this predicted observed data, and display them in the dropdown
        /// </summary>
        /// <param name="predictedObservedId"></param>
        private void BindPredictedObservedVariables(int predictedObservedId)
        {
            VariableList = PredictedObservedDS.GetVariablesByPredictedObservedID(predictedObservedId);
            ddlVariables.DataSource = VariableList;
            ddlVariables.DataBind();
        }



        private void BindCurrentAcceptedValuesDataTable()
        {
            if (POValuesDT == null)
            {
                if (Session["POValuesDT"] != null)
                {
                    POValuesDT = (DataTable)Session["POValuesDT"];
                }
            }
            if (POValuesDT == null)
            {
                BindCurrentAcceptedChartAndValues(false);
            }

            if (POValuesDT != null)
            {
                gvPOValues.DataSource = POValuesDT;
                gvPOValues.PageSize = Convert.ToInt32(ddlPageSize.SelectedValue);
                gvPOValues.DataBind();
                UpdatePanel2.Update();
            }
        }

        private void FilterCurrentAcceptedValues(string filter)
        {
            //get back the original datatable (not the one we may have in the Session object)
            BindCurrentAcceptedChartAndValues(false);

            if (filter.Length > 0)
            {
                DataView view = POValuesDT.DefaultView;
                view.RowFilter = " SimulationName Like '%" + filter + "%' ";
                POValuesDT = view.ToTable();
                Session["POValuesDT"] = POValuesDT;

                //And display the combined data and display it in the grid
            }
            gvPOValues.DataSource = POValuesDT;
            gvPOValues.PageSize = Convert.ToInt32(ddlPageSize.SelectedValue);

            gvPOValues.DataBind();
            UpdatePanel2.Update();
        }

        private void BindCurrentAcceptedChartAndValues(bool updateChart)
        {
            string variable = ddlVariables.SelectedItem.Text;
            BindCurrentAcceptedChartAndValues(updateChart, variable);
        }

        /// <summary>
        /// Get the PredictedObserved Details based on the Predicted Observed Id, and then call
        /// BindCurrentAcceptedValues, passing the requrid details
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="predictedObservedId"></param>
        private void BindCurrentAcceptedChartAndValues(bool updateChart, string variable)
        {
            //string variable = ddlVariables.SelectedItem.Text;
            int predictedObservedId = int.Parse(Convert.ToString(hfPredictedObservedID.Value));

            PredictedObservedDetail currPODetails = PredictedObservedDS.GetByPredictedObservedID(predictedObservedId);
            POValuesList = BindCurrentAcceptedValuesssssss(variable, currPODetails);

            POValuesDT = Genfuncs.ToDataTable(POValuesList);
            Session["POValuesDT"] = POValuesDT;

            gvPOValues.DataSource = POValuesDT;
            gvPOValues.PageSize = Convert.ToInt32(ddlPageSize.SelectedValue);

            gvPOValues.DataBind();

            UpdatePanel2.Update();

            if (updateChart == true)
            {
                BindCurrentAcceptedChart(variable, POValuesList);
                UpdatePanel4.Update();
            }
        }


        private List<vCurrentAndAccepted> BindCurrentAcceptedValuesssssss(String variable, PredictedObservedDetail currPODetails)
        {
            List<vCurrentAndAccepted> POCurrentValuesList;

            //some of the older records may not have this yet.  So find it the old way.
            if ((currPODetails.AcceptedPredictedObservedDetailsID == null) || (currPODetails.AcceptedPredictedObservedDetailsID <= 0))
            {
                //Retrieve the curresponding (parent) ApsimFile for this PredictedObservedDetail
                ApsimFile apsimFile = ApsimFilesDS.GetByID(currPODetails.ApsimFilesID);

                //get the Pull Request Id for the lastest released pull request that is not the current one
                int acceptPullRequestId = ApsimFilesDS.GetLatestMergedPullRequestId(apsimFile.PullRequestId);

                //get the PredictedObservedDetail.ID for the records that match our current record 'matching' criteria
                currPODetails.AcceptedPredictedObservedDetailsID = PredictedObservedDS.GetIDByMatchingDetails(acceptPullRequestId, apsimFile.FileName, currPODetails.TableName,
                    currPODetails.PredictedTableName, currPODetails.ObservedTableName, currPODetails.FieldNameUsedForMatch);
            }


            POCurrentValuesList = PredictedObservedDS.GetCurrentAcceptedValuesWithNulls(variable, currPODetails.ID, (int)currPODetails.AcceptedPredictedObservedDetailsID);
            //POCurrentValuesList = PredictedObservedDS.GetCurrentAcceptedValues(variable, currPODetails.ID, (int)currPODetails.AcceptedPredictedObservedDetailsID);
            //get the data to be displayed in the chart and grid
            //POValuesList = PredictedObservedDS.GetCurrentAcceptedValues(variable, currPODetails.ID, (int)currPODetails.AcceptedPredictedObservedDetailsID);

            //POValuesDT = Genfuncs.ToDataTable(POCurrentValuesList);

            return POCurrentValuesList;

        }

        //private DataTable CreatePOValuesDT(List<vCurrentAndAccepted> POValuesList)
        //{
        //    DataTable myTable = new DataTable();

        //    myTable.Columns.Add("ID", System.Type.GetType("System.Int32"));
        //    myTable.Columns.Add("TableName", System.Type.GetType("System.String"));
        //    myTable.Columns.Add("SimulationName", System.Type.GetType("System.String"));
        //    myTable.Columns.Add("MatchNames", System.Type.GetType("System.String"));
        //    myTable.Columns.Add("MatchValues", System.Type.GetType("System.String"));
        //    //myTable.Columns.Add("ValueName", System.Type.GetType("System.String"));

        //    DataColumn col;
        //    col = new DataColumn("CurrentPredictedValue", System.Type.GetType("System.Double"));
        //    col.AllowDBNull = true;
        //    myTable.Columns.Add(col);

        //    col = new DataColumn("CurrentObservedValue", System.Type.GetType("System.Double"));
        //    col.AllowDBNull = true;
        //    myTable.Columns.Add(col);

        //    col = new DataColumn("AcceptedPredictedValue", System.Type.GetType("System.Double"));
        //    col.AllowDBNull = true;
        //    myTable.Columns.Add(col);

        //    col = new DataColumn("AcceptedObservedValue", System.Type.GetType("System.Double"));
        //    col.AllowDBNull = true;
        //    myTable.Columns.Add(col);

        //    DataRow row;
        //    foreach (vCurrentAndAccepted item in POValuesList)
        //    {
        //        row = myTable.NewRow();
        //        row["ID"] = item.ID;

        //        row["TableName"] = item.TableName;
        //        row["SimulationName"] = item.SimulationName;
        //        row["MatchNames"] = item.MatchNames;
        //        row["MatchValues"] = item.MatchValues;
        //        //row["ValueName"] = item.ValueName;

        //        double outValue = 0;
        //        try
        //        {
        //            if (item.CurrentPredictedValue != null)
        //            {
        //                outValue = double.Parse(item.CurrentPredictedValue.ToString());
        //                row["CurrentPredictedValue"] = outValue;
        //            }
        //            else
        //            {
        //                row["CurrentPredictedValue"] = DBNull.Value;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            row["CurrentPredictedValue"] = DBNull.Value;
        //        }

        //        try
        //        {
        //            if (item.CurrentObservedValue != null)
        //            {
        //                outValue = double.Parse(item.CurrentObservedValue.ToString());
        //                row["CurrentObservedValue"] = outValue;
        //            }
        //            else
        //            {
        //                row["CurrentObservedValue"] = DBNull.Value;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            row["CurrentObservedValue"] = DBNull.Value;
        //        }

        //        try
        //        {
        //            if (item.AcceptedPredictedValue != null)
        //            {
        //                outValue = double.Parse(item.AcceptedPredictedValue.ToString());
        //                row["AcceptedPredictedValue"] = outValue;
        //            }
        //            else
        //            {
        //                row["AcceptedPredictedValue"] = DBNull.Value;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            row["AcceptedPredictedValue"] = DBNull.Value;
        //        }

        //        try
        //        {
        //            if (item.AcceptedObservedValue != null)
        //            {
        //                outValue = double.Parse(item.AcceptedObservedValue.ToString());
        //                row["AcceptedObservedValue"] = outValue;
        //            }
        //            else
        //            {
        //                row["AcceptedObservedValue"] = DBNull.Value;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            row["AcceptedObservedValue"] = DBNull.Value;
        //        }

        //        myTable.Rows.Add(row);
        //    }

        //    return myTable;
        //}

        /// <summary>
        /// Retrieve all of the Predicted Observed values (including simulation details), for the specified variable name, 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="currPODetails"></param>
        private void BindCurrentAcceptedChart(string variable, List<vCurrentAndAccepted> POValuesList)
        {
            lblValues.Text = "Current and Accepted values for " + variable;


            chartPODetails.Titles["chartTitle"].Text = "Current vs Accepted " + variable;

            //Now display it in the chart
            Series acceptedSeries = chartPODetails.Series["Accepted"];
            acceptedSeries.Points.DataBind(POValuesList, "AcceptedObservedValue", "AcceptedPredictedValue", "");

            Series currentSeries = chartPODetails.Series["Current"];
            currentSeries.Points.DataBind(POValuesList, "CurrentObservedValue", "CurrentPredictedValue", "");

            double maxValue = 0;
            double value = 0;
            var maxObject = POValuesList.OrderByDescending(item => item.AcceptedObservedValue).First();
            double.TryParse(maxObject.AcceptedObservedValue.ToString(), out value);
            if (value > maxValue) maxValue = value;

            maxObject = POValuesList.OrderByDescending(item => item.AcceptedPredictedValue).First();
            double.TryParse(maxObject.AcceptedPredictedValue.ToString(), out value);
            if (value > maxValue) maxValue = value;

            maxObject = POValuesList.OrderByDescending(item => item.CurrentObservedValue).First();
            double.TryParse(maxObject.CurrentObservedValue.ToString(), out value);
            if (value > maxValue) maxValue = value;

            maxObject = POValuesList.OrderByDescending(item => item.CurrentPredictedValue).First();
            double.TryParse(maxObject.CurrentPredictedValue.ToString(), out value);
            if (value > maxValue) maxValue = value;

            Series slope = chartPODetails.Series["Slope"];
            slope.Points.AddXY(0, 0);
            slope.Points.AddXY(maxValue, maxValue);


            //if (gvPOValues.Rows.Count > 0)
            //{
            //    //NOTE:  This is registered using the ScriptManager (not ClientScript), with different parameters, as this grid is nested in an update panel
            //    ScriptManager.RegisterStartupScript(this, GetType(), "CreateGridHeader_POValues", "CreateGridHeader('GridDataDiv_POValues', 'ContentPlaceHolder1_gvPOValues', 'GridHeaderDiv_POValues');", true);
            //}
        }


        private void BindCurrentAcceptedTestsDataTable()
        {
            if (POTestsDT == null)
            {
                if (Session["POTestsDT"] != null)
                {
                    POTestsDT = (DataTable)Session["POTestsDT"];
                }
            }
            if (POTestsDT == null)
            {
                BindCurrentAcceptedTests();
            }
            //And display the combined data and display it in the grid
            gvPOTests.DataSource = POTestsDT;
            gvPOTests.DataBind();
        }

        /// <summary>
        /// Retrieve the Tests data for the specified predicted observed id
        /// </summary>
        /// <param name="predictedObservedId"></param>
        private void BindCurrentAcceptedTests()
        {
            int predictedObservedId = int.Parse(hfPredictedObservedID.Value.ToString());
            POTestsList = PredictedObservedDS.GetCurrentAcceptedTestsSubset(predictedObservedId);

            POTestsDT = Genfuncs.ToDataTable(POTestsList);
            Session["POTestsDT"] = POTestsDT;

            //if (gvPOTests.Rows.Count > 0)
            //{
            //    //NOTE:  This is registered using the ClientScript (not ScriptManager), with different parameters, as this grid is NOT in an update panel
            //    ClientScript.RegisterStartupScript(this.GetType(), "CreateGridHeader", "<script>CreateGridHeader('GridDataDiv_POTests', 'ContentPlaceHolder1_gvPOTests', 'GridHeaderDiv_POTests');</script>");
            //}
        }

        private void FilterCurrentAcceptedTests(string filter)
        {
            //get back the original datatable (not the one we may have in the Session object)
            BindCurrentAcceptedTests();

            if (filter.Length > 0)
            {
                DataView view = POTestsDT.DefaultView;
                view.RowFilter = " Test Like '%" + filter + "%' OR Variable Like '%" + filter + "%' ";
                POTestsDT = view.ToTable();
                Session["POTestsDT"] = POTestsDT;

                //And display the combined data and display it in the grid
            }
            gvPOTests.DataSource = POTestsDT;
            gvPOTests.DataBind();
        }



        protected void SetSortDirection(string gridname, string sortDirection)
        {
            if (sortDirection == "ASC")
            {
                if (gridname == "gvPOTests")
                {
                    _sortDirection_POTestsList = "DESC";
                }
                else
                {
                    _sortDirection_POValuesList = "DESC";
                }
            }
            else
            {
                if (gridname == "gvPOTests")
                {
                    _sortDirection_POTestsList = "ASC";
                }
                else
                {
                    _sortDirection_POValuesList = "ASC";

                }
            }
        }

        #endregion

    }
}