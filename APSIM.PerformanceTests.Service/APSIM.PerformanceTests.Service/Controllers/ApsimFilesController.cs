using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Web.Http;
using System.Web.Http.Description;
using APSIM.PerformanceTests.Models;
using System.Threading.Tasks;
using System.Text;
using APSIM.Shared.Utilities;
using Newtonsoft.Json;



namespace APSIM.PerformanceTests.Service.Controllers
{
    public class ApsimFilesController : ApiController
    {
        /// <summary>
        /// Returns all ApsimFile details
        /// Usage:  GET (Read): api/apsimfiles/
        /// </summary>
        /// <returns></returns>
        public List<ApsimFile> GetAllApsimFiles()
        {
            List<ApsimFile> apsimFiles = new List<ApsimFile>();
            string connectStr = Utilities.GetConnectionString();

            using (SqlConnection sqlCon = new SqlConnection(connectStr))
            {
                sqlCon.Open();
                string strSQL = "SELECT * FROM ApsimFiles ORDER BY RunDate DESC, PullRequestId ";
                using (SqlCommand commandER = new SqlCommand(strSQL, sqlCon))
                {
                    commandER.CommandType = CommandType.Text;
                    string response = Comms.SendQuery(commandER, "reader");
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(response);
                    foreach(DataRow row in dt.Rows)
                    {
                        ApsimFile apsim = new ApsimFile
                        {
                            ID = (int)row[0],   
                            PullRequestId = (int)row[1],
                            FileName = (string)row[2],
                            FullFileName = (string)row[3],
                            RunDate = (DateTime)row[4],
                            StatsAccepted = (bool)row[5],
                            IsMerged = (bool)row[6],
                            SubmitDetails = (string)row[7]
                        };
                        if (row[8] == DBNull.Value)
                            apsim.AcceptedPullRequestId = 0;
                        else
                            apsim.AcceptedPullRequestId = (int)row[8];

                        apsimFiles.Add(apsim);
                    }
                }
            }
            return apsimFiles;
        }


        /// <summary>
        /// Returns the details of Apsim Files, based on the Pull Request ID
        ///  Usage:  GET (Read): api/apsimfiles/5 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(ApsimFile))]
        public List<ApsimFile> GetApsimFile(int id)
        {
            List<ApsimFile> apsimFiles = new List<ApsimFile>();
            string connectStr = Utilities.GetConnectionString();

            using (SqlConnection sqlCon = new SqlConnection(connectStr))
            {
                sqlCon.Open();
                string strSQL = "SELECT * FROM ApsimFiles WHERE PullRequestId = @PullRequestId ORDER BY RunDate DESC";
                using (SqlCommand commandER = new SqlCommand(strSQL, sqlCon))
                {
                    commandER.CommandType = CommandType.Text;
                    commandER.Parameters.AddWithValue("@PullRequestId", id);
                    SqlDataReader reader = commandER.ExecuteReader();
                    string response = Comms.SendQuery(commandER, "reader");
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(response);
                    foreach (DataRow row in dt.Rows)
                    {
                        ApsimFile apsim = new ApsimFile
                        {
                            ID = (int)row[0],
                            PullRequestId = (int)row[1],
                            FileName = (string)row[2],
                            FullFileName = (string)row[3],
                            RunDate = (DateTime)row[4],
                            StatsAccepted = (bool)row[5],
                            IsMerged = (bool)row[6],
                            SubmitDetails = (string)row[7]
                        };
                        if (row[8] == DBNull.Value)
                            apsim.AcceptedPullRequestId = 0;
                        else
                            apsim.AcceptedPullRequestId = (int)row[8];

                        apsimFiles.Add(apsim);
                    }
                }
            }
            return apsimFiles;
        }


        /// <summary>
        /// NOTE:  This is intended for localhost use only, will not work on production server
        /// Deletes all files (ApsimFiles, PredictedObservedDetails, PredictedObservedValues, PredictedObservedTests and Simualtions) for
        /// the specified Pull Request Id
        /// Usage:  DELETE : api/apsimfiles/ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> DeleteByPullRequestId(int id)
        {
            try
            {
                Utilities.WriteToLogFile("-----------------------------------");
                string connectStr = Utilities.GetConnectionString();
                using (SqlConnection sqlCon = new SqlConnection(connectStr))
                {
                    sqlCon.Open();
                    DeleteByPullRequest(sqlCon, id);
                    Utilities.WriteToLogFile(string.Format("Pull Request Id {0}, deleted on {1}!", id.ToString(), System.DateTime.Now.ToString("dd/mm/yyyy HH:mm")));
                }
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLogFile(string.Format("ERROR:  Unable to delete Pull Request Id {0}: {1}", id.ToString(), ex.Message.ToString()));
                return StatusCode(HttpStatusCode.NoContent);
            }
        }


        /// <summary>
        /// This takes the ApsimFile data, and provided from the APSIM.PerformanceTests.Collector and saves all of the date to the database
        /// Usage:  POST (Create): api/apsimfiles/ apsimfile
        /// </summary>
        /// <param name="apsimfile"></param>
        /// <returns></returns>
        [ResponseType(typeof(ApsimFile))]
        public async Task<IHttpActionResult> PostApsimFile(ApsimFile apsimfile)
        {
            int ApsimID = 0;
            string ErrMessageHelper = string.Empty;

            try
            {
                string connectStr = Utilities.GetConnectionString();
                string strSQL;

                Utilities.WriteToLogFile("  ");
                Utilities.WriteToLogFile("==========================================================");
                Utilities.WriteToLogFile(string.Format("Processing PullRequestId {0}, Apsim Filename {1}, dated {2}!", apsimfile.PullRequestId, apsimfile.FileName, apsimfile.RunDate.ToString("dd/MM/yyyy HH:mm")));

                //--------------------------------------------------------------------------------------
                //Need to check if this Pull Request Id has already been used,  if it has, then we need
                //to delete everything associated with it before we save the new set of data
                //--------------------------------------------------------------------------------------
                int pullRequestCount = 0;
                using (SqlConnection sqlCon = new SqlConnection(connectStr))
                {
                    sqlCon.Open();
                    Utilities.WriteToLogFile("    Checking for existing Pull Requests Details.");

                    try
                    {
                        strSQL = "SELECT COUNT(ID) FROM ApsimFiles WHERE PullRequestId = @PullRequestId AND RunDate != @RunDate";
                        using (SqlCommand commandES = new SqlCommand(strSQL, sqlCon))
                        {
                            commandES.CommandType = CommandType.Text;
                            commandES.Parameters.AddWithValue("@PullRequestId", apsimfile.PullRequestId);
                            commandES.Parameters.AddWithValue("@RunDate", apsimfile.RunDate);

                            string response = Comms.SendQuery(commandES, "scalar");
                            pullRequestCount = JsonConvert.DeserializeObject<int>(response);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.WriteToLogFile("    ERROR:  Checking for existing Pull Requests: " + ex.Message.ToString());
                    }

                    if (pullRequestCount > 0)
                    {
                        try
                        {
                            Utilities.WriteToLogFile("    Removing existing Pull Requests Details.");
                            using (SqlCommand commandENQ = new SqlCommand("usp_DeleteByPullRequestIdButNotRunDate", sqlCon))
                            {
                                // Configure the command and parameter.
                                commandENQ.CommandType = CommandType.StoredProcedure;
                                commandENQ.CommandTimeout = 0;
                                commandENQ.Parameters.AddWithValue("@PullRequestID", apsimfile.PullRequestId);
                                commandENQ.Parameters.AddWithValue("@RunDate", apsimfile.RunDate);

                                Comms.SendQuery(commandENQ, "nonquery");
                            }
                            Utilities.WriteToLogFile("    Removed original Pull Request Data.");
                        }
                        catch (Exception ex)
                        {
                            Utilities.WriteToLogFile("    ERROR:  Error Removing original Pull Request Data: " + ex.Message.ToString());
                        }
                    }

                    //--------------------------------------------------------------------------------------
                    //Add the ApsimFile Record first, so that we can get back the IDENTITY (ID) value
                    //--------------------------------------------------------------------------------------
                    //using (SqlConnection con = new SqlConnection(connectStr))
                    //{
                    Utilities.WriteToLogFile("    Inserting ApsimFiles details");
                    try
                    {
                        strSQL = "INSERT INTO ApsimFiles (PullRequestId, FileName, FullFileName, RunDate, StatsAccepted, IsMerged, SubmitDetails) "
                                + " OUTPUT INSERTED.ID Values ("
                                + "@PullRequestId, @FileName, @FullFileName, @RunDate, @StatsAccepted, @IsMerged, @SubmitDetails "
                                + " )";
                        using (SqlCommand commandES = new SqlCommand(strSQL, sqlCon))
                        {
                            commandES.CommandType = CommandType.Text;
                            commandES.Parameters.AddWithValue("@PullRequestId", apsimfile.PullRequestId);
                            commandES.Parameters.AddWithValue("@FileName", apsimfile.FileName);
                            commandES.Parameters.AddWithValue("@FullFileName", Utilities.GetModifiedFileName(apsimfile.FullFileName));
                            commandES.Parameters.AddWithValue("@RunDate", apsimfile.RunDate);
                            commandES.Parameters.AddWithValue("@StatsAccepted", apsimfile.StatsAccepted);
                            commandES.Parameters.AddWithValue("@IsMerged", apsimfile.IsMerged);
                            commandES.Parameters.AddWithValue("@SubmitDetails", apsimfile.SubmitDetails);


                            //this should return the IDENTITY value for this record (which is required for the next update)
                            ErrMessageHelper = "Filename: " + apsimfile.FileName;

                            string response = Comms.SendQuery(commandES, "scalar");
                            ApsimID = JsonConvert.DeserializeObject<int>(response);
                            ErrMessageHelper = "Filename: " + apsimfile.FileName + "- ApsimID: " + ApsimID;
                            Utilities.WriteToLogFile(string.Format("    Filename {0} imported successfully!", apsimfile.FileName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.WriteToLogFile("    ERROR:  Inserting into ApsimFiles failed: " + ex.Message.ToString());
                    }

                    //--------------------------------------------------------------------------------------
                    //Add the Simulation Details to the database 
                    //--------------------------------------------------------------------------------------
                    if (apsimfile.Simulations.Rows.Count > 0)
                    {
                        try
                        {
                            Utilities.WriteToLogFile("    Inserting Simualtion details for " + apsimfile.FileName);
                            using (SqlCommand commandENQ = new SqlCommand("usp_SimulationsInsert", sqlCon))
                            {
                                commandENQ.CommandType = CommandType.StoredProcedure;
                                commandENQ.Parameters.AddWithValue("@ApsimID", ApsimID);

                                SqlParameter tvpParam = commandENQ.Parameters.AddWithValue("@Simulations", apsimfile.Simulations);
                                tvpParam.SqlDbType = SqlDbType.Structured;
                                tvpParam.TypeName = "dbo.SimulationDataTableType";

                                ErrMessageHelper = "- Simualtion Data for " + apsimfile.FileName;

                                Comms.SendQuery(commandENQ, "nonquery");
                                Utilities.WriteToLogFile(string.Format("    Filename {0} Simulation Data imported successfully!", apsimfile.FileName));
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.WriteToLogFile("    ERROR:  usp_SimulationsInsert failed: " + ex.Message.ToString());
                        }
                    }


                    //--------------------------------------------------------------------------------------
                    //Add the Predited Observed Details (MetaData) and then the data
                    //--------------------------------------------------------------------------------------

                    //now look at each individual set of data
                    foreach (PredictedObservedDetails poDetail in apsimfile.PredictedObserved)
                    {
                        int predictedObservedID = 0;
                        Utilities.WriteToLogFile(string.Format("    Inserting Filename {0} PredictedObserved Table Details {1}.", apsimfile.FileName, poDetail.DatabaseTableName));
                        try
                        {
                            strSQL = "INSERT INTO PredictedObservedDetails ("
                            + " ApsimFilesID, TableName, PredictedTableName, ObservedTableName, FieldNameUsedForMatch, FieldName2UsedForMatch, FieldName3UsedForMatch, HasTests "
                            + " ) OUTPUT INSERTED.ID Values ("
                            + " @ApsimFilesID, @TableName, @PredictedTableName, @ObservedTableName, @FieldNameUsedForMatch, @FieldName2UsedForMatch, @FieldName3UsedForMatch, 0 "
                            + " )";

                            using (SqlCommand commandES = new SqlCommand(strSQL, sqlCon))
                            {
                                commandES.CommandType = CommandType.Text;
                                commandES.Parameters.AddWithValue("@ApsimFilesID", ApsimID);
                                commandES.Parameters.AddWithValue("@TableName", poDetail.DatabaseTableName);
                                commandES.Parameters.AddWithValue("@PredictedTableName", poDetail.PredictedTableName);
                                commandES.Parameters.AddWithValue("@ObservedTableName", poDetail.ObservedTableName);

                                commandES.Parameters.AddWithValue("@FieldNameUsedForMatch", poDetail.FieldNameUsedForMatch);
                                commandES.Parameters.AddWithValue("@FieldName2UsedForMatch", poDetail.FieldName2UsedForMatch);
                                commandES.Parameters.AddWithValue("@FieldName3UsedForMatch", poDetail.FieldName3UsedForMatch);

                                //this should return the IDENTITY value for this record (which is required for the next update)
                                ErrMessageHelper = "PredictedObservedDetails for " + poDetail.DatabaseTableName;

                                string response = Comms.SendQuery(commandES, "scalar");
                                predictedObservedID = JsonConvert.DeserializeObject<int>(response);
                                ErrMessageHelper = "PredictedObservedDetails for " + poDetail.DatabaseTableName + "(ID: " + predictedObservedID + ")";
                                Utilities.WriteToLogFile(string.Format("    Filename {0} PredictedObserved Table Details {1}, (Id: {2}) imported successfully!", apsimfile.FileName, poDetail.DatabaseTableName, predictedObservedID));
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.WriteToLogFile("    ERROR:  INSERT INTO PredictedObservedDetails failed: " + ex.Message.ToString()); 
                        }


                        //--------------------------------------------------------------------------------------
                        //And finally this is where we will insert the actual Predited Observed DATA
                        //--------------------------------------------------------------------------------------
                        DataView PredictedObservedView = new DataView(poDetail.PredictedObservedData);

                        string ObservedColumName, PredictedColumnName;

                        Utilities.WriteToLogFile(string.Format("    PredictedObserved Data Values for {0}.{1} - import started!", apsimfile.FileName, poDetail.DatabaseTableName));

                        //need to find the first (and then each instance thereafter) of a field name being with Observed,
                        //the get the corresponding Predicted field name, and then create a new table definition based on this
                        //data,
                        for (int i = 0; i < poDetail.PredictedObservedData.Columns.Count; i++)
                        {
                            ObservedColumName = poDetail.PredictedObservedData.Columns[i].ColumnName.Trim();
                            if (ObservedColumName.StartsWith("Observed"))
                            {
                                //get the corresponding Predicted Column Name
                                int dotPosn = ObservedColumName.IndexOf('.');
                                string valueName = ObservedColumName.Substring(dotPosn + 1);
                                PredictedColumnName = "Predicted." + valueName;

                                DataTable selectedData;
                                try
                                {
                                    if (poDetail.FieldName3UsedForMatch.Length > 0)
                                    {
                                        selectedData = PredictedObservedView.ToTable(false, "SimulationID", poDetail.FieldNameUsedForMatch, poDetail.FieldName2UsedForMatch, poDetail.FieldName3UsedForMatch, PredictedColumnName, ObservedColumName);
                                        strSQL = "usp_PredictedObservedDataThreeInsert";
                                    }
                                    else if (poDetail.FieldName2UsedForMatch.Length > 0)
                                    {
                                        selectedData = PredictedObservedView.ToTable(false, "SimulationID", poDetail.FieldNameUsedForMatch, poDetail.FieldName2UsedForMatch, PredictedColumnName, ObservedColumName);
                                        strSQL = "usp_PredictedObservedDataTwoInsert";
                                    }
                                    else
                                    {
                                        selectedData = PredictedObservedView.ToTable(false, "SimulationID", poDetail.FieldNameUsedForMatch, PredictedColumnName, ObservedColumName);
                                        strSQL = "usp_PredictedObservedDataInsert";
                                    }

                                    using (SqlCommand commandENQ = new SqlCommand(strSQL, sqlCon))
                                    {
                                        commandENQ.CommandType = CommandType.StoredProcedure;
                                        commandENQ.Parameters.AddWithValue("@PredictedObservedID", predictedObservedID);
                                        commandENQ.Parameters.AddWithValue("@ApsimFilesID", ApsimID);
                                        commandENQ.Parameters.AddWithValue("@ValueName", valueName);

                                        if (poDetail.FieldName3UsedForMatch.Length > 0)
                                        {
                                            commandENQ.Parameters.AddWithValue("@MatchName", poDetail.FieldNameUsedForMatch);
                                            commandENQ.Parameters.AddWithValue("@MatchName2", poDetail.FieldName2UsedForMatch);
                                            commandENQ.Parameters.AddWithValue("@MatchName3", poDetail.FieldName3UsedForMatch);
                                        }
                                        else if (poDetail.FieldName2UsedForMatch.Length > 0)
                                        {
                                            commandENQ.Parameters.AddWithValue("@MatchName", poDetail.FieldNameUsedForMatch);
                                            commandENQ.Parameters.AddWithValue("@MatchName2", poDetail.FieldName2UsedForMatch);
                                        }
                                        else
                                        {
                                            commandENQ.Parameters.AddWithValue("@MatchName", poDetail.FieldNameUsedForMatch);
                                        }

                                        SqlParameter tvtpPara = commandENQ.Parameters.AddWithValue("@PredictedOabservedData", selectedData);
                                        tvtpPara.SqlDbType = SqlDbType.Structured;

                                        if (poDetail.FieldName3UsedForMatch.Length > 0)
                                        {
                                            tvtpPara.TypeName = "dbo.PredictedObservedDataThreeTableType";
                                        }
                                        else if (poDetail.FieldName2UsedForMatch.Length > 0)
                                        {
                                            tvtpPara.TypeName = "dbo.PredictedObservedDataTwoTableType";
                                        }
                                        else
                                        {
                                            tvtpPara.TypeName = "dbo.PredictedObservedDataTableType";
                                        }

                                        ErrMessageHelper = "PredictedObservedDetails Id " + predictedObservedID + ", ValueName: " + valueName;
                                        Comms.SendQuery(commandENQ, "scalar");
                                        Utilities.WriteToLogFile(string.Format("       PredictedObserved Data for {0} import completed successfully!", valueName));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Utilities.WriteToLogFile("    ERROR:  Unable to import PredictedObserved Data: " + ErrMessageHelper.ToString() + " - " + ex.Message.ToString());
                                }
                            }   //ObservedColumName.StartsWith("Observed")
                        }   // for (int i = 0; i < poDetail.PredictedObservedData.Columns.Count; i++)


                        //Need to run the testing procecedure here, and then save the test data
                        if (poDetail.PredictedObservedData.Rows.Count > 0)
                        {
                            ErrMessageHelper = string.Empty;

                            Utilities.WriteToLogFile(string.Format("    Tests Data for {0}.{1} import started.", apsimfile.FileName, poDetail.DatabaseTableName));

                            //need to retrieve data for the "AcceptedStats" version, so that we can update the stats
                            int acceptedPredictedObservedDetailsID = 0;    //this should get updated in 'RetrieveAcceptedStatsData' 
                            ErrMessageHelper = "Processing RetrieveAcceptedStatsData.";
                            DataTable acceptedStats = RetrieveAcceptedStatsData(sqlCon, ApsimID, apsimfile, poDetail, predictedObservedID, ref acceptedPredictedObservedDetailsID);

                            ErrMessageHelper = "Processing Tests.DoValidationTest.";
                            DataTable dtTests = Tests.DoValidationTest(poDetail.DatabaseTableName, poDetail.PredictedObservedData, acceptedStats);

                            ErrMessageHelper = "Processing DBFunctions.AddPredictedObservedTestsData.";
                            DBFunctions.AddPredictedObservedTestsData(sqlCon, apsimfile.FileName, predictedObservedID, poDetail.DatabaseTableName, dtTests);

                            //Update the accepted reference for Predicted Observed Values, so that it can be 
                            ErrMessageHelper = "Processing DBFunctions.UpdatePredictedObservedDetails.";
                            DBFunctions.UpdatePredictedObservedDetails(sqlCon, acceptedPredictedObservedDetailsID, predictedObservedID);
                        }
                    }   //foreach (PredictedObservedDetails poDetail in apsimfile.PredictedObserved)
                }
                return CreatedAtRoute("DefaultApi", new { id = ApsimID }, apsimfile);
            }

            catch (Exception ex)
            {
                Utilities.WriteToLogFile("    ERROR in PostApsimFile:  " + ErrMessageHelper.ToString() + " - " + ex.Message.ToString());
                throw new Exception("    ERROR in PostApsimFile:  " + ErrMessageHelper.ToString() + " - " + ex.Message.ToString());
            }
        }

 

        /// <summary>
        // Returns the PredictedObservedTests data for 'Accepted' data set, based on matching 'Current' Details
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="currentApsimID"></param>
        /// <param name="currentApsim"></param>
        /// <param name="poDetail"></param>
        /// <param name="predictedObservedId"></param>
        /// <param name="acceptedPredictedObservedDetailsID"></param>
        /// <returns></returns>
        private static DataTable RetrieveAcceptedStatsData(SqlConnection sqlCon, int currentApsimID, ApsimFile currentApsim, PredictedObservedDetails poDetail, int predictedObservedId, ref int acceptedPredictedObservedDetailsID)
        {
            DataTable acceptedStats = new DataTable();
            ApsimFile acceptedApsim = new ApsimFile();
            try
            {
                string strSQL = "SELECT TOP 1 * FROM ApsimFiles WHERE StatsAccepted = 1 AND PullRequestId != @PullRequestId ORDER BY RunDate DESC";
                using (SqlCommand commandER = new SqlCommand(strSQL, sqlCon))
                {
                    commandER.CommandType = CommandType.Text;
                    commandER.Parameters.AddWithValue("@PullRequestId", currentApsim.PullRequestId);

                    string response = Comms.SendQuery(commandER, "scalar");
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(response);
                    foreach (DataRow row in dt.Rows)
                    {
                        acceptedApsim.ID = (int)row[0];
                        acceptedApsim.PullRequestId = (int)row[1];
                        acceptedApsim.FileName = (string)row[2];
                        acceptedApsim.FullFileName = (string)row[3];
                        acceptedApsim.RunDate = (DateTime)row[4];
                        acceptedApsim.StatsAccepted = (bool)row[5];
                        acceptedApsim.IsMerged = (bool)row[6];
                        acceptedApsim.SubmitDetails = (string)row[7];
                        if (row[8] == DBNull.Value)
                            acceptedApsim.AcceptedPullRequestId = 0;
                        else
                            acceptedApsim.AcceptedPullRequestId = (int)row[8];
                    }
                }

                if (acceptedApsim.PullRequestId > 0)
                {
                    DBFunctions.UpdateApsimFileAcceptedDetails(sqlCon, currentApsim.PullRequestId, acceptedApsim.PullRequestId, acceptedApsim.RunDate);

                    ////get the PredictedObservedDetail.ID for the records that match our current record 'matching' criteria
                    acceptedPredictedObservedDetailsID = DBFunctions.GetAcceptedPredictedObservedDetailsId(sqlCon, acceptedApsim.PullRequestId, currentApsim.FileName, poDetail);
                    ////Now retreieve the matching tests data for our predicted observed details
                    acceptedStats = DBFunctions.GetPredictedObservedTestsData(sqlCon, acceptedPredictedObservedDetailsID);
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLogFile(string.Format("    ERROR:  Unable to RetrieveAcceptedStatsData for ApsimFile {0}: Pull Request Id {1}: {2}.", currentApsim.FileName, currentApsim.PullRequestId, ex.Message.ToString()));
            }
            return acceptedStats;
        }



        /// <summary>
        /// Deletes all Data for a specified Pull RequestId
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="pullRequestId"></param>
        private static void DeleteByPullRequest(SqlConnection sqlCon, int pullRequestId)
        {
            try
            {
                using (SqlCommand commandENQ = new SqlCommand("usp_DeleteByPullRequestId", sqlCon))
                {
                    // Configure the command and parameter.
                    commandENQ.CommandType = CommandType.StoredProcedure;
                    commandENQ.CommandTimeout = 0;
                    commandENQ.Parameters.AddWithValue("@PullRequestID", pullRequestId);

                    Comms.SendQuery(commandENQ, "nonquery");
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLogFile(string.Format("    ERROR:  Unable to remove data for Pull Request Id: {0}: {1}.", pullRequestId, ex.Message.ToString()));
            }
        }

        /// <summary>
        /// Deletes all Data for a specified Pull RequestId, excluding those for this Run Date
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="pullRequestId"></param>
        /// <param name="runDate"></param>
        private static void DeleteByPullRequestButNotRunDate(SqlConnection sqlCon, int pullRequestId, DateTime runDate)
        {
            try
            {
                using (SqlCommand commandENQ = new SqlCommand("usp_DeleteByPullRequestIdButNotRunDate", sqlCon))
                {
                    // Configure the command and parameter.
                    commandENQ.CommandType = CommandType.StoredProcedure;
                    commandENQ.CommandTimeout = 0;
                    commandENQ.Parameters.AddWithValue("@PullRequestID", pullRequestId);
                    commandENQ.Parameters.AddWithValue("@RunDate", runDate);

                    Comms.SendQuery(commandENQ, "nonquery");
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLogFile(string.Format("    ERROR:  Unable to remove data for Pull Request Id: {0} on {1}: {2}.", pullRequestId, runDate.ToString("dd/MM/yyyy HH:mm"), ex.Message.ToString()));
            }
        }
    }
}
