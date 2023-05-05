using FileDownloader.Config;
using SharpConfig;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WinSCP;

namespace FileDownloader
{
    internal class Helper
    {
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
            catch(Exception ex)
            {
                Helper.WriteLog("EncodeSpecialChar: Error => "+ex.Message);
                return text;
            }
        }

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
                FtpConfig.Fingerprint = section["Fingerprint"].StringValue.Trim();
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

        public static List<string> GetRemoteFiles(string protocol, string username, string password, string host, string fingerprint, string remoteDirectory)
        {
            #region update 040523
            //update 040523: implement winscp
            List<string> listFiles = new List<string>();
            try
            {
                if (protocol == "sftp://")
                {
                    if (fingerprint != string.Empty || fingerprint != null)
                    {
                        // Set up session options
                        //SessionOptions sessionOptions = new SessionOptions
                        //{
                        //    Protocol = Protocol.Sftp,
                        //    HostName = "192.168.172.85",
                        //    UserName = "root",
                        //    Password = "bni1234/",
                        //    SshHostKeyFingerprint = "ssh-ed25519 255 Qh+7f1+MdElZVp+owaWxKa8c2U9qhhxQpbj2rLxbgnc",
                        //};

                        SessionOptions sessionOptions = new SessionOptions
                        {
                            Protocol = Protocol.Sftp,
                            HostName = host,
                            UserName = username,
                            Password = password,
                            SshHostKeyFingerprint = fingerprint,
                        };

                        sessionOptions.AddRawSettings("FSProtocol", "2");

                        using (Session session = new Session())
                        {
                            // Connect
                            session.Open(sessionOptions);

                            // Your code
                            RemoteDirectoryInfo remoteDirectoryInfo = session.ListDirectory(remoteDirectory);

                            foreach (RemoteFileInfo remoteFileInfo in remoteDirectoryInfo.Files)
                            {
                                listFiles.Add(remoteFileInfo.Name);
                            }

                            session.Close();
                        }
                        return listFiles;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = host,
                        UserName = username,
                        Password = password,
                    };

                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(sessionOptions);

                        // Your code
                        RemoteDirectoryInfo remoteDirectoryInfo = session.ListDirectory(remoteDirectory);

                        foreach (RemoteFileInfo remoteFileInfo in remoteDirectoryInfo.Files)
                        {
                            if (remoteFileInfo.Name.Contains(".") && remoteFileInfo.Name != "..")
                            {
                                //sb.Append(remoteFileInfo.Name+"\n");
                                listFiles.Add(remoteFileInfo.Name);
                            }
                        }

                        session.Close();
                    }
                    return listFiles;
                };
            }
            catch (Exception e)
            {
                Helper.WriteLog("CheckFTPFiles: Error => " + e.Message);
                return null;
            }
            #endregion

            #region before
            //StringBuilder sb = new StringBuilder();
            //sb.Append(protocol + username + ":" + password + "@" + host + "/" + remotedirectory);
            //string url = sb.ToString();

            //FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            //request.Method = WebRequestMethods.Ftp.ListDirectory;

            //FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            //Stream stream = response.GetResponseStream();
            //StreamReader sr = new StreamReader(stream);

            //string filename = sr.ReadToEnd();

            //Console.WriteLine($"Status {response.StatusDescription}" +
            //    $"\n#################################################################\n");

            //sr.Close();
            //response.Close();

            //return filename.Replace(remotedirectory+"/", "").Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList()
            #endregion
        }

        #region update 050523
        //update 050523: implement winscp
        public static DateTime GetRemoteFileDate(string protocol, string username, string password, string host, 
            string fingerprint, string remoteFilePath, string downloadFilePath, string remoteFilename)
        {
            DateTime lastModified = DateTime.MinValue;
            try
            {
                if (protocol == "sftp://")
                {
                    if (fingerprint != string.Empty || fingerprint != null)
                    {
                        SessionOptions sessionOptions = new SessionOptions
                        {
                            Protocol = Protocol.Sftp,
                            HostName = host,
                            UserName = username,
                            Password = password,
                            SshHostKeyFingerprint = fingerprint,
                        };

                        sessionOptions.AddRawSettings("FSProtocol", "2");

                        using (Session session = new Session())
                        {
                            session.Open(sessionOptions);

                            lastModified = session.GetFileInfo(remoteFilePath).LastWriteTime;

                            session.Close();
                        }

                        return lastModified;
                    }
                    else
                    {
                        Helper.WriteLog("Fingerprint in the Config.ini is empty..");
                        DialogResult ans = MessageBox.Show("Fingerprint is empty! Please fill the Config.ini file correctly!",
                                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (ans == DialogResult.OK)
                        {
                            Helper.WriteLog($"Process aborted, program will shut down..");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                        else
                        {
                            Helper.WriteLog($"Process aborted, program will shut down..");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                        return lastModified;
                    }
                }
                else
                {
                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = host,
                        UserName = username,
                        Password = password,
                    };

                    using (Session session = new Session())
                    {
                        session.Open(sessionOptions);

                        lastModified = session.GetFileInfo(remoteFilePath).LastWriteTime;

                        session.Close();
                    }

                    return lastModified;
                }
            }
            catch (Exception e)
            {
                Helper.WriteLog("GetRemoteFileDate: Error => "+e.Message);
                return lastModified;
            }
        }

        public static long GetRemoteFileSize(string protocol, string username, string password, string host,
            string fingerprint, string remoteFilePath, string downloadFilePath, string remoteFilename)
        {
            long remoteFileSize = 0;
            try
            {
                if (protocol == "sftp://")
                {
                    if (fingerprint != string.Empty || fingerprint != null)
                    {
                        SessionOptions sessionOptions = new SessionOptions
                        {
                            Protocol = Protocol.Sftp,
                            HostName = host,
                            UserName = username,
                            Password = password,
                            SshHostKeyFingerprint = fingerprint,
                        };

                        sessionOptions.AddRawSettings("FSProtocol", "2");

                        using (Session session = new Session())
                        {
                            session.Open(sessionOptions);

                            remoteFileSize = session.GetFileInfo(remoteFilePath).Length;

                            session.Close();
                        }

                        return remoteFileSize;
                    }
                    else
                    {
                        Helper.WriteLog("Fingerprint in the Config.ini is empty..");
                        DialogResult ans = MessageBox.Show("Fingerprint is empty! Please fill the Config.ini file correctly!",
                                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (ans == DialogResult.OK)
                        {
                            Helper.WriteLog($"Process aborted, program will shut down..");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                        else
                        {
                            Helper.WriteLog($"Process aborted, program will shut down..");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                        return remoteFileSize;
                    }
                }
                else
                {
                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = host,
                        UserName = username,
                        Password = password,
                    };

                    using (Session session = new Session())
                    {
                        session.Open(sessionOptions);

                        remoteFileSize = session.GetFileInfo(remoteFilePath).Length;

                        session.Close();
                    }

                    return remoteFileSize;
                }
            }
            catch (Exception e)
            {
                Helper.WriteLog("GetRemoteFileSize: Error => "+e.Message);
                return remoteFileSize;
            }
        }
        #endregion

        #region before
        //public static DateTime GetFTPFilesVersion(string url)
        //{
        //    DateTime lastModified = DateTime.MinValue;
        //    try
        //    {
        //        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
        //        request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
        //        request.Proxy = null;

        //        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        //        response.Close();

        //        lastModified = response.LastModified;
        //        return lastModified;
        //    }
        //    catch (Exception e)
        //    {
        //        Helper.WriteLog("GetFTPFilesVersion: Error => "+e.Message);
        //        return lastModified;
        //    }
        //}
        #endregion

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
