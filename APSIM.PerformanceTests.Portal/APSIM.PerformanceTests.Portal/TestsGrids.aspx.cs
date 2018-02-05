using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.DataVisualization.Charting;
using System.Data;
using APSIM.PerformanceTests.Portal.Models;


namespace APSIM.PerformanceTests.Portal
{
    public partial class TestsGrids : System.Web.UI.Page
    {
        #region Constants and variables
        private int _TableColCount = 0;
        #endregion

        #region Page and Control Events

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["PULLREQUEST"] != null)
            {
                int pullRequestId = int.Parse(Request.QueryString["PULLREQUEST"].ToString());
                hfPullRequestID.Value = pullRequestId.ToString();
                lblPullRequest.Text = "Pull Request Id: " + pullRequestId.ToString();
                RetrieveDataAndBindGrids();
            }
            else
            {
                hfPullRequestID.Value = "2236";
                lblPullRequest.Text = "Pull Request Id: 2236";
                RetrieveDataAndBindGrids();
            }

        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
            string pullrequestId = hfPullRequestID.Value.ToString();
            Response.Redirect(string.Format("Default.aspx?PULLREQUEST={0}", pullrequestId));
        }

        //private void myChart_Click(object sender, System.Web.UI.WebControls.ImageMapEventArgs e)
        //{
        //    string PO_Id = e.PostBackValue;
        //    Response.Redirect(string.Format("PODetails.aspx?PO_Id={0}", PO_Id));
        //}

        #endregion

        #region Data Retreval and Binding

        private void RetrieveDataAndBindGrids()
        {
            int pullRequestId = int.Parse(hfPullRequestID.Value.ToString());
            List<vPredictedObservedTests> POTestsList = PredictedObservedDS.GetCurrentAcceptedTestsFiltered(pullRequestId);

            bool newVariable = false;
            string holdFileName = string.Empty;
            string holdTableName = string.Empty;
            string holdVariable = string.Empty;
            string currPO_ID = string.Empty;
            bool firstVariable = true;
            bool firstFileName = true;

            string ArrowChars = "&#10004;";
            string CrossChars = "&#10008;";
            string diff = string.Empty;

            string N_Current = string.Empty, N_Accepted = string.Empty;
            string RMSE_Current = string.Empty, RMSE_Accepted = string.Empty, RMSE_Difference = string.Empty;
            string NSE_Current = string.Empty, NSE_Accepted = string.Empty, NSE_Difference = string.Empty;
            string Bias_Current = string.Empty, Bias_Accepted = string.Empty, Bias_Difference = string.Empty;
            string RSR_Current = string.Empty, RSR_Accepted = string.Empty, RSR_Difference = string.Empty;


            Table table = new Table();
            table.CssClass = "TGR_Table";
            table.Rows.Add(DefineTableStructure());

            foreach (vPredictedObservedTests item in POTestsList)
            {

                //this is the first instance
                newVariable = false;
                if (item.Variable != holdVariable) { newVariable = true; }


                if (newVariable == true)
                {
                    //we need to output details
                    //have we output the actual file name details
                    //NEED TO ALLOW FOR FIRST TIME OUTPUT
                    if (firstFileName == true)
                    {
                        table.Rows.Add(DefineSimNameRow(item.FileName));
                        holdFileName = item.FileName;
                        firstVariable = true;
                        table.Rows.Add(DefineTableRow(item.TableName));
                        holdTableName = item.TableName;
                        firstFileName = false;
                    }

                    if (holdVariable != string.Empty)
                    {
                        //then we can print the variable details
                        table.Rows.Add(DefineTableVariableRow(holdVariable, currPO_ID, N_Current, N_Accepted, RMSE_Current, RMSE_Accepted, RMSE_Difference, NSE_Current, NSE_Accepted, NSE_Difference, RSR_Current, RSR_Accepted, RSR_Difference));
                        N_Current = string.Empty;
                        N_Accepted = string.Empty;
                        RMSE_Current = string.Empty;
                        RMSE_Accepted = string.Empty;
                        RMSE_Difference = string.Empty;
                        NSE_Current = string.Empty;
                        NSE_Accepted = string.Empty;
                        NSE_Difference = string.Empty;
                        RSR_Current = string.Empty;
                        RSR_Accepted = string.Empty;
                        RSR_Difference = string.Empty;
                        currPO_ID = string.Empty;
                    }

                    if (item.FileName != holdFileName)
                    {
                        table.Rows.Add(DefineBlankRow());
                        table.Rows.Add(DefineSimNameRow(item.FileName));
                        holdFileName = item.FileName;
                        firstVariable = true;

                        //alway output the PO table when we change filename
                        table.Rows.Add(DefineTableRow(item.TableName));
                        holdTableName = item.TableName;

                    }

                    //have we output the table name details
                    if (item.TableName != holdTableName)
                    {
                        table.Rows.Add(DefineTableRow(item.TableName));
                        holdTableName = item.TableName;
                        firstVariable = false;
                    }
                    //holdTableName = item.TableName;

                    firstVariable = false;
                }


                bool isImprovement = false;
                if (item.IsImprovement != null)
                {
                    isImprovement = (bool)item.IsImprovement;
                }

                bool passedTest = false;
                if (item.PassedTest != null)
                {
                    passedTest = (bool)item.PassedTest;
                }

                diff = string.Empty;
                if (isImprovement == true)
                {
                    diff = "<span style=\"font-weight: bold; color: Green;\">" + ArrowChars + "</span>";
                }
                else if (passedTest != true)
                {
                    diff = "<span style=\"font-weight: bold; color: Red;\">" + CrossChars + "</span>";
                }

                switch (item.Test)
                {
                    case "RMSE":
                        RMSE_Current = FormatSignificant(item.Current, 6, false);
                        RMSE_Accepted = FormatSignificant(item.Accepted, 6, true);
                        RMSE_Difference = diff;
                        break;
                    case "NSE":
                        NSE_Current = RoundValues(item.Current, 3, false);
                        NSE_Accepted = RoundValues(item.Accepted, 3, true);
                        if (item.Current != null)
                        {
                            if (((double)item.Current > 0.75) && ((double)item.Current <= 1.00)) { diff = diff + "***"; }
                            if (((double)item.Current > 0.65) && ((double)item.Current <= 0.75)) { diff = diff + "**"; }
                            if (((double)item.Current > 0.50) && ((double)item.Current <= 0.65)) { diff = diff + "*"; }
                        }
                        NSE_Difference = diff;
                        break;
                    case "n":
                        N_Current = RoundValues(item.Current, 0, false);
                        N_Accepted = RoundValues(item.Accepted, 0, true);
                        break;
                    case "RSR":
                        RSR_Current = RoundValues(item.Current, 3, false);
                        RSR_Accepted = RoundValues(item.Accepted, 3, true);
                        if (item.Current != null)
                        {
                            if (((double)item.Current >= 0.00) && ((double)item.Current <= 0.50)) { diff = diff + "***"; }
                            if (((double)item.Current > 0.50) && ((double)item.Current <= 0.60)) { diff = diff + "**"; }
                            if (((double)item.Current > 0.60) && ((double)item.Current <= 0.70)) { diff = diff + "*"; }
                        }
                        RSR_Difference = diff;
                        break;
                }
                //we need this for the hyperlink
                currPO_ID = item.PredictedObservedDetailsID.ToString();
                holdVariable = item.Variable;
            }

            //output details here
            //have we output the table name details
            if (holdVariable != string.Empty)
            {
                //then we can print the variable details
                table.Rows.Add(DefineTableVariableRow(holdVariable, currPO_ID, N_Current, N_Accepted, RMSE_Current, RMSE_Accepted, RMSE_Difference, NSE_Current, NSE_Accepted, NSE_Difference, RSR_Current, RSR_Accepted, RSR_Difference));
            }
            phGrids.Controls.Add(table);
            AddPerformanceRatingTable();
        }


        private TableRow DefineTableStructure()
        {
            TableRow row = new TableRow();
            _TableColCount = 0;

            TableCell cell = new TableCell();
            cell.CssClass = "TGCell_SimHeader";
            row.Cells.Add(cell);
            _TableColCount += 1;

            cell = new TableCell();
            cell.CssClass = "TGCell_SimHeader";
            row.Cells.Add(cell);
            _TableColCount += 1;

            cell = new TableCell();
            cell.CssClass = "TGCell_Variable";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //these are for 'n'
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TG_CellTick";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            row.Cells.Add(cell);
            _TableColCount += 1;
            
            //these are for 'RMSE'
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_RMSE";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_RMSE";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TGCell_Tick";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //these are for 'NSE'
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TGCell_Tick";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //these are for 'RSR'
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            row.Cells.Add(cell);
            _TableColCount += 1;
            cell = new TableCell();
            cell.CssClass = "TGCell_Tick";
            row.Cells.Add(cell);
            _TableColCount += 1;

            //now ad the row to the table
            return row;
        }


        private TableRow DefineBlankRow()
        {
            TableRow row = new TableRow();

            TableCell cell = new TableCell();
            cell.ColumnSpan = _TableColCount;
            cell.Text = "&nbsp;";
            row.Cells.Add(cell);
            //now ad the row to the table
            return row;

        }
        private TableRow DefineSimNameRow(string simName)
        {
            TableRow row = new TableRow();

            TableCell cell = new TableCell();
            cell.ColumnSpan = _TableColCount;
            cell.CssClass = "TGCell_SimName";
            cell.Text = simName;
            row.Cells.Add(cell);

            //now ad the row to the table
            return row;
        }
        private TableRow DefineTableRow(string POtableName)
        {
            TableRow row = new TableRow();

            TableCell cell = new TableCell();
            cell.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.CssClass = "TGCell_POHeader";
            cell.ColumnSpan = 2;
            cell.Text = POtableName;
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace_Header";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.ColumnSpan = 2;
            cell.CssClass = "TGCell_TestHeader";
            cell.Text = "n";
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace_Header";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.ColumnSpan = 3;
            cell.CssClass = "TGCell_TestHeader";
            cell.Text = "RMSE";
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace_Header";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.ColumnSpan = 3;
            cell.CssClass = "TGCell_TestHeader";
            cell.Text = "NSE";
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace_Header";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.ColumnSpan = 3;
            cell.CssClass = "TGCell_TestHeader";
            cell.Text = "RSR";
            row.Cells.Add(cell);

            //now ad the row to the table
            return row;
        }

        private TableRow DefineTableVariableRow(string variable, string currPO_ID, string currentN, string acceptedN, string currentRMSE, string acceptRMSE, string diffRMSE, string currentNSE, string acceptNSE, string diffNSE,
           string currentRSR, string acceptRSR, string diffRSR)
        {
            TableRow row = new TableRow();
            row.CssClass = "TGR_Cell";

            //this column holds the TableName
            TableCell cell = new TableCell();
            cell.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
            row.Cells.Add(cell);

            //this column holds the variable Name
            cell = new TableCell();
            cell.CssClass = "TGCell_Variable";
            cell.Text = variable;
            //Need to add a link in here
            //acceptedSeries.Url = string.Format("PODetails.aspx?PO_Id={0}", currPO_ID);
            // Create a Hyperlink Web server control and add it to the cell.
            System.Web.UI.WebControls.HyperLink h = new HyperLink();
            h.Text = variable;
            h.NavigateUrl = string.Format("PODetails.aspx?PO_Id={0}&Variable={1}", currPO_ID, variable);
            cell.Controls.Add(h);
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            //n
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            cell.Text = currentN;
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "TG_CellTick";
            cell.Text = acceptedN;
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            //RMSE
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_RMSE";
            cell.Text = currentRMSE;
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_RMSE";
            cell.Text = acceptRMSE;
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "TGCell_Tick";
            cell.Text = diffRMSE;
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            //NSE
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            cell.Text = currentNSE;
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other"; //"TGCell_Num_OtherL";  
            cell.Text = acceptNSE;
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.CssClass = "TGCell_Tick";
            cell.Text = diffNSE;
            row.Cells.Add(cell);

            //this is for a spacer cell
            cell = new TableCell();
            cell.CssClass = "TG_CellSpace";
            cell.Text = "&nbsp;&nbsp;";
            row.Cells.Add(cell);

            //RSR
            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other";
            cell.Text = currentRSR;
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.CssClass = "TGCell_Num_Other"; //"TGCell_Num_OtherL";  
            cell.Text = acceptRSR;
            row.Cells.Add(cell);

            cell = new TableCell();
            cell.CssClass = "TGCell_Tick";
            cell.Text = diffRSR;
            row.Cells.Add(cell);

            //now ad the row to the table
            return row;
        }



        private string RoundValues(double? dvalue, int digits, bool isAccepted)
        {
            string returnStr = string.Empty;
            try
            {
                returnStr = Math.Round((double)dvalue, digits).ToString();
            }
            catch (Exception)
            {
            }
            if (isAccepted == true)
            {
                returnStr = "(" + returnStr + ")";
            }
            //add a space so that we always have something
            return "&nbsp;" + returnStr;
        }

        private string FormatSignificant(double? dvalue, int digits, bool isAccepted)
        {
            string returnStr = string.Empty;
            try
            {
                //get the number of digits before the decimal place
                int decPos = dvalue.ToString().IndexOf(".");

                //then round the number up based on the number of decimal places we can have
                //ie 62.3422623  should become 62.3423  if decPos = 2, then round dvalue to 4 decimal places
                //   3642.45678  should become 3642.57  if decPos = 4, then round dvalue to 2 decimal places
                //   3642        should become 3642.00  if decPos = 0, then round dvalue to 2 decimal places
                if (decPos > 0)
                {
                    returnStr = Math.Round((double)dvalue, digits - decPos).ToString();
                }
                else
                {
                    returnStr = Math.Round((double)dvalue, digits).ToString();
                }

                if (returnStr.Length >= (digits + 1))
                {
                    //new trucate the string to get the correct number of digits
                    returnStr = returnStr.Substring(0, digits + 1);
                }
                else //if (returnStr.Length < (digits + 1))
                {
                    returnStr = returnStr.PadRight(7, '0');
                }
                
            }
            catch (Exception)
            {
            }
            if (isAccepted == true)
            {
                returnStr = "(" + returnStr + ")";
            }
            //add a space so that we always have something
            return "&nbsp;" + returnStr;
        }


        private void AddPerformanceRatingTable()
        {
            Table table = new Table();
            table.CssClass = "PR_Table";

            TableRow row = new TableRow();
            TableCell cell = new TableCell();
            cell.ColumnSpan = 4;
            cell.Text = "Table 1.General performance ratings for recommended statistics.";
            row.Cells.Add(cell);
            table.Rows.Add(row);

            row = new TableRow();
            cell = new TableCell();
            cell.Text = "&nbsp;";
            cell.CssClass = "PR_Cell_Header PR_Cell1";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.Text = "Performance Rating";
            cell.CssClass = "PR_Cell_Header PR_Cell2";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.Text = "RSR";
            cell.CssClass = "PR_Cell_Header PR_Cell2";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.Text = "NSE";
            cell.CssClass = "PR_Cell_Header PR_Cell2";
            row.Cells.Add(cell);
            row.CssClass = "PR_TopRow";
            table.Rows.Add(row);


            row = new TableRow();
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "***";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "Very Good";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "0.00 ≤ RSR ≤ 0.50";  // ≥  ≠
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "0.75 < NSE ≤ 1.00";
            row.Cells.Add(cell);
            row.CssClass = "PR_2ndRow";
            table.Rows.Add(row);

            row = new TableRow();
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "**";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "Good";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "0.50 < RSR ≤ 0.60";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "0.65 < NSE ≤ 0.75";
            row.Cells.Add(cell);
            table.Rows.Add(row);

            row = new TableRow();
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "*";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "Satisfactory";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "0.60 < RSR ≤ 0.70";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "0.50 < NSE ≤ 0.65";
            row.Cells.Add(cell);
            table.Rows.Add(row);

            row = new TableRow();
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "&nbsp;";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "Unsatisfactory";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "RSR > 0.70";
            row.Cells.Add(cell);
            cell = new TableCell();
            cell.CssClass = "PR_Cell_Other";
            cell.Text = "NSE ≤ 0.50";
            row.Cells.Add(cell);
            row.CssClass = "PR_EndRow";
            table.Rows.Add(row);

            phGrids.Controls.Add(new LiteralControl("<br />"));
            phGrids.Controls.Add(new LiteralControl("<br />"));
            phGrids.Controls.Add(table);
            phGrids.Controls.Add(new LiteralControl("<br />"));
        }
        #endregion

    }
}