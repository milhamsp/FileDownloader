using FileDownloader.Config;
using SharpConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FileDownloader
{
    internal class Helper
    {
        public static bool ReadConfig()
        {
            bool isOk = false;
            try
            {
                var config = Configuration.LoadFromFile("Config.ini");
                var section = config["FtpConfig"];
                FtpConfig.Protocol = section["Protocol"].StringValue.Trim();
                FtpConfig.Username = section["Username"].StringValue.Trim();
                FtpConfig.Password = section["Password"].StringValue.Trim();
                FtpConfig.Host = section["Host"].StringValue.Trim();
                FtpConfig.RemoteDirectory = section["RemoteDirectory"].StringValue.Trim();
                FtpConfig.DownloadDirectory = section["DownloadDirectory"].StringValue.Trim();
                FtpConfig.TempDirectory = section["TempDirectory"].StringValue.Trim();
                FtpConfig.TargetDirectory = section["TargetDirectory"].StringValue.Trim();
                FtpConfig.LogDirectory = section["LogDirectory"].StringValue.Trim();
                isOk = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                isOk = false;
            }
            return isOk;
        }

        public static List<string> GetFTPFiles(string protocol, string username, string password, string host, string remotedirectory)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(protocol + username + ":" + password + "@" + host + "/" + remotedirectory);
                string url = sb.ToString();

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream);

                string filename = sr.ReadToEnd();

                Console.WriteLine($"Status {response.StatusDescription}" +
                    $"\n#################################################################\n");

                sr.Close();
                response.Close();

                return filename.Replace(remotedirectory+"/", "").Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch
            {
                return null;
            }
        }

        public static bool CheckDirectory(string directory)
        {
            bool isOk = false;
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"{directory} doesn't exist, creating {directory} ");
                Directory.CreateDirectory(directory);
                return !isOk;
            }
            else if (Directory.Exists(directory))
            {
                Console.WriteLine($"{directory} already exist, continuing the process");
                return !isOk;
            }
            else
            {
                return isOk;
            }
        }

        public static string CheckFolderDate(string path)
        {
            string folderPath = "";
            StringBuilder sbFolder = new StringBuilder(path);
            DateTime dateTime = DateTime.Now.Date;

            if (CheckDirectory(sbFolder.ToString()))
            {
                sbFolder.Append("\\");
                sbFolder.Append(dateTime.ToString("yyyy"));
                if (CheckDirectory(sbFolder.ToString()))
                {
                    sbFolder.Append("\\");
                    sbFolder.Append(dateTime.ToString("MM"));
                    if (CheckDirectory(sbFolder.ToString()))
                    {
                        sbFolder.Append("\\");
                        sbFolder.Append(dateTime.ToString("dd"));
                        if (CheckDirectory(sbFolder.ToString()))
                        {
                            folderPath = sbFolder.ToString();
                        }
                    }
                }
            }

            return folderPath;
        }

        public static void WriteLog(string message) { 
            Console.WriteLine(message);

            string logFolder = FtpConfig.LogDirectory;
            string currDate = "Log_"+DateTime.Now.ToString("ddMMyy")+".log";
            string logPath = logFolder + "/" + currDate;
            
            try
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    LogFormat(message, sw);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("LogWrite: Error => " + ex.Message);
            }
        }

        public static void LogFormat(string logMessage, TextWriter tw)
        {
            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                //string pdi = currentProcess.Id.ToString();
                //string pname = currentProcess.ProcessName;
                string currDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                //tw.Write($"[{currDate}] [{pdi}] [{pname}] ");
                tw.Write($"[{currDate}] ");
                tw.WriteLine($": {logMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log: Error => " + ex.Message);
            }
        }

        public static void ClearLog(string directory) 
        {
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        public static void ClearTempFolder(string directory)
        {
            DirectoryInfo tempDirPath = new DirectoryInfo(directory);
            Helper.WriteLog("Clearing Temp folder..");

            foreach (FileInfo file in tempDirPath.GetFiles())
            {
                Helper.WriteLog($"Deleting file {directory+file}..");
                file.Delete();
            }

            foreach (DirectoryInfo dir in tempDirPath.GetDirectories())
            {
                Helper.WriteLog($"Deleting directory {directory+dir}..");
                dir.Delete(true);
            }
        }
    }
}
