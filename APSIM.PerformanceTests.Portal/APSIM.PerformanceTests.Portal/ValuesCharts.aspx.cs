using APSIM.PerformanceTests.Portal.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace APSIM.PerformanceTests.Portal
{
    public partial class ValuesCharts : System.Web.UI.Page
    {
        #region Constants and variables
        #endregion

        #region Page and Control Events

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                int predictedObservedId = 0;
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

                    lblPullRequest.Text = "Pull Request: " + apsim.PullRequestId.ToString();
                    lblApsimFile.Text = "Apsim File: " + apsim.FileName;
                    lblPOTableName.Text = "Table: " + currPODetails.TableName + " (PO Id: " + currPODetails.ID.ToString() + ")";

                    //now bind the data
                    RetrieveDataAndBindCharts(currPODetails);
                }
            }
        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
            string pullrequestId = hfPullRequestID.Value.ToString();
            Response.Redirect(string.Format("TestsCharts.aspx?PULLREQUEST={0}", pullrequestId));
        }


        #endregion

        #region Data Retreval and Binding

        private void RetrieveDataAndBindCharts(PredictedObservedDetail currPODetails)
        {
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

            List<vSimulationPredictedObserved> POCurrentValuesList = PredictedObservedDS.GetCurrentAcceptedSimulationValues(currPODetails.ID, (int)currPODetails.AcceptedPredictedObservedDetailsID);
            string holdVariable = string.Empty, holdSimulationName = string.Empty;
            string tooltip = string.Empty, textAnnotation = string.Empty ;
            List<double> AcceptedXValues = new List<double>();
            List<double> AcceptedYValues = new List<double>();
            List<double> CurrentXValues = new List<double>();
            List<double> CurrentYValues = new List<double>();
            double maxXYValue = 0;
            bool newchart = false;
            int chartNo = 0;

            foreach (vSimulationPredictedObserved item in POCurrentValuesList)
            {
                if (holdVariable == string.Empty)
                {
                    holdVariable = item.ValueName;
                    holdSimulationName = item.SimulationName;
                    tooltip = String.Format("{0} - {1}", item.SimulationName, item.ValueName); 
                }

                newchart = false;
                //if (item.SimulationName != holdSimulationName) { newchart = true; }
                if (item.ValueName != holdVariable) { newchart = true; }

                if (newchart == true)
                {
                    //create the chart
                    if ((CurrentXValues.Count > 0) || (AcceptedXValues.Count > 0))
                    {
                        chartNo += 1;
                        textAnnotation = GetPredictedObservedTests(currPODetails.ID, holdVariable);
                        BindCurrentAcceptedChart(chartNo, holdVariable, currPODetails.ID, tooltip, CurrentXValues.ToArray(), CurrentYValues.ToArray(), AcceptedXValues.ToArray(), AcceptedYValues.ToArray(), maxXYValue, textAnnotation);
                    }
                    //reset the variables
                    AcceptedXValues = new List<double>();
                    AcceptedYValues = new List<double>();
                    CurrentXValues = new List<double>();
                    CurrentYValues = new List<double>();
                    maxXYValue = 0;
                    holdVariable = item.ValueName;
                    holdSimulationName = item.SimulationName;
                }

                if ((item.AcceptedObservedValue != null) && (item.AcceptedPredictedValue != null))
                {
                    AcceptedXValues.Add((double)item.AcceptedObservedValue);
                    AcceptedYValues.Add((double)item.AcceptedPredictedValue);

                    if ((double)item.AcceptedObservedValue > maxXYValue) { maxXYValue = (double)item.AcceptedObservedValue; }
                    if ((double)item.AcceptedPredictedValue > maxXYValue) { maxXYValue = (double)item.AcceptedPredictedValue; }
                }

                if ((item.CurrentObservedValue != null) && (item.CurrentPredictedValue != null))
                {
                    CurrentXValues.Add((double)item.CurrentObservedValue);
                    CurrentYValues.Add((double)item.CurrentPredictedValue);

                    if ((double)item.CurrentObservedValue > maxXYValue) { maxXYValue = (double)item.CurrentObservedValue; }
                    if ((double)item.CurrentPredictedValue > maxXYValue) { maxXYValue = (double)item.CurrentPredictedValue; }

                }
            }
            if ((CurrentXValues.Count > 0) || (AcceptedXValues.Count > 0))
            {
                chartNo += 1;
                textAnnotation = GetPredictedObservedTests(currPODetails.ID, holdVariable);
                BindCurrentAcceptedChart(chartNo, holdVariable, currPODetails.ID, tooltip, CurrentXValues.ToArray(), CurrentYValues.ToArray(), AcceptedXValues.ToArray(), AcceptedYValues.ToArray(), maxXYValue, textAnnotation);
            }
        }


        private string GetPredictedObservedTests(int currentPoID, string variable)
        {
            StringBuilder sb = new StringBuilder();
            string returnTests = string.Empty;
            //double? cIntercept = 0, cMAE = 0, cME = 0, cn = 0, cNSE = 0, cR2 = 0, cRMSE = 0, cRSR = 0, cSEintercept = 0, cSEslope = 0, cSlope = 0;
            //double? aIntercept = 0, aMAE = 0, aME = 0, an = 0, aNSE = 0, aR2 = 0, aRMSE = 0, aRSR = 0, aSEintercept = 0, aSEslope = 0, aSlope = 0;
            string cIntercept=string.Empty, aIntercept = string.Empty, cSlope = string.Empty, aSlope = string.Empty;

            //string htmlCodeBlack = "<font style='color:black'>";
            //string htmlCodeRed = "<font style='color:red'>";
            //string htmlCodeGreen = "<font style='color:green'>";
            //string htmlCodeEnd = "</font>";
            string newLine = " \n";

            string strCurrentY = "Y=";
            string strCurrentR2 = "R2=";
            string strCurrentNSE = "NSE=";
            string strCurrentRMSE = "RMSE=";
            string strCurrentME = "ME=";
            string strCurrentMAE = "MAE=";
            string strCurrentN = "N=";

            string strAcceptedY = "Y=";
            string strAcceptedR2 = "R2=";
            string strAcceptedNSE = "NSE=";
            string strAcceptedRMSE = "RMSE=";
            string strAcceptedME = "ME=";
            string strAcceptedMAE = "MAE=";
            string strAcceptedN = "N=";
            string itemCurrentStr = string.Empty;

            List<vPredictedObservedTestsFormatted> tests = PredictedObservedDS.GetPredictedObservedTestFormatted(currentPoID, variable);
            foreach (vPredictedObservedTestsFormatted item in tests)
            {
                try
                {
                    itemCurrentStr = item.CurrentF;

                    switch (item.Test)
                    {
                        case "Intercept":
                            aIntercept = item.AcceptedF;
                            cIntercept = itemCurrentStr;
                            break;
                        case "MAE":
                            strAcceptedMAE = strAcceptedMAE + item.AcceptedF;
                            strCurrentMAE = strCurrentMAE + itemCurrentStr;
                            break;
                        case "ME":
                            strAcceptedME = strAcceptedME + item.AcceptedF;
                            strCurrentME = strCurrentME + itemCurrentStr;
                            break;
                        case "n":
                            strAcceptedN = strAcceptedN + item.AcceptedF;
                            strCurrentN = strCurrentN + itemCurrentStr;
                            break;
                        case "NSE":
                            strAcceptedNSE = strAcceptedNSE + item.AcceptedF;
                            strCurrentNSE = strCurrentNSE + itemCurrentStr;
                            break;
                        case "R2":
                            strAcceptedR2 = strAcceptedR2 + item.AcceptedF;
                            strCurrentR2 = strCurrentR2 + itemCurrentStr;
                            break;
                        case "RMSE":
                            strAcceptedRMSE = strAcceptedRMSE + item.AcceptedF;
                            strCurrentRMSE = strCurrentRMSE + itemCurrentStr;
                            break;
                        case "Slope":
                            aSlope = item.AcceptedF;
                            cSlope = itemCurrentStr;
                            break;
                    }
                }
                catch (Exception)
                {
                }
            }
            strCurrentY = strCurrentY + cSlope+ "x + " + cIntercept;
            strAcceptedY = strAcceptedY + aSlope + "x + " + aIntercept;

            sb.Append("ACCEPTED" + newLine);
            sb.Append(strAcceptedY + newLine);
            sb.Append(strAcceptedR2 + newLine);
            sb.Append(strAcceptedRMSE + newLine);
            sb.Append(strAcceptedME + newLine);
            sb.Append(strAcceptedMAE + newLine);
            sb.Append(strAcceptedN + newLine);
            sb.Append(newLine + "CURRENT" + newLine);
            sb.Append(strCurrentY + newLine);
            sb.Append(strCurrentR2 + newLine);
            sb.Append(strCurrentRMSE + newLine);
            sb.Append(strCurrentME + newLine);
            sb.Append(strCurrentMAE + newLine);
            sb.Append(strCurrentN + newLine);
            returnTests = sb.ToString();

            return returnTests;
        }


        private void BindCurrentAcceptedChart(int chartNo, string chartTitle, int currPO_ID, string tooltip, double[] currentXValues, double[] currentYValues, 
            double[] acceptedXValues, double[] acceptedYValues, double maxXYValue, string annotationText)
        {

            Chart myChart = new Chart();
            myChart.Height = 350;
            myChart.Width = 500;

            myChart.Titles.Add(chartTitle);
            myChart.ChartAreas.Add("ChartArea1");

            myChart.ChartAreas["ChartArea1"].AxisX.LabelStyle.Format = "#";
            myChart.ChartAreas["ChartArea1"].AxisY.LabelStyle.Format = "#";

            myChart.ToolTip = tooltip;
            //this needs to be unique for each chart for the click event to work
            myChart.ID = chartNo.ToString();

            Legend myLegend = new Legend("myLegend");
            myChart.Legends.Add(myLegend);
            myChart.Legends[0].Alignment = System.Drawing.StringAlignment.Near;



            myChart.Series.Add("Accepted");
            Series acceptedSeries = myChart.Series["Accepted"];
            acceptedSeries.ChartType = SeriesChartType.Point;
            acceptedSeries.MarkerColor = Color.FromArgb(255, 128, 0);
            acceptedSeries.MarkerSize = 12;
            acceptedSeries.MarkerStyle = MarkerStyle.Circle;
            acceptedSeries.IsVisibleInLegend = true;
            acceptedSeries.ToolTip = "Observed: #VALX, Predicted: #VALY";
            acceptedSeries.Points.DataBindXY(acceptedXValues, acceptedYValues);
            acceptedSeries.Url = string.Format("PODetails.aspx?PO_Id={0}", currPO_ID);
            acceptedSeries.Legend = "myLegend";


            myChart.Series.Add("Current");
            Series currentSeries = myChart.Series["Current"];
            currentSeries.ChartType = SeriesChartType.Point;
            currentSeries.MarkerColor = Color.Navy;
            currentSeries.MarkerSize = 6;
            currentSeries.MarkerStyle = MarkerStyle.Diamond;
            currentSeries.IsVisibleInLegend = true;
            currentSeries.ToolTip = "Observed: #VALX, Predicted: #VALY";
            currentSeries.Points.DataBindXY(currentXValues, currentYValues);
            currentSeries.Url = string.Format("PODetails.aspx?PO_Id={0}", currPO_ID);
            currentSeries.Legend = "myLegend";


            //now for the slope line
            myChart.Series.Add("Slope");
            Series slope = myChart.Series["Slope"];
            slope.ChartType = SeriesChartType.Line;
            slope.Color = Color.Black;
            slope.Points.AddXY(0, 0);
            slope.Points.AddXY(maxXYValue, maxXYValue);
            slope.IsVisibleInLegend = false;


            //TextAnnotation annotation = new TextAnnotation();
            RectangleAnnotation annotation = new RectangleAnnotation();
            //annotation.Text = "Testing \n Annotation \n Details";
            annotation.Text = annotationText;
            annotation.X = 78;
            annotation.Y = 26;
            annotation.Url = string.Format("PODetails.aspx?PO_Id={0}", currPO_ID);

            //the following puts the annotation on the chart
            //annotation.AxisX = myChart.ChartAreas["ChartArea1"].AxisX;
            //annotation.AxisY = myChart.ChartAreas["ChartArea1"].AxisY;
            //annotation.AnchorX = (maxXYValue * 1.25);
            //annotation.AnchorY = (maxXYValue * 0.45);
            //myChart.ChartAreas["ChartArea1"].AxisX.Maximum = (maxXYValue * 1.25);
            //myChart.ChartAreas["ChartArea1"].AxisY.Maximum = (maxXYValue * 1.2);
            myChart.Annotations.Add(annotation);
            

            phGrids.Controls.Add(myChart);
        }

        #endregion

    }
}