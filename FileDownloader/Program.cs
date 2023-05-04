using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FileDownloader.Config;
using WinSCP;

namespace FileDownloader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ExecProgram();
        }

        private static void ExecProgram()
        {
            bool isOk = false;
            isOk = Helper.ReadConfig();
            if (isOk)
            {
                StartProcess(FtpConfig.Protocol, FtpConfig.Username, FtpConfig.Password, FtpConfig.Host, 
                    FtpConfig.RemoteDirectory, FtpConfig.DownloadDirectory, FtpConfig.TempDirectory, FtpConfig.TargetDirectory, FtpConfig.LogDirectory);
            }
            else
            {
                Helper.WriteLog("Error reading config file");
                Thread.Sleep(5000);
            }
        }

        private static void StartProcess(string protocol, string username, string password, string host, 
            string remoteDirectory, string downloadDirectory, string tempDirectory, string targetDirectory, string logDirectory)
        {
            string remoteServer = host + remoteDirectory;
            bool tempDirExist = Helper.CheckDirectory(tempDirectory);
            bool downloadDirExist = Helper.CheckDirectory(downloadDirectory);
            bool targetDirExist = Helper.CheckDirectory(targetDirectory);
            bool logDirExist = Helper.CheckDirectory(logDirectory);
            bool isBackupSuccess = false, isDownloadSuccess = false, isFTPConnectSuccess = false;

            if(tempDirExist && downloadDirExist && targetDirExist && logDirExist)
            {

                Helper.WriteLog($"Connecting to FTP server..");
                do
                {

                    if(protocol == "sftp://")
                    {
                        // Set up session options
                        SessionOptions sessionOptions = new SessionOptions
                        {
                            Protocol = Protocol.Sftp,
                            HostName = "192.168.172.85",
                            UserName = "root",
                            Password = "bni1234/",
                            SshHostKeyFingerprint = "ssh-ed25519 255 Qh+7f1+MdElZVp+owaWxKa8c2U9qhhxQpbj2rLxbgnc",
                        };

                        sessionOptions.AddRawSettings("FSProtocol", "2");

                        using (Session session = new Session())
                        {
                            // Connect
                            session.Open(sessionOptions);

                            // Your code
                        }
                    }

                    //step 1: getting the ftp files
                    var files = Helper.GetFTPFiles(protocol, username, password, host, remoteDirectory);
                    if (files != null)
                    {
                        int FileExtracted = 0;
                        int FileProcessed = 0;

                        List<string> filteredFiles = new List<string>();

                        foreach (var file in files)
                        {
                            if (file.Contains("."))
                            {
                                filteredFiles.Add(file);
                            }
                        }

                        Console.WriteLine($"\n#################################################################\n" +
                            $"                         DIGICSLITE UPDATER" +
                            $"\n#################################################################\n");

                        Helper.WriteLog($"Program started, process will begin..");

                        Helper.ClearTempFolder(tempDirectory);

                        Helper.WriteLog($"{filteredFiles.Count} files available in FTP Server {remoteServer}");

                        int filesToBeUpdated = 0;
                        int filesToBeDownloaded = 0;

                        foreach (string file in filteredFiles)
                        {
                            Helper.WriteLog($"{file}");
                        }

                        Console.WriteLine("\n#################################################################\n");

                        foreach (string file in filteredFiles)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(protocol + username + ":" + password + "@" + host + "/" + remoteDirectory + "/" + file);
                            string url = sb.ToString();
                            string downloadFilePath = downloadDirectory + file;

                            string[] existingFiles = Directory.GetFiles(downloadDirectory);

                            if (existingFiles.Length > 0)
                            {
                                foreach (var existingFile in existingFiles)
                                {
                                    string existingFilename = existingFile.Replace(downloadDirectory, "");

                                    if (existingFilename == file)
                                    {
                                        //step 2: checking the version package
                                        DateTime lastModified = Process.GetFTPFilesVersion(url);
                                        DateTime existingLastModified = File.GetLastWriteTime(existingFile);

                                        if (lastModified > existingLastModified)
                                        {
                                            //step 2.1: backing up the last version package
                                            isBackupSuccess = Process.BackupFiles(existingFile, downloadDirectory);

                                            if (isBackupSuccess)
                                            {

                                                #region update 270423
                                                //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                                do
                                                {
                                                    isDownloadSuccess = Process.DownloadFile(url, file, remoteServer, downloadFilePath);
                                                    if (isDownloadSuccess == true)
                                                    {

                                                        ++filesToBeDownloaded;
                                                        FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                        if (FileExtracted == 0)
                                                        {
                                                            Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                            isDownloadSuccess = false;
                                                        }
                                                        else
                                                        {
                                                            FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                            filesToBeUpdated = FileProcessed;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Helper.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                        isDownloadSuccess = false;
                                                    }
                                                }
                                                while (isDownloadSuccess == false);
                                                #endregion

                                                Console.WriteLine($"\n\n#################################################################\n");
                                            }
                                            else
                                            {
                                                Helper.WriteLog("Backuping the last version package failed, process aborted..");
                                            }
                                        }
                                        else if (lastModified < existingLastModified)
                                        {
                                            //step 2.2: checking if the existing file had different size due to interruption download process
                                            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                                            request.Method = WebRequestMethods.Ftp.GetFileSize;
                                            request.Proxy = null;

                                            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                                            FileInfo fileInfo = new FileInfo(downloadFilePath);

                                            response.Close();

                                            if (fileInfo.Length < response.ContentLength)
                                            {
                                                Helper.WriteLog($"Checking the existing {file} file size and the {file} file at FTP Server..");

                                                Helper.WriteLog($"Existing file: {fileInfo.Length} <> File at FTP Server: {response.ContentLength}");

                                                Helper.WriteLog($"The existing {existingFilename} file size were different, retrying the download process..");

                                                #region update 270423
                                                //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                                do
                                                {
                                                    isDownloadSuccess = Process.DownloadFile(url, file, remoteServer, downloadFilePath);
                                                    if (isDownloadSuccess == true)
                                                    {

                                                        ++filesToBeDownloaded;
                                                        FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                        if (FileExtracted == 0)
                                                        {
                                                            Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                            isDownloadSuccess = false;
                                                        }
                                                        else
                                                        {
                                                            FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                            filesToBeUpdated = FileProcessed;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Helper.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                        isDownloadSuccess = false;
                                                    }
                                                }
                                                while (isDownloadSuccess == false);
                                                #endregion

                                                Console.WriteLine($"\n\n#################################################################\n");
                                            }
                                            else
                                            {
                                                Helper.WriteLog($"{existingFilename} file version is the latest, update aborted..");
                                            }
                                        }
                                        else
                                        {
                                            Helper.WriteLog($"{existingFilename} file version is the latest, update aborted..");
                                        }
                                    }
                                }

                                if (!File.Exists(downloadFilePath))
                                {
                                    #region update 270423
                                    //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                    do
                                    {
                                        isDownloadSuccess = Process.DownloadFile(url, file, remoteServer, downloadFilePath);
                                        if (isDownloadSuccess == true)
                                        {

                                            ++filesToBeDownloaded;
                                            FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                            if (FileExtracted == 0)
                                            {
                                                Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                isDownloadSuccess = false;
                                            }
                                            else
                                            {
                                                FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                filesToBeUpdated = FileProcessed;
                                            }
                                        }
                                        else
                                        {
                                            Helper.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                            isDownloadSuccess = false;
                                        }
                                    }
                                    while (isDownloadSuccess == false);
                                    #endregion

                                    Console.WriteLine($"\n#################################################################\n");
                                }
                            }
                            else
                            {
                                #region update 270423
                                //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                do
                                {
                                    isDownloadSuccess = Process.DownloadFile(url, file, remoteServer, downloadFilePath);
                                    if (isDownloadSuccess == true)
                                    {

                                        ++filesToBeDownloaded;
                                        FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                        if (FileExtracted == 0)
                                        {
                                            Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                            isDownloadSuccess = false;
                                        }
                                        else
                                        {
                                            FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                            filesToBeUpdated = FileProcessed;
                                        }
                                    }
                                    else
                                    {
                                        Helper.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                        isDownloadSuccess = false;
                                    }
                                }
                                while (isDownloadSuccess == false);
                                #endregion

                                Console.WriteLine($"\n#################################################################\n");
                            }
                        }

                        if (filesToBeDownloaded == 0 && filesToBeUpdated == 0)
                        {
                            Helper.WriteLog("No files downloaded and updated");
                        }
                        else if (FileExtracted > 0 && FileProcessed > 0)
                        {
                            if (filesToBeUpdated > 0)
                            {
                                Helper.WriteLog($"{filesToBeDownloaded} files downloaded at {downloadDirectory}");
                                Helper.WriteLog($"{FileProcessed} files updated at {targetDirectory}");
                                Helper.WriteLog("Update process done");
                            }
                            else
                            {
                                Helper.WriteLog($"{filesToBeDownloaded} files downloaded at {downloadDirectory}");
                                Helper.WriteLog($"{FileProcessed} files extracted at {targetDirectory}");
                                Helper.WriteLog("Download process done");
                            }
                        }

                        Helper.ClearTempFolder(tempDirectory);

                        Helper.WriteLog($"Process has finished, program will shut down..\n");
                        Console.WriteLine($"\n#################################################################\n");
                        isFTPConnectSuccess = true;
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        Helper.WriteLog("FTP connection error, please recheck the FTP server connection");
                        DialogResult ans = MessageBox.Show("FTP connection error, please recheck the FTP server connection!",
                                                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (ans == DialogResult.OK)
                        {
                            Helper.WriteLog("Retrying connection to FTP server..");
                            isFTPConnectSuccess = false;
                            //Helper.WriteLog($"Process aborted, program will shut down..");
                            //Environment.Exit(0);
                        }
                        else
                        {
                            Helper.WriteLog("Retrying connection to FTP server..");
                            isFTPConnectSuccess = false;
                            //Helper.WriteLog($"Process aborted, program will shut down..");
                            //Environment.Exit(0);
                        }
                        //Helper.WriteLog($"Process aborted, program will shut down..\n");

                        //Thread.Sleep(5000);
                    }
                } while (isFTPConnectSuccess == false);

            }
        }
    }
}
