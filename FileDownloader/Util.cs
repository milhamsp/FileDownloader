using FileDownloader.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DigiCSLiteUpdater
{
    internal class Util
    {
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

        public static void WriteLog(string message)
        {
            Console.WriteLine(message);

            string logFolder = FtpConfig.LogDirectory;
            string currDate = "Log_" + DateTime.Now.ToString("ddMMyy") + ".log";
            string logPath = logFolder + "/" + currDate;

            try
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    LogFormat(message, sw);
                }
            }
            catch (Exception ex)
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
            Util.WriteLog("Clearing Temp folder..");

            foreach (FileInfo file in tempDirPath.GetFiles())
            {
                Util.WriteLog($"Deleting file {directory + file}..");
                file.Delete();
            }

            foreach (DirectoryInfo dir in tempDirPath.GetDirectories())
            {
                Util.WriteLog($"Deleting directory {directory + dir}..");
                dir.Delete(true);
            }
        }

        public static string GetClientVersion(string clientPath)
        {
            string version = string.Empty;
            try
            {
                version = System.Diagnostics.FileVersionInfo
                .GetVersionInfo(clientPath)
                .FileVersion;
            }
            catch (Exception e)
            {
                Util.WriteLog("GetClientVersion: Error => " + e.Message);
            }

            return version;
        }

        public static string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Util.WriteLog("GetLocalIpAddress: Error => " + e.Message);
            }

            return null;
        }

        public static string EncodeSpecialChar(string text)
        {
            int textLength;
            string tempText, encodedText;
            StringBuilder sb = new StringBuilder();

            try
            {
                textLength = text.Length;
                if (textLength > 0)
                {
                    for (int i = 0; i < textLength; i++)
                    {
                        tempText = text.Substring(i, 1);
                        switch (tempText)
                        {
                            case " ":
                                tempText = "%20";
                                break;
                            case "#":
                                tempText = "%23";
                                break;
                            case "%":
                                tempText = "%25";
                                break;
                            case "+":
                                tempText = "%2B";
                                break;
                            case "/":
                                tempText = "%2F";
                                break;
                            case "@":
                                tempText = "%40";
                                break;
                            case ":":
                                tempText = "%3A";
                                break;
                            case ";":
                                tempText = "%3B";
                                break;
                            default:
                                tempText = tempText;
                                break;
                        }

                        sb.Append(tempText);
                    }

                    encodedText = sb.ToString();
                    return encodedText;
                }
                else
                {
                    return text;
                }
            }
            catch (Exception ex)
            {
                Util.WriteLog("EncodeSpecialChar: Error => " + ex.Message);
                return text;
            }
        }
    }
}
