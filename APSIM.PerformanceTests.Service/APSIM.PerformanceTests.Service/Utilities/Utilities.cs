﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace APSIM.PerformanceTests.Service
{
    public class Utilities
    {

        public static string GetModifiedFileName(string fileName)
        {
            string returnStr;
            int posn = fileName.IndexOf(@"ApsimX\Tests");
            if (posn < 0)
            {
                posn = fileName.IndexOf(@"ApsimX\Prototypes");
            }
            if (posn > 0) { posn += 7; }
            if (posn < 0) { posn = 0; }
            returnStr = fileName.Substring(posn);

            return returnStr;
        }

        public static string GetConnectionString()
        {
            string file = @"D:\Websites\dbConnect.txt";
            string connectionString = string.Empty;
#if DEBUG
            file = @"C:\Dev\PerformanceTests\dbConnect.txt";
#endif
            try
            {
                connectionString = File.ReadAllText(file) + ";Database=\"APSIM.PerformanceTests\"";
                return connectionString;

            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR: Unable to retrieve Database connection details: " + ex.Message.ToString());
                return connectionString;
            }
        }



        public static void WriteToLogFile(string message)
        {
            if (message.Length > 0)
            {
                //this is just a temporary measure so that I can see what is happening
                //Console.WriteLine(message);

                //Need to make sure we are in the same directory as this application 
                //string fileName = getDirectoryPath("PerformanceTestsLog.txt");
                string fileName = @"D:\Websites\APSIM.PerformanceTests.Service\PerformanceTestsLog.txt";
#if DEBUG
                fileName = @"C:\Dev\PerformanceTests\PerformanceTestsLog.txt";
#endif
                StreamWriter sw;

                if (!File.Exists(fileName))
                {
                    sw = new StreamWriter(fileName);
                }
                else
                {
                    sw = File.AppendText(fileName);
                }
                string logLine = String.Format("{0}: {1}", System.DateTime.Now.ToString("yyyy-MMM-dd HH:mm"), message);
                sw.WriteLine(logLine);
                sw.Close();
            }
        }

        /// <summary>
        /// creates the file/name path details for the for the specified file and the application's path.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string getDirectoryPath(string fileName)
        {
            string returnStr = string.Empty;

            //To get the location the assembly normally resides on disk or the install directory
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;

            returnStr = Path.GetDirectoryName(path) + "\\" + fileName;
            return returnStr;
        }

    }

}