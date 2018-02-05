using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.DataVisualization.Charting;
using APSIM.PerformanceTests.Portal.Models;


namespace APSIM.PerformanceTests.Portal
{
    public partial class TestsCharts : System.Web.UI.Page
    {
        #region Constants and variables
        #endregion

        #region Page and Control Events

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["PULLREQUEST"] != null)
            {
                int pullRequestId = int.Parse(Request.QueryString["PULLREQUEST"].ToString());
                hfPullRequestID.Value = pullRequestId.ToString();
                lblPullRequest.Text = "Pull Request Id: " + pullRequestId.ToString();
                RetrieveDataAndBindCharts();
            }
            else
            {
                hfPullRequestID.Value = "2208";
                lblPullRequest.Text = "Pull Request Id: 2208";
                RetrieveDataAndBindCharts();
            }

        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
            string pullrequestId = hfPullRequestID.Value.ToString();
            Response.Redirect(string.Format("Default.aspx?PULLREQUEST={0}", pullrequestId));
        }


        private void myChart_Click(object sender, System.Web.UI.WebControls.ImageMapEventArgs e)
        {
            string PO_Id = e.PostBackValue;
            Response.Redirect(string.Format("PODetails.aspx?PO_Id={0}", PO_Id));
        }

        #endregion

        #region Data Retreval and Binding


        private void RetrieveDataAndBindCharts()
        {
            int pullRequestId = int.Parse(hfPullRequestID.Value.ToString());
            List<vPredictedObservedTests> POTestsList = PredictedObservedDS.GetCurrentAcceptedTests(pullRequestId);

            bool newchart = false;
            string holdFileName = string.Empty;
            string holdTableName = string.Empty;
            string holdVariable = string.Empty;
            string holdTitle = string.Empty;
            string holdPO_Id = string.Empty;
            string tooltip = string.Empty;
            //string[] xValues = { "September", "October", "November", "December" };
            //double[] yValues = { 15, 60, 12, 13 };


            List<string> AcceptedXValues = new List<string>();
            List<double> AcceptedYValues = new List<double>(); 
            List<Color> AcceptedColours = new List<Color>();
            List<string> CurrentXValues = new List<string>();
            List<double> CurrentYValues = new List<double>();
            List<Color> CurrentColours = new List<Color>();


            Color currColour, accColour;
            int chartNo = 0;

            foreach (vPredictedObservedTests item in POTestsList)
            {

                //this is the first instance
                if (holdFileName == string.Empty)
                {
                    holdFileName = item.FileName;
                    UpdatePlaceHolderWithTitle(phCharts, item.FileName, false);

                    holdTableName = item.TableName;
                    holdVariable = item.Variable;

                    holdPO_Id = item.PredictedObservedDetailsID.ToString();
                }

                newchart = false;
                if (item.FileName != holdFileName) { newchart = true; }
                if (item.TableName != holdTableName) { newchart = true; }
                if (item.Variable != holdVariable) { newchart = true; }

                if (newchart == true)
                {
                    if ((AcceptedXValues.Count > 0) || (CurrentXValues.Count > 0))
                    {
                        chartNo += 1;
                        tooltip = string.Format("{0} - {1}", holdFileName, holdTableName);
                        CreateCharts(chartNo, holdVariable, holdPO_Id, tooltip, AcceptedColours.ToArray(), AcceptedXValues.ToArray(), AcceptedYValues.ToArray(),
                            CurrentColours.ToArray(), CurrentXValues.ToArray(), CurrentYValues.ToArray());
                    }

                    if (item.FileName != holdFileName)
                    {
                        UpdatePlaceHolderWithTitle(phCharts, item.FileName, true);
                    }

                    //clear these for next time
                    AcceptedXValues = new List<string>();
                    AcceptedYValues = new List<double>();
                    AcceptedColours = new List<Color>();
                    CurrentXValues = new List<string>();
                    CurrentYValues = new List<double>();
                    CurrentColours = new List<Color>();
                    holdFileName = item.FileName;
                    holdTableName = item.TableName;
                    holdVariable = item.Variable;
                    holdPO_Id = item.PredictedObservedDetailsID.ToString();
                }

                if ((item.Test != "n") && (item.Test != "RMSE"))
                {

                    if (item.Accepted != null)
                    {
                        AcceptedXValues.Add(item.Test);
                        AcceptedYValues.Add((double)item.Accepted);
                        if (Math.Abs((double)item.Accepted) > 1)
                        {
                            accColour = Color.Black;
                        }
                        else
                        {
                            accColour = Color.Gray;
                        }
                        AcceptedColours.Add(accColour);
                    }

                    if (item.Current != null)
                    {
                        try
                        {
                            CurrentXValues.Add(item.Test);
                            CurrentYValues.Add((double)item.Current);
                            if (Math.Abs((double)item.Current) > 1)
                            {
                                currColour = Color.Orange;
                            }
                            else if (((bool)item.IsImprovement) || ((bool)item.PassedTest))
                            {
                                currColour = Color.Green;
                            }
                            else if (item.Accepted != null) 
                            {
                                if ((double)item.Current == (double)item.Accepted)
                                {
                                    currColour = Color.Gray;
                                }
                                else
                                {
                                    currColour = Color.Red;
                                }
                            }
                            else
                            {
                                currColour = Color.Red;
                            }
                            CurrentColours.Add(currColour);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            if ((AcceptedXValues.Count > 0) || (CurrentXValues.Count > 0))
            {
                chartNo += 1;
                tooltip = string.Format("{0} - {1}", holdFileName, holdTableName);
                CreateCharts(chartNo, holdVariable, holdPO_Id, tooltip, AcceptedColours.ToArray(), AcceptedXValues.ToArray(), AcceptedYValues.ToArray(),
                    CurrentColours.ToArray(), CurrentXValues.ToArray(), CurrentYValues.ToArray());
            }
        }

        private void UpdatePlaceHolderWithTitle(PlaceHolder ph, string name, bool addSpace)
        {
            if (addSpace == true)
            {
                phCharts.Controls.Add(new LiteralControl("<br />"));
            }
            phCharts.Controls.Add(new LiteralControl("<hr>"));
            Label lblFileName = new Label();
            lblFileName.Text = name;
            lblFileName.CssClass = "SectionTitles";
            phCharts.Controls.Add(lblFileName);
            phCharts.Controls.Add(new LiteralControl("<br />"));

        }

        private void CreateCharts(int chartNo, string chartTitle, string PO_id, string tooltip, Color[] acceptedColours, string[] acceptedXValues, double[] acceptedYValues,
            Color[] currentColours, string[] currentXValues, double[] currentYValues)
        {
            Chart myChart = new Chart();
            myChart.Height = 200;

            myChart.Titles.Add(chartTitle);
            myChart.ChartAreas.Add("ChartArea1");
            myChart.ToolTip = tooltip;
            //this needs to be unique for each chart for the click event to work
            myChart.ID = chartNo.ToString();


            //Add the accepted Series
            myChart.Series.Add("Accepted");
            myChart.Series["Accepted"].ChartArea = "ChartArea1";
            myChart.Series["Accepted"].ChartType = SeriesChartType.Column;
            //myChart.Series["Accepted"].Url = string.Format("Details.aspx?PO_Id={0}", PO_id);
            myChart.Series["Accepted"].Url = string.Format("ValuesCharts.aspx?PO_Id={0}", PO_id);
            myChart.Series["Accepted"].Points.DataBindXY(acceptedXValues, acceptedYValues);
            for (int i = 0; i < acceptedColours.Count(); i++)
            {
                myChart.Series["Accepted"].Points[i].Color = acceptedColours[i];
            }
            //if (myChart.Series["Accepted"].Points.Count > 0)
            //{
            //    for (int i = 0; i < myChart.Series["Accepted"].Points.Count; i++)
            //    {
            //        DataPoint myDataPoint = myChart.Series["Accepted"].Points[i];
            //        myDataPoint.Url = string.Format("Details.aspx?PO_Id={0}", PO_id);
            //    }
            //}


            //Now add the Current Series
            myChart.Series.Add("Current");
            myChart.Series["Current"].ChartArea = "ChartArea1";
            myChart.Series["Current"].ChartType = SeriesChartType.Column;
            //myChart.Series["Current"].Url = string.Format("Details.aspx?PO_Id={0}", PO_id);
            myChart.Series["Current"].Url = string.Format("ValuesCharts.aspx?PO_Id={0}", PO_id);

            myChart.Series["Current"].Points.DataBindXY(currentXValues, currentYValues);
            for (int i = 0; i < currentColours.Count(); i++)
            {
                myChart.Series["Current"].Points[i].Color = currentColours[i];
            }

            phCharts.Controls.Add(myChart);
        }
        #endregion

    }
}