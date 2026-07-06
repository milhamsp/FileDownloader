using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using DigiCSLiteUpdater;
using DigiCSLiteUpdater.Config;
using DigiCSLiteUpdater.Model;
using FileDownloader.Config;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using WinSCP;
using static System.Net.Mime.MediaTypeNames;

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
            #region header
            Console.WriteLine($"\n#################################################################\n" +
                $"                        DIGICSLITE UPDATER" +
                $"\n#################################################################\n");
            #endregion

            Util.WriteLog($"\nProgram started, process will begin..");

            bool isOk = false;
            isOk = Helper.ReadConfig();
            bool isExecDateTimeExist = false;
            isExecDateTimeExist = Helper.GetExecDatetime();

            if (isOk)
                RunScheduler();

            //if (isOk && isExecDateTimeExist)
            //{
            //    //StartProcess(FtpConfig.Protocol, FtpConfig.Username, FtpConfig.Password, FtpConfig.Host, FtpConfig.Fingerprint,
            //    //    FtpConfig.RemoteDirectory, FtpConfig.DownloadDirectory, FtpConfig.TempDirectory, FtpConfig.TargetDirectory, FtpConfig.LogDirectory);
            //    RunScheduler();
            //}
            //else if (isOk && !isExecDateTimeExist)
            //{
            //    AppData.ExecTime = "16.00";
            //    RunScheduler();
            //}
            //else
            //{
            //    Util.WriteLog("Error reading config file");
            //    Thread.Sleep(5000);
            //}
        }

        private static void RunScheduler()
        {
            bool onLoop = true; 
            Util.WriteLog("Running scheduler...");
            do
            {
                DateTime currentTime = DateTime.Now;
                string execTime = AppData.ExecTime;
                string[] splitExecTime = execTime.Split('.');

                if (!AppData.SuccessProcess)
                {
                    //StartProcess(FtpConfig.Protocol, FtpConfig.Username, FtpConfig.Password, FtpConfig.Host, FtpConfig.Fingerprint,
                    //        FtpConfig.RemoteDirectory, FtpConfig.DownloadDirectory, FtpConfig.TempDirectory, FtpConfig.TargetDirectory, FtpConfig.LogDirectory);

                    Util.WriteLog("StartProcess executed at: " + currentTime);
                    StartProcess(FtpConfig.Protocol, FtpConfig.Username, FtpConfig.Password, FtpConfig.Host, FtpConfig.Fingerprint,
                        DirectoryConfig.RemoteDirectory, DirectoryConfig.DownloadDirectory, DirectoryConfig.TempDirectory, DirectoryConfig.TargetDirectory, DirectoryConfig.LogDirectory,
                        ConnectionConfig.ConnectionUrl);

                    //if (splitExecTime.Length > 1)
                    //{
                    //    DateTime scheduledTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, int.Parse(splitExecTime[0]), int.Parse(splitExecTime[1]), 0);

                    //    if (currentTime >= scheduledTime && currentTime < scheduledTime.AddMinutes(1))
                    //    {
                    //        Util.WriteLog("StartProcess executed at: " + currentTime);
                    //        StartProcess(FtpConfig.Protocol, FtpConfig.Username, FtpConfig.Password, FtpConfig.Host, FtpConfig.Fingerprint,
                    //            DirectoryConfig.RemoteDirectory, DirectoryConfig.DownloadDirectory, DirectoryConfig.TempDirectory, DirectoryConfig.TargetDirectory, DirectoryConfig.LogDirectory,
                    //            ConnectionConfig.ConnectionUrl);
                    //    }
                    //}

                    Thread.Sleep(10000);
                }
                else
                {
                    onLoop = false;
                }
            } while (onLoop);
        }

        private static void StartProcess(string protocol, string username, string password, string host, string fingerprint,
            string remoteDirectory, string downloadDirectory, string tempDirectory, string targetDirectory, string logDirectory,
            string remoteAddress)
        {
            string remoteServer = host + remoteDirectory;
            bool tempDirExist = Util.CheckDirectory(tempDirectory);
            bool downloadDirExist = Util.CheckDirectory(downloadDirectory);
            bool targetDirExist = Util.CheckDirectory(targetDirectory);
            bool logDirExist = Util.CheckDirectory(logDirectory);
            bool isBackupSuccess = false, isDownloadSuccess = false, isFTPCycleOk = false, isRemoteCycleOk = false;

            int filesUpdated = 0;
            int filesDownloaded = 0;

            //encode special char for username & pass
            //username = Helper.EncodeSpecialChar(username);
            //password = Helper.EncodeSpecialChar(password);;

            if (tempDirExist && downloadDirExist && targetDirExist && logDirExist)
            {
                if (ClientConfig.Mode.Equals("1")) //remote
                {
                    //step 1: checking connection to remote
                    Util.WriteLog($"Checking connection to remote : {Helper.MaskedBaseUrl(remoteAddress)}");
                    bool isConnect = Helper.IsAddressAvailable(remoteAddress);

                    do
                    {
                        Util.ClearTempFolder(tempDirectory);

                        //step 2: checking version
                        Util.WriteLog($"Checking version..");

                        bool checkUpdate = false, isUpdateExist = false;
                        RspCheckUpdate rspCheckUpdate;

                        (checkUpdate, rspCheckUpdate) = Helper.PostCheckUpdate();
                        if (rspCheckUpdate != null)
                            isUpdateExist = rspCheckUpdate.HasUpdate;

                        //step 3: check for update success
                        if (checkUpdate)
                        {
                            int FileExtracted = 0;
                            int FileProcessed = 0;
                            if (isUpdateExist)
                            {
                                ++filesDownloaded; //assume file is already downloaded

                                Util.WriteLog($"Program started, process will begin..");

                                Util.ClearTempFolder(tempDirectory);

                                List<string> existingFiles = Directory.GetFiles(downloadDirectory)
                                    .Where(file =>
                                    {
                                        string ext = Path.GetExtension(file);
                                        return !ext.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
                                        && !ext.Equals(".downloaded", StringComparison.OrdinalIgnoreCase);
                                    })
                                    .ToList();

                                //check for existing update
                                if (existingFiles.Any())
                                {
                                    int backedUpFile = 0;
                                    foreach (string file in existingFiles)
                                    {
                                        string filename = Path.GetFileName(file);

                                        //backup old update
                                        isBackupSuccess = Process.BackupMultipartFiles(filename, downloadDirectory);
                                        if (isBackupSuccess)
                                        {
                                            ++backedUpFile;
                                        }
                                        else
                                        {
                                            Util.WriteLog("Backuping the last version package failed, process aborted..");
                                        }
                                    }

                                    if (backedUpFile > 0)
                                    {
                                        if (backedUpFile == existingFiles.Count)
                                        {
                                            //update status in db
                                            bool updateStatus = Helper.PostUpdateStatus(2); //start updating

                                            //rename file to existing file
                                            bool renameFile = Util.RenameFile(downloadDirectory);

                                            //extract files
                                            if (updateStatus)
                                            {
                                                FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                if (FileExtracted > 0)
                                                {
                                                    //update files
                                                    FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                    filesUpdated = FileProcessed;

                                                    //update status in db
                                                    updateStatus = Helper.PostUpdateStatus(3); //update done
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Util.WriteLog("There are unsuccessful backup attempt");
                                        }
                                    }
                                }
                                else
                                {
                                    //update status in db
                                    bool updateStatus = Helper.PostUpdateStatus(2); //start updating

                                    //rename file to existing file
                                    bool renameFile = Util.RenameFile(downloadDirectory);

                                    //extract files
                                    if (updateStatus)
                                    {
                                        FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                        if (FileExtracted > 0)
                                        {
                                            //update files
                                            FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                            filesUpdated = FileProcessed;

                                            //update status in db
                                            updateStatus = Helper.PostUpdateStatus(3); //update done
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Util.WriteLog($"{rspCheckUpdate.Message}");
                            }

                            if (filesDownloaded == 0 && filesUpdated == 0)
                            {
                                Util.WriteLog("No files downloaded and updated");
                                AppData.SuccessProcess = true;
                            }
                            else if (FileExtracted > 0 && FileProcessed > 0)
                            {
                                if (filesUpdated > 0)
                                {
                                    Util.WriteLog($"{filesDownloaded} files downloaded at {downloadDirectory}");
                                    Util.WriteLog($"{FileProcessed} files updated at {targetDirectory}");
                                    Util.WriteLog("Update process done");
                                    AppData.SuccessProcess = true;
                                }
                                else
                                {
                                    Util.WriteLog($"{filesDownloaded} files downloaded at {downloadDirectory}");
                                    Util.WriteLog($"{FileProcessed} files extracted at {targetDirectory}");
                                    Util.WriteLog("Download process done");
                                    AppData.SuccessProcess = true;
                                }
                            }

                            isRemoteCycleOk = true;
                        }
                        else
                        {
                            Util.WriteLog($"Check for update fail..\n");
                            //isRemoteCycleOk = true;
                        }

                        Util.ClearTempFolder(tempDirectory);

                        AppData.SuccessProcess = true;
                        Util.WriteLog($"Process has finished, program will shut down..\n");
                        Console.WriteLine($"#################################################################\n");

                        Thread.Sleep(5000);
                    } while (!isRemoteCycleOk);
                }
                else
                {
                    Util.WriteLog($"Connecting to FTP server..");
                    do
                    {
                        //step 1: getting the ftp files
                        var files = Helper.GetRemoteFiles(protocol, username, password, host, fingerprint, remoteDirectory);

                        if (files != null)
                        {
                            int FileExtracted = 0;
                            int FileProcessed = 0;

                            List<string> filteredFiles = new List<string>();
                            List<string> existingFiles = Directory.GetFiles(downloadDirectory).ToList();

                            if (files.Count > 1)
                            {
                                foreach (var file in files)
                                {
                                    if (file.Contains(".part"))
                                    {
                                        filteredFiles.Add(file);
                                    }
                                }
                            }
                            else if (files.Count > 0)
                            {
                                foreach (var file in files)
                                {
                                    if (file.Contains("."))
                                    {
                                        filteredFiles.Add(file);
                                    }
                                }
                            }

                            Util.ClearTempFolder(tempDirectory);

                            Util.WriteLog($"{filteredFiles.Count} files available in FTP Server {remoteServer}");

                            foreach (string file in filteredFiles)
                            {
                                Util.WriteLog($"{file}");
                            }

                            Console.WriteLine("\n#################################################################\n");

                            #region multipart file handler
                            if (filteredFiles.Count > 1)
                            {
                                if (existingFiles != null)
                                {
                                    if (existingFiles.Count > 0)
                                    {
                                        string firstRemoteFile = filteredFiles.First();
                                        string filenameReference = Regex.Replace(firstRemoteFile, @"\d", "");
                                        filenameReference = filenameReference.Replace(".part", "").Replace(".rar", "");
                                        string downloadFilePath = downloadDirectory + firstRemoteFile;
                                        string remoteFilePath = remoteDirectory + firstRemoteFile;

                                        string firstExistingFile = existingFiles.First();

                                        DateTime lastModified = Helper.GetRemoteFileDate(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, firstRemoteFile);
                                        DateTime existingLastModified = File.GetLastWriteTime(firstExistingFile);
                                        Util.WriteLog($"Remote File : {lastModified} <> Local File : {existingLastModified}");

                                        if (lastModified > existingLastModified)
                                        {

                                            isBackupSuccess = Process.BackupMultipartFiles(filenameReference, downloadDirectory);

                                            if (isBackupSuccess == true)
                                            {
                                                do
                                                {
                                                    foreach (var file in filteredFiles)
                                                    {
                                                        downloadFilePath = downloadDirectory + file;
                                                        remoteFilePath = remoteDirectory + file;

                                                        do
                                                        {
                                                            isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                        }
                                                        while (isDownloadSuccess == false);

                                                        ++filesDownloaded;
                                                    }

                                                    FileExtracted = Process.SuccessExtractMultipartFilesToTemp(filteredFiles, downloadDirectory, tempDirectory);

                                                    if (FileExtracted == 0)
                                                    {
                                                        Util.WriteLog($"Extracting file failure, retrying to redownload the files..");
                                                        filesDownloaded = 0;
                                                        isDownloadSuccess = false;
                                                    }
                                                    else
                                                    {
                                                        FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                        filesUpdated = FileProcessed;
                                                        isDownloadSuccess = true;
                                                    }
                                                }
                                                while (isDownloadSuccess == false);
                                            }
                                        }
                                        else if (lastModified < existingLastModified)
                                        {

                                            if (filteredFiles.Count != existingFiles.Count)
                                            {
                                                Util.WriteLog($"Checking the existing total files and the total files at Remote Server..");

                                                Util.WriteLog($"Existing file: {existingFiles.Count} <> File at FTP Server: {filteredFiles.Count}");

                                                Util.WriteLog($"The existing total files were different, retrying the download process..");

                                                do
                                                {
                                                    foreach (var file in filteredFiles)
                                                    {
                                                        downloadFilePath = downloadDirectory + file;
                                                        remoteFilePath = remoteDirectory + file;

                                                        do
                                                        {
                                                            isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                        }
                                                        while (isDownloadSuccess == false);

                                                        ++filesDownloaded;
                                                    }

                                                    FileExtracted = Process.SuccessExtractMultipartFilesToTemp(filteredFiles, downloadDirectory, tempDirectory);

                                                    if (FileExtracted == 0)
                                                    {
                                                        Util.WriteLog($"Extracting file failure, retrying to redownload the files..");
                                                        filesDownloaded = 0;
                                                        isDownloadSuccess = false;
                                                    }
                                                    else
                                                    {
                                                        FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                        filesUpdated = FileProcessed;
                                                        isDownloadSuccess = true;
                                                    }
                                                }
                                                while (isDownloadSuccess == false);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        do
                                        {
                                            foreach (var file in filteredFiles)
                                            {
                                                string downloadFilePath = downloadDirectory + file;
                                                string remoteFilePath = remoteDirectory + file;

                                                do
                                                {
                                                    isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                }
                                                while (isDownloadSuccess == false);

                                                ++filesDownloaded;
                                            }

                                            FileExtracted = Process.SuccessExtractMultipartFilesToTemp(filteredFiles, downloadDirectory, tempDirectory);

                                            if (FileExtracted == 0)
                                            {
                                                Util.WriteLog($"Extracting file failure, retrying to redownload the files..");
                                                filesDownloaded = 0;
                                                isDownloadSuccess = false;
                                            }
                                            else
                                            {
                                                FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                filesUpdated = FileProcessed;
                                                isDownloadSuccess = true;
                                            }
                                        }
                                        while (isDownloadSuccess == false);
                                    }
                                }
                                else
                                {
                                    do
                                    {
                                        foreach (var file in filteredFiles)
                                        {
                                            string downloadFilePath = downloadDirectory + file;
                                            string remoteFilePath = remoteDirectory + file;

                                            do
                                            {
                                                isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                            }
                                            while (isDownloadSuccess == false);

                                            ++filesDownloaded;
                                        }

                                        FileExtracted = Process.SuccessExtractMultipartFilesToTemp(filteredFiles, downloadDirectory, tempDirectory);

                                        if (FileExtracted == 0)
                                        {
                                            Util.WriteLog($"Extracting file failure, retrying to redownload the files..");
                                            filesDownloaded = 0;
                                            isDownloadSuccess = false;
                                        }
                                        else
                                        {
                                            FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                            filesUpdated = FileProcessed;
                                            isDownloadSuccess = true;
                                        }
                                    }
                                    while (isDownloadSuccess == false);
                                }
                            }
                            #endregion

                            #region single file handler
                            else if (filteredFiles.Count > 0)
                            {
                                string firstRemoteFile = filteredFiles.First();
                                string filenameReference = Regex.Replace(firstRemoteFile, @"\d", "");
                                filenameReference = filenameReference.Replace(".part", "").Replace(".rar", "");

                                foreach (string file in filteredFiles)
                                {
                                    //StringBuilder sb = new StringBuilder();
                                    //sb.Append(protocol + username + ":" + password + "@" + host + "/" + remoteDirectory + "/" + file);
                                    //string url = sb.ToString();

                                    string downloadFilePath = downloadDirectory + file;
                                    string remoteFilePath = remoteDirectory + file;

                                    if (existingFiles != null)
                                    {
                                        if (existingFiles.Count > 1)
                                        {
                                            string firstExistingFile = existingFiles.First();

                                            DateTime lastModified = Helper.GetRemoteFileDate(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                            DateTime existingLastModified = File.GetLastWriteTime(firstExistingFile);

                                            if (lastModified > existingLastModified)
                                            {
                                                isBackupSuccess = Process.BackupMultipartFiles(filenameReference, downloadDirectory);

                                                if (isBackupSuccess)
                                                {
                                                    #region update 270423
                                                    //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                                    do
                                                    {
                                                        //update 050523: implement winscp
                                                        isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                        if (isDownloadSuccess == true)
                                                        {
                                                            ++filesDownloaded;
                                                            FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                            if (FileExtracted == 0)
                                                            {
                                                                Util.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                                isDownloadSuccess = false;
                                                            }
                                                            else
                                                            {
                                                                FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                                filesUpdated = FileProcessed;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Util.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                            isDownloadSuccess = false;
                                                        }
                                                    }
                                                    while (isDownloadSuccess == false);
                                                    #endregion

                                                    Console.WriteLine($"\n\n#################################################################\n");
                                                }
                                                else
                                                {
                                                    Util.WriteLog("Backuping the last version package failed, process aborted..");
                                                }
                                            }
                                        }
                                        #endregion
                                        else if (existingFiles.Count == 1)
                                        {
                                            foreach (var existingFile in existingFiles)
                                            {
                                                string existingFilename = existingFile.Replace(downloadDirectory, "");

                                                if (existingFilename == file)
                                                {
                                                    //step 2: checking the version package
                                                    DateTime lastModified = Helper.GetRemoteFileDate(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                    DateTime existingLastModified = File.GetLastWriteTime(existingFile);
                                                    Util.WriteLog($"Remote File : {lastModified} <> Local File : {existingLastModified}");

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
                                                                //update 050523: implement winscp
                                                                isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                                if (isDownloadSuccess == true)
                                                                {
                                                                    ++filesDownloaded;
                                                                    FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                                    if (FileExtracted == 0)
                                                                    {
                                                                        Util.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                                        isDownloadSuccess = false;
                                                                    }
                                                                    else
                                                                    {
                                                                        FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                                        filesUpdated = FileProcessed;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Util.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                                    isDownloadSuccess = false;
                                                                }
                                                            }
                                                            while (isDownloadSuccess == false);
                                                            #endregion

                                                            Console.WriteLine($"\n\n#################################################################\n");
                                                        }
                                                        else
                                                        {
                                                            Util.WriteLog("Backuping the last version package failed, process aborted..");
                                                        }
                                                    }
                                                    else if (lastModified < existingLastModified)
                                                    {
                                                        //step 2.2: checking if the existing file had different size due to interruption download process
                                                        FileInfo fileInfo = new FileInfo(downloadFilePath);

                                                        long remoteFileSize = Helper.GetRemoteFileSize(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);

                                                        if (fileInfo.Length < remoteFileSize)
                                                        {
                                                            Util.WriteLog($"Checking the existing {file} file size and the {file} file at FTP Server..");

                                                            Util.WriteLog($"Existing file: {fileInfo.Length} <> File at FTP Server: {remoteFileSize}");

                                                            Util.WriteLog($"The existing {existingFilename} file size were different, retrying the download process..");

                                                            #region update 270423
                                                            //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                                            do
                                                            {
                                                                isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                                if (isDownloadSuccess == true)
                                                                {

                                                                    ++filesDownloaded;
                                                                    FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                                    if (FileExtracted == 0)
                                                                    {
                                                                        Util.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                                        isDownloadSuccess = false;
                                                                    }
                                                                    else
                                                                    {
                                                                        FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                                        filesUpdated = FileProcessed;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Util.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                                    isDownloadSuccess = false;
                                                                }
                                                            }
                                                            while (isDownloadSuccess == false);
                                                            #endregion

                                                            Console.WriteLine($"\n\n#################################################################\n");
                                                        }
                                                        else
                                                        {
                                                            Util.WriteLog($"{existingFilename} file version is the latest, update aborted..");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Util.WriteLog($"{existingFilename} file version is the latest, update aborted..");
                                                    }
                                                }
                                            }

                                            if (!File.Exists(downloadFilePath))
                                            {
                                                #region update 270423
                                                //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                                do
                                                {
                                                    isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                    if (isDownloadSuccess == true)
                                                    {

                                                        ++filesDownloaded;
                                                        FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                        if (FileExtracted == 0)
                                                        {
                                                            Util.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                            isDownloadSuccess = false;
                                                        }
                                                        else
                                                        {
                                                            FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                            filesUpdated = FileProcessed;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Util.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
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
                                                isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                if (isDownloadSuccess == true)
                                                {

                                                    ++filesDownloaded;
                                                    FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                    if (FileExtracted == 0)
                                                    {
                                                        Util.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                        isDownloadSuccess = false;
                                                    }
                                                    else
                                                    {
                                                        FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                        filesUpdated = FileProcessed;
                                                    }
                                                }
                                                else
                                                {
                                                    Util.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                    isDownloadSuccess = false;
                                                }
                                            }
                                            while (isDownloadSuccess == false);
                                            #endregion

                                            Console.WriteLine($"\n#################################################################\n");
                                        }
                                    }
                                    #region test if existing file only 1

                                    else
                                    {
                                        #region update 270423
                                        //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
                                        do
                                        {
                                            isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                            if (isDownloadSuccess == true)
                                            {

                                                ++filesDownloaded;
                                                FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                if (FileExtracted == 0)
                                                {
                                                    Util.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
                                                    isDownloadSuccess = false;
                                                }
                                                else
                                                {
                                                    FileProcessed = Process.SuccessCutFilesToTarget(tempDirectory, targetDirectory);
                                                    filesUpdated = FileProcessed;
                                                }
                                            }
                                            else
                                            {
                                                Util.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
                                                isDownloadSuccess = false;
                                            }
                                        }
                                        while (isDownloadSuccess == false);
                                        #endregion

                                        Console.WriteLine($"\n#################################################################\n");
                                    }
                                }
                            }
                            #endregion

                            if (filesDownloaded == 0 && filesUpdated == 0)
                            {
                                Util.WriteLog("No files downloaded and updated");
                                AppData.SuccessProcess = true;
                            }
                            else if (FileExtracted > 0 && FileProcessed > 0)
                            {
                                if (filesUpdated > 0)
                                {
                                    Util.WriteLog($"{filesDownloaded} files downloaded at {downloadDirectory}");
                                    Util.WriteLog($"{FileProcessed} files updated at {targetDirectory}");
                                    Util.WriteLog("Update process done");
                                    AppData.SuccessProcess = true;
                                }
                                else
                                {
                                    Util.WriteLog($"{filesDownloaded} files downloaded at {downloadDirectory}");
                                    Util.WriteLog($"{FileProcessed} files extracted at {targetDirectory}");
                                    Util.WriteLog("Download process done");
                                    AppData.SuccessProcess = true;
                                }
                            }

                            Util.ClearTempFolder(tempDirectory);

                            AppData.SuccessProcess = true;
                            Util.WriteLog($"Process has finished, program will shut down..\n");
                            Console.WriteLine($"#################################################################\n");

                            isFTPCycleOk = true;

                            Thread.Sleep(5000);
                        }
                        else
                        {
                            Util.WriteLog("FTP connection error, please recheck the FTP server connection");
                            DialogResult ans = MessageBox.Show("FTP connection error, please recheck the FTP server connection!",
                                                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            if (ans == DialogResult.OK)
                            {
                                Util.WriteLog("Retrying connection to FTP server..");
                                isFTPCycleOk = false;
                                //Util.WriteLog($"Process aborted, program will shut down..");
                                //Environment.Exit(0);
                            }
                            else
                            {
                                Util.WriteLog("Retrying connection to FTP server..");
                                isFTPCycleOk = false;
                                //Util.WriteLog($"Process aborted, program will shut down..");
                                //Environment.Exit(0);
                            }
                            //Util.WriteLog($"Process aborted, program will shut down..\n");

                            //Thread.Sleep(5000);
                        }
                    } while (isFTPCycleOk == false);
                }

            }
        }

        #region winscp example
        //private static void FileTransferred(object sender, TransferEventArgs e)
        //{
        //    if (e.Error == null)
        //    {
        //        Console.WriteLine("Download of {0} succeeded", e.FileName);
        //    }
        //    else
        //    {
        //        Console.WriteLine("Download of {0} failed: {1}", e.FileName, e.Error);
        //    }

        //    if (e.Chmod != null)
        //    {
        //        if (e.Chmod.Error == null)
        //        {
        //            Console.WriteLine(
        //                "Permissions of {0} set to {1}", e.Chmod.FileName, e.Chmod.FilePermissions);
        //        }
        //        else
        //        {
        //            Console.WriteLine(
        //                "Setting permissions of {0} failed: {1}", e.Chmod.FileName, e.Chmod.Error);
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Permissions of {0} kept with their defaults", e.Destination);
        //    }

        //    if (e.Touch != null)
        //    {
        //        if (e.Touch.Error == null)
        //        {
        //            Console.WriteLine(
        //                "Timestamp of {0} set to {1}", e.Touch.FileName, e.Touch.LastWriteTime);
        //        }
        //        else
        //        {
        //            Console.WriteLine(
        //                "Setting timestamp of {0} failed: {1}", e.Touch.FileName, e.Touch.Error);
        //        }
        //    }
        //    else
        //    {
        //        // This should never happen during "local to remote" synchronization
        //        Console.WriteLine(
        //            "Timestamp of {0} kept with its default (current time)", e.Destination);
        //    }
        //}
        #endregion
    }
}
