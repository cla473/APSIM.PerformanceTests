using APSIM.PerformanceTests.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace APSIM.PerformanceTests.Service
{
    public class DBFunctions
    {
        /// <summary>
        /// Gets the PredictedObservedDetail.ID for the records that match our current record 'matching' criteria
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="acceptedPullRequestID"></param>
        /// <param name="currentApsimFileFileName"></param>
        /// <param name="currentPODetails"></param>
        /// <returns></returns>
        public static int GetAcceptedPredictedObservedDetailsId(SqlConnection sqlCon, int acceptedPullRequestID, string currentApsimFileFileName, PredictedObservedDetails currentPODetails)
        {
            int acceptedPredictedObservedDetailsID = 0;
            try
            {
                string strSQL = "SELECT p.ID  "
                + " FROM PredictedObservedDetails p INNER JOIN ApsimFiles a ON p.ApsimFilesID = a.ID "
                + " WHERE a.PullRequestId = @pullRequestId "
                + "    AND a.FileName = @filename "
                + "    AND p.TableName = @tablename "
                + "    AND p.PredictedTableName = @predictedTableName "
                + "    AND p.ObservedTableName = @observedTableName "
                + "    AND p.FieldNameUsedForMatch = @fieldNameUsedForMatch ";

                if ((currentPODetails.FieldName2UsedForMatch != null) &&  (currentPODetails.FieldName2UsedForMatch.Length > 0))
                {
                    strSQL = strSQL + "    AND p.FieldName2UsedForMatch = @fieldName2UsedForMatch ";
                }

                if ((currentPODetails.FieldName3UsedForMatch != null) && (currentPODetails.FieldName3UsedForMatch.Length > 0))
                {
                    strSQL = strSQL + "    AND p.FieldName3UsedForMatch = @fieldName3UsedForMatch ";
                }

                using (SqlCommand commandES = new SqlCommand(strSQL, sqlCon))
                {
                    commandES.CommandType = CommandType.Text;
                    commandES.Parameters.AddWithValue("@PullRequestId", acceptedPullRequestID);
                    commandES.Parameters.AddWithValue("@filename", currentApsimFileFileName);
                    commandES.Parameters.AddWithValue("@tablename", currentPODetails.DatabaseTableName);
                    commandES.Parameters.AddWithValue("@predictedTableName", currentPODetails.PredictedTableName);
                    commandES.Parameters.AddWithValue("@observedTableName", currentPODetails.ObservedTableName);
                    commandES.Parameters.AddWithValue("@fieldNameUsedForMatch", currentPODetails.FieldNameUsedForMatch);

                    if ((currentPODetails.FieldName2UsedForMatch != null) && (currentPODetails.FieldName2UsedForMatch.Length > 0))
                    {
                        commandES.Parameters.AddWithValue("@fieldName2UsedForMatch", currentPODetails.FieldName2UsedForMatch);
                    }

                    if ((currentPODetails.FieldName3UsedForMatch != null) && (currentPODetails.FieldName3UsedForMatch.Length > 0))
                    {
                        commandES.Parameters.AddWithValue("@fieldName3UsedForMatch", currentPODetails.FieldName3UsedForMatch);
                    }

                    string response = Comms.SendQuery(commandES, "scalar");
                    object obj = JsonConvert.DeserializeObject<DataTable>(response);
                    if (obj != null)
                    {
                        acceptedPredictedObservedDetailsID = int.Parse(obj.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLogFile(string.Format("    ERROR in GetAcceptedPredictedObservedDetailsId: Unable to retrieve 'Accepted' PredictedObservedDetailsID for Pull Request {0}: {1}", acceptedPullRequestID, ex.Message.ToString()));
            }
            return acceptedPredictedObservedDetailsID;
        }

        /// <summary>
        /// Retrieves the Tests stats for the 'Accepted' PredictedObservedTests 
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="acceptedPredictedObservedDetailsID"></param>
        /// <returns></returns>
        public static DataTable GetPredictedObservedTestsData(SqlConnection sqlCon, int acceptedPredictedObservedDetailsID)
        {
            DataTable acceptedStats = new DataTable();
            if (acceptedPredictedObservedDetailsID > 0)
            {
                try
                {
                    string strSQL = "SELECT Variable, Test, [Current] as 'Accepted', ID As 'AcceptedPredictedObservedTestsID' "
                            + " FROM PredictedObservedTests "
                            + " WHERE PredictedObservedDetailsID = @PredictedObservedDetailsID "
                            + " ORDER BY Variable, Test, 4";

                    using (SqlCommand commandER = new SqlCommand(strSQL, sqlCon))
                    {
                        commandER.CommandType = CommandType.Text;
                        commandER.Parameters.AddWithValue("@PredictedObservedDetailsID", acceptedPredictedObservedDetailsID);

                        string response = Comms.SendQuery(commandER, "reader");

                        acceptedStats = JsonConvert.DeserializeObject<DataTable>(response);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLogFile(string.Format("    ERROR in getPredictedObservedTestsData: Unable to retrieve Tests Data PredictedObserved ID {0}: {1}", acceptedPredictedObservedDetailsID, ex.Message.ToString()));
                }
            }
            return acceptedStats;
        }

        /// <summary>
        /// Retreieves the matching Predicted Pbserved Values based on the Predicted Pbserved Id
        /// </summary>
        /// <param name="sqlCon"></param>
        /// <param name="predictedObservedID"></param>
        /// <returns></returns>
        public static DataTable GetPredictedObservedValues(SqlConnection sqlCon, int predictedObservedID)
        {
            DataTable resultDT = new DataTable();
            try
            {
                string strSQL = "SELECT ID, ValueName, PredictedValue, ObservedValue"
                        + " FROM PredictedObservedValues "
                        + " WHERE PredictedObservedDetailsID = @PredictedObservedDetailsID " 
                        + " ORDER BY ValueName, ID ";

                using (SqlCommand commandER = new SqlCommand(strSQL, sqlCon))
                {
                    commandER.CommandType = CommandType.Text;
                    commandER.Parameters.AddWithValue("@PredictedObservedDetailsID", predictedObservedID);

                    string response = Comms.SendQuery(commandER, "reader");

                    resultDT = JsonConvert.DeserializeObject<DataTable>(response);
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLogFile(string.Format("    ERROR in getPredictedObservedValues: Unable to retrieve PredictedObserved Values for ID {0}: {1}", predictedObservedID, ex.Message.ToString()));
            }

            return resultDT;
        }

        /// <summary>
        /// Adds PredictedObservedTests Data to the database
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="currentApsimFileFileName"></param>
        /// <param name="currentPODetailsID"></param>
        /// <param name="currentPODetailsDatabaseTableName"></param>
        /// <param name="dtTests"></param>
        public static void AddPredictedObservedTestsData(SqlConnection sqlCon, string currentApsimFileFileName, int currentPODetailsID, string currentPODetailsDatabaseTableName, DataTable dtTests)
        {
            if (dtTests.Rows.Count > 0)
            {
                try
                {
                    using (SqlCommand commandENQ = new SqlCommand("usp_PredictedObservedTestsInsert", sqlCon))
                    {
                        //Now update the database with the test results
                        // Configure the command and parameter.
                        commandENQ.CommandType = CommandType.StoredProcedure;
                        commandENQ.Parameters.AddWithValue("@PredictedObservedID", currentPODetailsID);

                        SqlParameter tvpParam = commandENQ.Parameters.AddWithValue("@Tests", dtTests);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "dbo.PredictedObservedTestsTableType";

                        Comms.SendQuery(commandENQ, "nonquery");
                    }
                    Utilities.WriteToLogFile(string.Format("    Tests Data for {0}.{1} import completed successfully!", currentApsimFileFileName, currentPODetailsDatabaseTableName));
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLogFile(string.Format("    ERROR in AddPredictedObservedTestsData: Unable to save Tests Data for {0}.{1}:  {2}", currentApsimFileFileName, currentPODetailsDatabaseTableName, ex.Message.ToString()));
                }
            }
        }

        /// <summary>
        /// Updates the Accepted PredictedObservedDetails.ID for the current PredictedObservedDetails record
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="acceptedPredictedObservedDetailsID"></param>
        /// <param name="currentPODetailsID"></param>
        public static void UpdatePredictedObservedDetails(SqlConnection sqlCon, int acceptedPredictedObservedDetailsID, int currentPODetailsID)
        {
            //Update the accepted reference for Predicted Observed Values, so that it can be 
            if (acceptedPredictedObservedDetailsID > 0 && currentPODetailsID > 0)
            {
                try
                {
                    string strSQL = "UPDATE PredictedObservedDetails "
                            + " SET AcceptedPredictedObservedDetailsID = @AcceptedPredictedObservedDetailsID "
                            + " WHERE ID = @PredictedObservedDetailsID ";

                    using (SqlCommand commandENQ = new SqlCommand(strSQL, sqlCon))
                    {
                        commandENQ.CommandType = CommandType.Text;
                        commandENQ.Parameters.AddWithValue("@AcceptedPredictedObservedDetailsID", acceptedPredictedObservedDetailsID);
                        commandENQ.Parameters.AddWithValue("@PredictedObservedDetailsID", currentPODetailsID);

                        Comms.SendQuery(commandENQ, "nonquery");
                    }
                    Utilities.WriteToLogFile("    Accepted PredictedObservedDetailsID added to PredictedObservedDetails.");
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLogFile(string.Format("    ERROR in UpdatePredictedObservedDetails: Unable to update 'Accepted' PredictedObservedDetailsID {0} ON PredictedObservedDetails ID {1}: {2} ", acceptedPredictedObservedDetailsID, currentPODetailsID, ex.Message.ToString()));
                }
            }
        }


        /// <summary>
        /// Updates 'Accepted' ApsimFile details (PullRequestId, RunDate) for ApsimFile using Current Pull RequestId
        /// </summary>
        /// <param name="connectStr"></param>
        /// <param name="currentApsimFileID"></param>
        /// <param name="acceptedPullRequestID"></param>
        public static void UpdateApsimFileAcceptedDetails(SqlConnection sqlCon, int currentPullRequestID, int acceptedPullRequestID, DateTime acceptedRunDate)
        {
            if ((currentPullRequestID > 0) && (acceptedPullRequestID > 0))
            {
                try
                {
                    string strSQL = "UPDATE ApsimFiles "
                            + " SET AcceptedPullRequestId = @AcceptedPullRequestId, "
                            + " AcceptedRunDate = @AcceptedRunDate "
                            + " WHERE PullRequestId = @PullRequestID ";

                    using (SqlCommand commandENQ = new SqlCommand(strSQL, sqlCon))
                    {
                        commandENQ.CommandType = CommandType.Text;
                        commandENQ.Parameters.AddWithValue("@AcceptedPullRequestId", acceptedPullRequestID);
                        commandENQ.Parameters.AddWithValue("@AcceptedRunDate", acceptedRunDate);
                        commandENQ.Parameters.AddWithValue("@PullRequestID", currentPullRequestID);

                        Comms.SendQuery(commandENQ, "nonquery");
                    }
                    //Utilities.WriteToLogFile("    Accepted ApsimFilesID added to ApsimFiles.");
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLogFile(String.Format("    ERROR in UpdateApsimFileAcceptedDetails: Unable to update Accepted Pull Request {0} Details for Pull Request ID {1}: {2}", acceptedPullRequestID, currentPullRequestID, ex.Message.ToString()));
                }
            }

        }

        /// <summary>
        /// UPdate the AcceptStats Log file
        /// </summary>
        /// <param name="acceptLog"></param>
        public static void UpdateAsStatsAccepted(string StatsType, AcceptStatsLog acceptLog)
        {
            string connectStr = Utilities.GetConnectionString();
            Utilities.WriteToLogFile("-----------------------------------");

            //make sure this is 0 if we are updating as 'Accepted'
            if (StatsType == "Accept")
            {
                acceptLog.StatsPullRequestId = 0;
                acceptLog.FileCount = 0;
            }

            //need to authenticate the process
            int statsAccepted = Convert.ToInt32(acceptLog.LogStatus);

            using (SqlConnection sqlCon = new SqlConnection(connectStr))
            {
                sqlCon.Open();

                try
                {
                    string strSQL = "INSERT INTO AcceptStatsLogs (PullRequestId, SubmitPerson, SubmitDate, FileCount, LogPerson, LogReason, LogStatus, LogAcceptDate, StatsPullRequestId) "
                                    + " Values ( @PullRequestId, @SubmitPerson, @SubmitDate, @LogPerson, @LogReason, @LogStatus, @LogAcceptDate, @StatsPullRequestId )";
                    using (SqlCommand commandENQ = new SqlCommand(strSQL, sqlCon))
                    {
                        commandENQ.CommandType = CommandType.Text;
                        commandENQ.Parameters.AddWithValue("@PullRequestId", acceptLog.PullRequestId);
                        commandENQ.Parameters.AddWithValue("@SubmitPerson", acceptLog.SubmitPerson);
                        commandENQ.Parameters.AddWithValue("@SubmitDate", acceptLog.SubmitDate);
                        commandENQ.Parameters.AddWithValue("@FileCount", acceptLog.FileCount);
                        commandENQ.Parameters.AddWithValue("@LogPerson", acceptLog.LogPerson);
                        commandENQ.Parameters.AddWithValue("@LogReason", acceptLog.LogReason);
                        commandENQ.Parameters.AddWithValue("@LogStatus", acceptLog.LogStatus);
                        commandENQ.Parameters.AddWithValue("@LogAcceptDate", acceptLog.LogAcceptDate);
                        commandENQ.Parameters.AddWithValue("@StatsPullRequestId", acceptLog.StatsPullRequestId);

                        Comms.SendQuery(commandENQ, "nonquery");
                    }

                    if (StatsType == "Accept")
                    {
                        strSQL = "UPDATE ApsimFiles SET StatsAccepted = @StatsAccepted, IsMerged = @IsMerged WHERE PullRequestId = @PullRequestId";
                        using (SqlCommand commandENQ = new SqlCommand(strSQL, sqlCon))
                        {
                            commandENQ.CommandType = CommandType.Text;
                            commandENQ.Parameters.AddWithValue("@StatsAccepted", statsAccepted);
                            commandENQ.Parameters.AddWithValue("@IsMerged", statsAccepted);        //do this the same to during changeover
                            commandENQ.Parameters.AddWithValue("@PullRequestId", acceptLog.PullRequestId);

                            Comms.SendQuery(commandENQ, "nonquery");
                        }
                    }
                    //Utilities.WriteToLogFile(string.Format("    Accept Stats Status updated to {0} by {1} on {2}. Reason: {3}", acceptLog.LogStatus, acceptLog.LogPerson, acceptLog.SubmitDate, acceptLog.LogReason));
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLogFile(string.Format("ERROR in UpdateAsStatsAccepted:  Pull Request Id {0}, Failed to update as 'Stats Accepted': {1}", acceptLog.PullRequestId.ToString(), ex.Message.ToString()));
                }
            }
        }

        public static DateTime GetLatestPullRequestRunDate(SqlConnection sqlCon, int acceptedPullRequestID)
        {
            DateTime returnDate = new DateTime();

            string strSQL = "SELECT TOP 1 RunDate FROM ApsimFiles WHERE PullRequestId = @PullRequestId ORDER BY RunDate DESC";
            using (SqlCommand commandES = new SqlCommand(strSQL, sqlCon))
            {
                commandES.CommandType = CommandType.Text;
                commandES.Parameters.AddWithValue("@PullRequestId", acceptedPullRequestID);

                string response = Comms.SendQuery(commandES, "scalar");

                returnDate = JsonConvert.DeserializeObject<DateTime>(response);
            }
            return returnDate;
        }

    }
}