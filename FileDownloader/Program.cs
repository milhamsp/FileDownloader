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
            bool isOk = false;
            isOk = Helper.ReadConfig();
            if (isOk)
            {
                StartProcess(FtpConfig.Protocol, FtpConfig.Username, FtpConfig.Password, FtpConfig.Host, FtpConfig.Fingerprint,
                    FtpConfig.RemoteDirectory, FtpConfig.DownloadDirectory, FtpConfig.TempDirectory, FtpConfig.TargetDirectory, FtpConfig.LogDirectory);
            }
            else
            {
                Helper.WriteLog("Error reading config file");
                Thread.Sleep(5000);
            }
        }

        private static void StartProcess(string protocol, string username, string password, string host, string fingerprint,
            string remoteDirectory, string downloadDirectory, string tempDirectory, string targetDirectory, string logDirectory)
        {
            string remoteServer = host + remoteDirectory;
            bool tempDirExist = Helper.CheckDirectory(tempDirectory);
            bool downloadDirExist = Helper.CheckDirectory(downloadDirectory);
            bool targetDirExist = Helper.CheckDirectory(targetDirectory);
            bool logDirExist = Helper.CheckDirectory(logDirectory);
            bool isBackupSuccess = false, isDownloadSuccess = false, isFTPConnectSuccess = false, isExtractSuccess = false;

            int filesUpdated = 0;
            int filesDownloaded = 0;

            //encode special char for username & pass
            //username = Helper.EncodeSpecialChar(username);
            //password = Helper.EncodeSpecialChar(password);

            if (tempDirExist && downloadDirExist && targetDirExist && logDirExist)
            {
                Helper.WriteLog($"Connecting to FTP server..");
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
                        else
                        {
                            foreach (var file in files)
                            {
                                if (file.Contains("."))
                                {
                                    filteredFiles.Add(file);
                                }
                            }
                        }

                        #region header
                        Console.WriteLine($"\n#################################################################\n" +
                            $"                        DIGICSLITE UPDATER" +
                            $"\n#################################################################\n");
                        #endregion

                        Helper.WriteLog($"Program started, process will begin..");

                        Helper.ClearTempFolder(tempDirectory);

                        Helper.WriteLog($"{filteredFiles.Count} files available in FTP Server {remoteServer}");

                        foreach (string file in filteredFiles)
                        {
                            Helper.WriteLog($"{file}");
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
                                                    Helper.WriteLog($"Extracting file failure, retrying to redownload the files..");
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
                                            Helper.WriteLog($"Checking the existing total files and the total files at Remote Server..");

                                            Helper.WriteLog($"Existing file: {existingFiles.Count} <> File at FTP Server: {filteredFiles.Count}");

                                            Helper.WriteLog($"The existing total files were different, retrying the download process..");

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
                                                    Helper.WriteLog($"Extracting file failure, retrying to redownload the files..");
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
                                            Helper.WriteLog($"Extracting file failure, retrying to redownload the files..");
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
                                        Helper.WriteLog($"Extracting file failure, retrying to redownload the files..");
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
                        else
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
                                                            Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
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
                                                                    Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
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
                                                    FileInfo fileInfo = new FileInfo(downloadFilePath);

                                                    long remoteFileSize = Helper.GetRemoteFileSize(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);

                                                    if (fileInfo.Length < remoteFileSize)
                                                    {
                                                        Helper.WriteLog($"Checking the existing {file} file size and the {file} file at FTP Server..");

                                                        Helper.WriteLog($"Existing file: {fileInfo.Length} <> File at FTP Server: {remoteFileSize}");

                                                        Helper.WriteLog($"The existing {existingFilename} file size were different, retrying the download process..");

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
                                                                    Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
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
                                                isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                                if (isDownloadSuccess == true)
                                                {

                                                    ++filesDownloaded;
                                                    FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                    if (FileExtracted == 0)
                                                    {
                                                        Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
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
                                            isDownloadSuccess = Process.DownloadFile(protocol, username, password, host, fingerprint, remoteFilePath, downloadFilePath, file);
                                            if (isDownloadSuccess == true)
                                            {

                                                ++filesDownloaded;
                                                FileExtracted = Process.SuccessExtractFilesToTemp(downloadDirectory, tempDirectory);
                                                if (FileExtracted == 0)
                                                {
                                                    Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
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
                                                Helper.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
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
                                                Helper.WriteLog($"Extracting file failure, retrying to redownload the file {file}..");
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
                                            Helper.WriteLog($"Downloading file failure, retrying to redownload the file {file}..");
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
                            Helper.WriteLog("No files downloaded and updated");
                        }
                        else if (FileExtracted > 0 && FileProcessed > 0)
                        {
                            if (filesUpdated > 0)
                            {
                                Helper.WriteLog($"{filesDownloaded} files downloaded at {downloadDirectory}");
                                Helper.WriteLog($"{FileProcessed} files updated at {targetDirectory}");
                                Helper.WriteLog("Update process done");
                            }
                            else
                            {
                                Helper.WriteLog($"{filesDownloaded} files downloaded at {downloadDirectory}");
                                Helper.WriteLog($"{FileProcessed} files extracted at {targetDirectory}");
                                Helper.WriteLog("Download process done");
                            }
                        }

                        Helper.ClearTempFolder(tempDirectory);

                        Helper.WriteLog($"Process has finished, program will shut down..\n");
                        Console.WriteLine($"#################################################################\n");

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
