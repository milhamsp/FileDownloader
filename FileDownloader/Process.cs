using System;
using System.Collections.Generic;
//using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Archives.Zip;
using System.Net.Mail;
using System.Windows.Forms;
using System.Threading;
using WinSCP;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace FileDownloader
{
    internal class Process
    {
        #region before
        //public static bool DownloadFile(string url, string file, string remoteServer, string downloadFilePath)
        //{
        //    bool isOk = false;
        //    do
        //    {
        //        try
        //        {
        //            Helper.WriteLog($"Downloading {file} from FTP Server {remoteServer} and updating {downloadFilePath}..");

        //            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
        //            request.Method = WebRequestMethods.Ftp.DownloadFile;
        //            request.Proxy = null;

        //            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        //            //Console.WriteLine(lastModified.ToString() + " <> " + existingLastModified.ToString());

        //            using (Stream stream = response.GetResponseStream())

        //            using (Stream filestream = File.Create(downloadFilePath))
        //            {
        //                byte[] buffer = new byte[10240];
        //                int read;
        //                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        //                {
        //                    filestream.Write(buffer, 0, read);
        //                    Console.Write("\rDownloaded {0} bytes ", filestream.Position);
        //                }
        //            }

        //            Helper.WriteLog($"Download file {file} done");

        //            request = (FtpWebRequest)WebRequest.Create(url);
        //            request.Method = WebRequestMethods.Ftp.GetFileSize;
        //            request.Proxy = null;

        //            response = (FtpWebResponse)request.GetResponse();

        //            FileInfo fileInfo = new FileInfo(downloadFilePath);

        //            response.Close();

        //            Helper.WriteLog($"Checking the downloaded {file} file size and the {file} file at FTP Server..");

        //            Helper.WriteLog($"Downloaded file: {fileInfo.Length} <> File at FTP Server: {response.ContentLength}");

        //            if (fileInfo.Length != response.ContentLength)
        //            {
        //                Helper.WriteLog($"The downloaded {file} file size were different, retrying the download process..");
        //                return isOk;
        //            }
        //            else
        //            {
        //                Helper.WriteLog($"The downloaded {file} file size are the same");
        //                return !isOk;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Helper.WriteLog("DownloadFile: Error => "+e.Message);
        //            Helper.WriteLog("Retrying the download process.. ");
        //            return isOk;
        //        }
        //    }
        //    while (isOk);
        //}
        #endregion

        #region update 040523
        //update 040523: implement winscp
        public static bool DownloadFile(string protocol, string username, string password, string host, 
            string fingerprint, string remoteFilePath, string downloadFilePath, string remoteFilename)
        {
            bool isOk = false;
            long remoteFileSize;
            do
            {
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

                                TransferOptions transferOptions = new TransferOptions();
                                transferOptions.TransferMode = TransferMode.Automatic;
                                transferOptions.ResumeSupport.State = TransferResumeSupportState.On;
                                transferOptions.FileMask = $"{remoteFilePath}";

                                #region GetFiles method
                                //TransferOperationResult method
                                //TransferOperationResult operationResult;
                                //operationResult = session.GetFiles($"{file}", $"{downloadDirectory}", false, transferOptions);
                                //operationResult.Check();

                                //foreach (TransferEventArgs transfer in operationResult.Transfers)
                                //{
                                //    Console.WriteLine("Download of {0} succeeded in {1}", transfer.FileName, transfer.Destination);
                                //}
                                #endregion

                                #region SynchronizeDirectories method
                                //SynchronizationResult method
                                //SynchronizationResult synchronizationResult;
                                //synchronizationResult = session.SynchronizeDirectories(SynchronizationMode.Local, 
                                //    downloadDirectory, remoteDirectory, false, false, SynchronizationCriteria.Time, transferOptions);

                                //synchronizationResult.Check();
                                #endregion

                                using (Stream stream = session.GetFile($"{remoteFilePath}", transferOptions))

                                using (Stream filestream = File.Create(downloadFilePath))
                                {
                                    byte[] buffer = new byte[10240];
                                    int read;
                                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        filestream.Write(buffer, 0, read);
                                        Console.Write("\rDownloaded {0} bytes ", filestream.Position);
                                    }
                                }

                                Helper.WriteLog($"Download file {remoteFilename} done");

                                FileInfo fileInfo = new FileInfo(downloadFilePath);

                                Helper.WriteLog($"Checking the downloaded {remoteFilename} file size and the {remoteFilename} file at FTP Server..");

                                Helper.WriteLog($"Downloaded file: {fileInfo.Length} bytes <> File at FTP Server: {remoteFileSize} bytes");

                                if (fileInfo.Length != remoteFileSize)
                                {
                                    Helper.WriteLog($"The downloaded {remoteFilename} file size were different, retrying the download process..");
                                    session.Close();
                                    return isOk;
                                }
                                else
                                {
                                    Helper.WriteLog($"The downloaded {remoteFilename} file size are the same");
                                    session.Close();
                                    return !isOk;
                                }
                            }
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

                            TransferOptions transferOptions = new TransferOptions();
                            transferOptions.TransferMode = TransferMode.Automatic;
                            transferOptions.ResumeSupport.State = TransferResumeSupportState.On;
                            transferOptions.FileMask = $"{remoteFilePath}";

                            using (Stream stream = session.GetFile($"{remoteFilePath}", transferOptions))

                            using (Stream filestream = File.Create(downloadFilePath))
                            {
                                byte[] buffer = new byte[10240];
                                int read;
                                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    filestream.Write(buffer, 0, read);
                                    Console.Write("\rDownloaded {0} bytes from {1} bytes", filestream.Position, remoteFileSize);
                                }
                            }

                            Helper.WriteLog($"Download file {remoteFilename} done");

                            FileInfo fileInfo = new FileInfo(downloadFilePath);

                            Helper.WriteLog($"Checking the downloaded {remoteFilename} file size and the {remoteFilename} file at FTP Server..");

                            Helper.WriteLog($"Downloaded file: {fileInfo.Length} bytes <> File at FTP Server: {remoteFileSize} bytes");

                            if (fileInfo.Length != remoteFileSize)
                            {
                                Helper.WriteLog($"The downloaded {remoteFilename} file size were different, retrying the download process..");
                                session.Close();
                                return isOk;
                            }
                            else
                            {
                                Helper.WriteLog($"The downloaded {remoteFilename} file size are the same");
                                session.Close();
                            }
                        }
                    };
                    isOk = true;
                    return isOk;
                }
                catch (Exception e)
                {
                    Helper.WriteLog("DownloadFile: Error => "+e.Message);
                    Helper.WriteLog("Retrying the download process.. ");
                    return isOk;
                }
            }
            while (isOk == false);
        }

        #endregion

        public static bool ExtractCopyFilesToTarget(string tempDirectory, string targetDirectory)
        {
            bool isOk = false;
            string[] tempFiles = Directory.GetFiles(tempDirectory);
            try
            {
                foreach (var tempFile in tempFiles)
                {
                    string filename = tempFile.Replace(tempDirectory, "");
                    DateTime existingLastModified = File.GetLastWriteTime(tempFile);
                    DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+filename);

                    if (existingLastModified > targetLastModified)
                    {
                        if (filename.Contains(".zip"))
                        {
                            Console.WriteLine($"Opening file {filename}");
                            //using (FileStream zipFile = new FileStream(tempDirectory+filename, FileMode.Open))
                            //{
                            //    using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
                            //    {
                            //        foreach (ZipArchiveEntry fileEntry in archive.Entries)
                            //        {
                            //            DateTimeOffset fileLastModified = fileEntry.LastWriteTime;
                            //            DateTime convertedFileLastModified = fileLastModified.DateTime;
                            //            targetLastModified = File.GetLastWriteTime(targetDirectory+fileEntry.FullName);

                            //            if (convertedFileLastModified > targetLastModified)
                            //            {
                            //                //Console.WriteLine("\ndalam zip: " + fileEntry.FullName + " <> " +fileEntry.Name);
                            //                if (File.Exists(targetDirectory+fileEntry.FullName))
                            //                {
                            //                    //Console.WriteLine($"Replacing {fileEntry.FullName} in {targetDirectory}{fileEntry.FullName}");
                            //                    Helper.WriteLog($"Replacing {fileEntry.FullName} in {targetDirectory}{fileEntry.FullName}");
                            //                    File.Delete(targetDirectory+fileEntry.FullName);
                            //                    fileEntry.ExtractToFile(targetDirectory+fileEntry.FullName);
                            //                }
                            //                else
                            //                {
                            //                    //Console.WriteLine($"Extracting {fileEntry.FullName} in {targetDirectory}{fileEntry.FullName}");
                            //                    Helper.WriteLog($"Extracting {fileEntry.FullName} in {targetDirectory}{fileEntry.FullName}");
                            //                    fileEntry.ExtractToFile(targetDirectory+fileEntry.FullName);
                            //                }
                            //            }
                            //        }
                            //    }
                            //}

                            //update 180423: implement sharpcompress for zip extraction
                            using (var zip = ZipArchive.Open(filename))
                            {
                                foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                            Console.WriteLine($"\n#################################################################\n");
                            //Console.WriteLine($"Extracting {filename} to {targetDirectory}" +
                            //    $"\n\n#################################################################\n");
                            //ZipFile.ExtractToDirectory(tempDirectory+filename, targetDirectory);
                        }
                        else
                        {
                            Helper.WriteLog($"Copying {filename} to {targetDirectory}");
                            Console.WriteLine($"\n#################################################################\n");
                            File.Copy(tempDirectory+filename, targetDirectory+filename, true);
                        }
                    }
                }
                return !isOk;
            }
            catch (Exception e)
            {
                Helper.WriteLog("ExtractCopy: Error => " + e.Message);
                return isOk;
            }
        }

        public static bool ExtractCutFilesToTarget(string downloadDirectory, string tempDirectory, string targetDirectory)
        {
            bool isOk = false;
            string[] downloadedFiles = Directory.GetFiles(downloadDirectory);
            try
            {
                if(downloadedFiles.Length > 0) 
                {
                    //step 1 : extract file to tempDirectory
                    foreach (var downloadedFile in downloadedFiles)
                    {
                        string filename = downloadedFile.Replace(downloadDirectory, "");

                        if (filename.Contains(".zip"))
                        {
                            //Console.WriteLine($"Opening file {filename}");
                            Helper.WriteLog($"Opening file {filename}");
                            //using (FileStream zipFile = new FileStream(downloadDirectory+filename, FileMode.Open))
                            //{
                            //    using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
                            //    {
                            //        foreach (ZipArchiveEntry fileEntry in archive.Entries)
                            //        {
                            //            //Console.WriteLine($"Extracting {fileEntry.FullName} in {tempDirectory}{fileEntry.FullName}");
                            //            Helper.WriteLog($"Extracting {fileEntry.FullName} at {tempDirectory}{fileEntry.FullName}");
                            //            fileEntry.ExtractToFile(tempDirectory+fileEntry.FullName);
                            //        }
                            //    }
                            //}

                            //update 180423: implement sharpcompress for zip extraction
                            using (var zip = ZipArchive.Open(downloadedFile))
                            {
                                foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }

                            Console.WriteLine($"\n#################################################################\n");
                            //Console.WriteLine($"Extracting {filename} to {targetDirectory}" +
                            //    $"\n\n#################################################################\n");
                            //ZipFile.ExtractToDirectory(tempDirectory+filename, targetDirectory);
                        }
                        else if (filename.Contains(".rar"))
                        {
                            Helper.WriteLog($"Extracting {filename} to {tempDirectory}");
                            using (var archive = RarArchive.Open(downloadedFile))
                            {
                                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                        }
                        else
                        {
                            Helper.WriteLog($"Copying {filename} to {tempDirectory}");
                            Console.WriteLine($"\n#################################################################\n");
                            File.Copy(downloadDirectory+filename, tempDirectory+filename, true);
                        }
                    }
                }

                //step 2 : getting files and subdirectory in tempDirectory
                string[] tempDirectories = Directory.GetDirectories(tempDirectory);
                List<string> tempSubDirectories = new List<string>();

                if(tempDirectories.Length > 0)
                {
                    //step 3 : getting subdirectory names
                    foreach (var tempDir in tempDirectories)
                    {
                        string tempSubDir = tempDir.Replace(tempDirectory, "")+"/";
                        tempSubDirectories.Add(tempSubDir);
                    }

                    //step 4 : checking subdirectory in tempDirectory
                    foreach (var tempSubDir in tempSubDirectories)
                    {
                        string[] tempSubFiles = Directory.GetFiles(tempDirectory+tempSubDir);
                        foreach (var tempSubFile in tempSubFiles)
                        {
                            string filename = tempSubFile.Replace(tempDirectory+tempSubDir, "").Replace("\\", "");
                            DateTime tempLastModified = File.GetLastWriteTime(tempSubFile);
                            DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+tempSubDir+filename);

                            //step 5 : replacing outdated file in targetSubDirectory
                            if(targetLastModified.ToString() == "1/1/1601 7:00:00 AM")
                            {
                                Helper.WriteLog($"Copying file {filename} in {targetDirectory+tempSubDir+filename}");
                                File.Copy(tempSubFile, targetDirectory+tempSubDir+filename, true);
                                Helper.WriteLog($"Deleting file {filename} in {tempDirectory+tempSubDir+filename}");
                                File.Delete(tempDirectory+tempSubDir+filename);
                            }
                            else
                            {
                                if (tempLastModified > targetLastModified)
                                {
                                    Helper.WriteLog($"Replacing file {filename} in {targetDirectory+tempSubDir+filename}");
                                    File.Copy(tempSubFile, targetDirectory+tempSubDir+filename, true);
                                    Helper.WriteLog($"Deleting file {filename} in {tempDirectory+tempSubDir+filename}");
                                    File.Delete(tempDirectory+tempSubDir+filename);
                                }
                                else
                                {
                                    Helper.WriteLog($"{filename} file version is the latest, deleting the extracted {filename} in {tempDirectory+tempSubDir+filename}");
                                    File.Delete(tempDirectory+tempSubDir+filename);
                                }
                            }
                        }
                    }
                }

                string[] tempFiles = Directory.GetFiles(tempDirectory);
                //step 6 : checking files in tempDirectory
                if(tempFiles.Length > 0)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        string filename = tempFile.Replace(tempDirectory, "").Replace("\\", "");
                        DateTime tempLastModified = File.GetLastWriteTime(tempFile);
                        DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+filename);

                        //step 7 : replacing outdated files in targetDirectory
                        if (targetLastModified.ToString() == "1/1/1601 7:00:00 AM")
                        {
                            Helper.WriteLog($"Copying file {filename} in {targetDirectory+filename}");
                            File.Copy(tempFile, targetDirectory+filename, true);
                            Helper.WriteLog($"Deleting file {filename} in {tempDirectory+filename}");
                            File.Delete(tempDirectory+filename);
                        }
                        else
                        {
                            if (tempLastModified > targetLastModified)
                            {
                                Helper.WriteLog($"Replacing file {filename} in {targetDirectory+filename}");
                                File.Copy(tempFile, targetDirectory+filename, true);
                                Helper.WriteLog($"Deleting file {filename} in {tempDirectory+filename}");
                                File.Delete(tempDirectory+filename);
                            }
                            else
                            {
                                Helper.WriteLog($"{filename} file version is the latest, deleting the extracted {filename} in {tempDirectory+filename}");
                                File.Delete(tempDirectory+filename);
                            }
                        }
                            

                    }
                    Console.WriteLine($"\n#################################################################\n");
                }
                return !isOk;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                //Helper.WriteLog("ExtractCutFilesToTarget: Error => " + e.Message);
                Helper.WriteLog("ExtractFiles: Error => " + e.Message);
                return isOk;
            }
        }

        public static int SuccessExtractCutFilesToTarget(string downloadDirectory, string tempDirectory, string targetDirectory)
        {
            bool isOk = false;
            int isExtracted = 0;
            string[] downloadedFiles = Directory.GetFiles(downloadDirectory);
            try
            {
                if (downloadedFiles.Length > 0)
                {
                    //step 1 : extract file to tempDirectory
                    foreach (var downloadedFile in downloadedFiles)
                    {
                        string filename = downloadedFile.Replace(downloadDirectory, "");

                        if (filename.Contains(".zip"))
                        {
                            //Console.WriteLine($"Opening file {filename}");
                            Helper.WriteLog($"Extracting file {filename}");
                            //using (FileStream zipFile = new FileStream(downloadDirectory+filename, FileMode.Open))
                            //{
                            //    using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
                            //    {
                            //        foreach (ZipArchiveEntry fileEntry in archive.Entries)
                            //        {
                            //            //Console.WriteLine($"Extracting {fileEntry.FullName} in {tempDirectory}{fileEntry.FullName}");
                            //            Helper.WriteLog($"Extracting {fileEntry.FullName} at {tempDirectory}{fileEntry.FullName}");
                            //            fileEntry.ExtractToFile(tempDirectory+fileEntry.FullName);
                            //        }
                            //    }
                            //}

                            //update 180423: implement sharpcompress for zip extraction
                            using (var zip = ZipArchive.Open(downloadedFile))
                            {
                                foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                            //Console.WriteLine($"Extracting {filename} to {targetDirectory}" +
                            //    $"\n\n#################################################################\n");
                            //ZipFile.ExtractToDirectory(tempDirectory+filename, targetDirectory);
                        }
                        else if (filename.Contains(".rar"))
                        {
                            Helper.WriteLog($"Extracting {filename} to {tempDirectory}");
                            using (var archive = RarArchive.Open(downloadedFile))
                            {
                                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                        }
                        else
                        {
                            Helper.WriteLog($"Copying {filename} to {tempDirectory}");
                            Console.WriteLine($"\n#################################################################\n");
                            File.Copy(downloadDirectory+filename, tempDirectory+filename, true);
                        }
                    }
                }

                //step 2 : getting files and subdirectory in tempDirectory
                string[] tempDirectories = Directory.GetDirectories(tempDirectory);
                List<string> tempSubDirectories = new List<string>();

                if (tempDirectories.Length > 0)
                {
                    //step 3 : getting subdirectory names
                    foreach (var tempDir in tempDirectories)
                    {
                        string tempSubDir = tempDir.Replace(tempDirectory, "")+"/";
                        tempSubDirectories.Add(tempSubDir);
                    }

                    //step 4 : checking subdirectory in tempDirectory
                    foreach (var tempSubDir in tempSubDirectories)
                    {
                        string[] tempSubFiles = Directory.GetFiles(tempDirectory+tempSubDir);
                        foreach (var tempSubFile in tempSubFiles)
                        {
                            string filename = tempSubFile.Replace(tempDirectory+tempSubDir, "").Replace("\\", "");
                            DateTime tempLastModified = File.GetLastWriteTime(tempSubFile);
                            DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+tempSubDir+filename);

                            //step 5 : replacing outdated file in targetSubDirectory
                            if (targetLastModified.ToString() == "1/1/1601 7:00:00 AM")
                            {
                                Helper.WriteLog($"Copying file {filename} in {targetDirectory+tempSubDir+filename}");
                                File.Copy(tempSubFile, targetDirectory+tempSubDir+filename, true);
                                Helper.WriteLog($"Deleting file {filename} in {tempDirectory+tempSubDir+filename}");
                                File.Delete(tempDirectory+tempSubDir+filename);
                                ++isExtracted;
                            }
                            else
                            {
                                if (tempLastModified > targetLastModified)
                                {
                                    Helper.WriteLog($"Replacing file {filename} in {targetDirectory+tempSubDir+filename}");
                                    File.Copy(tempSubFile, targetDirectory+tempSubDir+filename, true);
                                    Helper.WriteLog($"Deleting file {filename} in {tempDirectory+tempSubDir+filename}");
                                    File.Delete(tempDirectory+tempSubDir+filename);
                                    ++isExtracted;
                                }
                                else
                                {
                                    Helper.WriteLog($"{filename} file version is the latest, deleting the extracted {filename} in {tempDirectory+tempSubDir+filename}");
                                    File.Delete(tempDirectory+tempSubDir+filename);
                                }
                            }
                        }
                    }
                }

                string[] tempFiles = Directory.GetFiles(tempDirectory);
                //step 6 : checking files in tempDirectory
                if (tempFiles.Length > 0)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        string filename = tempFile.Replace(tempDirectory, "").Replace("\\", "");
                        DateTime tempLastModified = File.GetLastWriteTime(tempFile);
                        DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+filename);

                        //step 7 : replacing outdated files in targetDirectory
                        if (targetLastModified.ToString() == "1/1/1601 7:00:00 AM")
                        {
                            Helper.WriteLog($"Copying file {filename} in {targetDirectory+filename}");
                            File.Copy(tempFile, targetDirectory+filename, true);
                            Helper.WriteLog($"Deleting file {filename} in {tempDirectory+filename}");
                            File.Delete(tempDirectory+filename);
                            ++isExtracted;
                        }
                        else
                        {
                            if (tempLastModified > targetLastModified)
                            {
                                Helper.WriteLog($"Replacing file {filename} in {targetDirectory+filename}");
                                File.Copy(tempFile, targetDirectory+filename, true);
                                Helper.WriteLog($"Deleting file {filename} in {tempDirectory+filename}");
                                File.Delete(tempDirectory+filename);
                                ++isExtracted;
                            }
                            else
                            {
                                Helper.WriteLog($"{filename} file version is the latest, deleting the extracted {filename} in {tempDirectory+filename}");
                                File.Delete(tempDirectory+filename);
                            }
                        }


                    }
                    Console.WriteLine($"\n#################################################################\n");
                }
                return isExtracted;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                //Helper.WriteLog("ExtractCutFilesToTarget: Error => " + e.Message);
                Helper.WriteLog("ExtractFiles: Error => " + e.Message);
                return isExtracted;
            }
        }

        #region update 270423
        //update 270423: separate extract function and cut files function due to issue if the file is opened while updater running
        public static int SuccessExtractFilesToTemp(string downloadDirectory, string tempDirectory)
        {
            bool isOk = false;
            int isExtracted = 0;
            
            try
            {
                string[] downloadedFiles = Directory.GetFiles(downloadDirectory);
                if (downloadedFiles.Length > 0)
                {
                    //step 1 : extract file to tempDirectory
                    foreach (var downloadedFile in downloadedFiles)
                    {
                        string filename = downloadedFile.Replace(downloadDirectory, "");

                        if (filename.Contains(".zip"))
                        {
                            //Console.WriteLine($"Opening file {filename}");
                            Helper.WriteLog($"Extracting file {filename}");

                            //update 180423: implement sharpcompress for zip extraction
                            using (var zip = ZipArchive.Open(downloadedFile))
                            {
                                foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                            ++isExtracted;
                            //Console.WriteLine($"Extracting {filename} to {targetDirectory}" +
                            //    $"\n\n#################################################################\n");
                            //ZipFile.ExtractToDirectory(tempDirectory+filename, targetDirectory);
                        }
                        else if (filename.Contains(".rar"))
                        {
                            Helper.WriteLog($"Extracting {filename} to {tempDirectory}");
                            using (var archive = RarArchive.Open(downloadedFile))
                            {
                                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                {
                                    entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                            ++isExtracted;
                        }
                    }
                }
                return isExtracted;
            }
            catch (Exception e)
            {
                Helper.WriteLog("ExtractFiles: Error => " + e.Message);
                isExtracted = 0;
                return isExtracted;
            }
        }

        public static int SuccessCutFilesToTarget(string tempDirectory, string targetDirectory)
        {
            bool isOk = false;
            int isUpdated = 0;

            //step 2 : getting files and subdirectory in tempDirectory
            string[] tempDirectories = Directory.GetDirectories(tempDirectory);
            List<string> tempSubDirectories = new List<string>();

            if (tempDirectories != null)
            {
                if (tempDirectories.Length > 0)
                {
                    //step 3 : getting subdirectory names
                    foreach (var tempDir in tempDirectories)
                    {
                        string tempSubDir = tempDir.Replace(tempDirectory, "")+"/";
                        tempSubDirectories.Add(tempSubDir);
                    }

                    //step 4 : checking subdirectory in tempDirectory
                    foreach (var tempSubDir in tempSubDirectories)
                    {
                        string[] tempSubFiles = Directory.GetFiles(tempDirectory+tempSubDir);
                        foreach (var tempSubFile in tempSubFiles)
                        {
                            string filename = tempSubFile.Replace(tempDirectory+tempSubDir, "").Replace("\\", "");
                            DateTime tempLastModified = File.GetLastWriteTime(tempSubFile);
                            DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+tempSubDir+filename);

                            //step 5 : replacing outdated file in targetSubDirectory
                            if (targetLastModified.ToString() == "1/1/1601 7:00:00 AM")
                            {
                                try
                                {
                                    Helper.WriteLog($"Copying file {filename} in {targetDirectory+tempSubDir+filename}");
                                    File.Copy(tempSubFile, targetDirectory+tempSubDir+filename, true);
                                    Helper.WriteLog($"Deleting file {filename} in {tempDirectory+tempSubDir+filename}");
                                    File.Delete(tempDirectory+tempSubDir+filename);
                                    ++isUpdated;
                                }
                                catch (Exception e)
                                {
                                    Helper.WriteLog("CopyFiles: Error => " + e.Message);
                                }
                            }
                            else
                            {
                                if (tempLastModified > targetLastModified)
                                {
                                    do
                                    {
                                        try
                                        {
                                            Helper.WriteLog($"Replacing file {filename} in {targetDirectory+tempSubDir+filename}");
                                            File.Copy(tempSubFile, targetDirectory+tempSubDir+filename, true);
                                            Helper.WriteLog($"Deleting file {filename} in {tempDirectory+tempSubDir+filename}");
                                            File.Delete(tempDirectory+tempSubDir+filename);
                                            ++isUpdated;
                                            isOk = true;
                                        }
                                        catch (Exception e)
                                        {
                                            Helper.WriteLog("ReplaceFiles: Error => " + e.Message);
                                            DialogResult ans = MessageBox.Show("Please close the DigiCSLite app and its related files to continue the update process!",
                                                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            if (ans == DialogResult.OK)
                                            {
                                                isOk = false;
                                                //Helper.WriteLog($"Process aborted, program will shut down..\n");
                                                //Environment.Exit(0);
                                            }
                                            else
                                            {
                                                isOk = false;
                                                //Helper.WriteLog($"Process aborted, program will shut down..\n");
                                                //Environment.Exit(0);
                                            }
                                        }
                                    }
                                    while (isOk == false);
                                }
                                else
                                {
                                    Helper.WriteLog($"{filename} file version is the latest, deleting the extracted {filename} in {tempDirectory+tempSubDir+filename}");
                                    File.Delete(tempDirectory+tempSubDir+filename);
                                }
                            }
                        }
                    }
                }
            }

            string[] tempFiles = Directory.GetFiles(tempDirectory);
            //step 6 : checking files in tempDirectory
            if (tempFiles != null)
            {
                if (tempFiles.Length > 0)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        string filename = tempFile.Replace(tempDirectory, "").Replace("\\", "");
                        DateTime tempLastModified = File.GetLastWriteTime(tempFile);
                        DateTime targetLastModified = File.GetLastWriteTime(targetDirectory+filename);

                        //step 7 : replacing outdated files in targetDirectory
                        if (targetLastModified.ToString() == "1/1/1601 7:00:00 AM")
                        {
                            try
                            {
                                Helper.WriteLog($"Copying file {filename} in {targetDirectory+filename}");
                                File.Copy(tempFile, targetDirectory+filename, true);
                                Helper.WriteLog($"Deleting file {filename} in {tempDirectory+filename}");
                                File.Delete(tempDirectory+filename);
                                ++isUpdated;
                            }
                            catch (Exception e)
                            {
                                Helper.WriteLog("CopyFiles: Error => " + e.Message);
                            }
                        }
                        else
                        {
                            if (tempLastModified > targetLastModified)
                            {
                                do
                                {
                                    try
                                    {
                                        Helper.WriteLog($"Replacing file {filename} in {targetDirectory+filename}");
                                        File.Copy(tempFile, targetDirectory+filename, true);
                                        Helper.WriteLog($"Deleting file {filename} in {tempDirectory+filename}");
                                        File.Delete(tempDirectory+filename);
                                        ++isUpdated;
                                        isOk = true;
                                    }
                                    catch (Exception e)
                                    {
                                        Helper.WriteLog("ReplaceFiles: Error => " + e.Message);
                                        DialogResult ans = MessageBox.Show("Please close the DigiCSLite app and its related files to continue the update process!",
                                                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        if (ans == DialogResult.OK)
                                        {
                                            isOk = false;
                                            //Helper.WriteLog($"Process aborted, program will shut down..");
                                            //Environment.Exit(0);
                                        }
                                        else
                                        {
                                            isOk = false;
                                            //Helper.WriteLog($"Process aborted, program will shut down..");
                                            //Environment.Exit(0);
                                        }
                                    }
                                }
                                while (isOk == false);
                            }
                            else
                            {
                                Helper.WriteLog($"{filename} file version is the latest, deleting the extracted {filename} in {tempDirectory+filename}");
                                File.Delete(tempDirectory+filename);
                            }
                        }


                    }
                    Console.WriteLine($"\n#################################################################\n");
                }
            }
            return isUpdated;
        }

        #endregion

        #region update 080523
        //update 080523: adding handler for multipart rar archive
        public static int SuccessExtractMultipartFilesToTemp(List<string> files, string downloadDirectory, string tempDirectory)
        {
            int isExtracted = 0;
            string filename;

            try
            {
                filename = Regex.Replace(files.First(), @"\d", "*");
                Helper.WriteLog($"Extracting {filename} and other parts to {tempDirectory}");
                using (var archive = RarArchive.Open(files.ToArray().Select(s => Path.Combine(downloadDirectory, s)).Select(File.OpenRead)))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
                ++isExtracted;
                return isExtracted;
            }
            catch(Exception e)
            {
                Helper.WriteLog("ExtractFiles: Error => " + e.Message);
                isExtracted = 0;
                return isExtracted;
            }
        }

        #endregion

        public static bool BackupFiles(string existingFile, string downloadDirectory)
        {
            bool isOk = false;
            string[] existingFiles = Directory.GetFiles(downloadDirectory);
            string existingFilename = existingFile.Replace(downloadDirectory, "");
            try
            {
                //foreach (string targetFile in targetFiles)
                //{
                //    string filename = targetFile.Replace(targetDirectory, "");
                //    File.Copy(targetFile, backupDirectory+filename, true);
                //}
                if (File.Exists(existingFile))
                {
                    Helper.WriteLog("Backing up the last version package process started..");
                    string subDirectoryName;
                    string currDate = DateTime.Now.ToString("ddMM");
                    string[] otherDirectories = Directory.GetDirectories(downloadDirectory);
                    if (otherDirectories.Length > 0)
                    {
                        foreach (var otherDirectory in otherDirectories)
                        {
                            subDirectoryName = otherDirectory.Replace(downloadDirectory, "");
                            Helper.WriteLog($"Deleting directory and files in {otherDirectory}");
                            //Console.WriteLine($"Deleting directory and files in {downloadDirectory+subDirectoryName}");
                            //delete existing backup directory
                            Directory.Delete(otherDirectory, true);
                        }
                    }
                    //Console.WriteLine($"Backing up file to {downloadDirectory+currDate}/{existingFilename}");
                    Helper.WriteLog($"Backing up file to {downloadDirectory+currDate}/{existingFilename}");
                    //create backup directory
                    Directory.CreateDirectory(downloadDirectory+currDate);
                    //copy backup directory
                    File.Copy(existingFile, downloadDirectory+currDate+"/"+existingFilename, true);
                }
                return !isOk;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                Helper.WriteLog("BackupFiles: Error => "+e.Message);
                return isOk;
            }
        }

        public static bool CopyFiles(string tempDirectory, string targetDirectory)
        {
            bool isOk = false;
            string[] tempFiles = Directory.GetFiles(tempDirectory);
            try
            {
                foreach (var tempFile in tempFiles)
                {
                    string filename = tempFile.Replace(tempDirectory, "");
                    Console.WriteLine($"Copying {filename} to {targetDirectory}" +
                        $"\n\n#################################################################\n");
                    File.Copy(tempDirectory+filename, targetDirectory+filename, true);
                }
                return !isOk;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return isOk;
            }
        }

        public static bool ExtractFiles(string tempDirectory, string targetDirectory)
        {
            bool isOk = false;
            string[] tempFiles = Directory.GetFiles(tempDirectory);
            try
            {
                foreach (var tempFile in tempFiles)
                {
                    string filename = tempFile.Replace(tempDirectory, "");
                    //using (FileStream zipFile = new FileStream(tempDirectory+filename, FileMode.Open))
                    //{
                    //    using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
                    //    {
                    //        foreach (ZipArchiveEntry fileEntry in archive.Entries)
                    //        {
                    //            //Console.WriteLine("\ndalam zip: " + fileEntry.FullName + " <> " +fileEntry.Name);
                    //            if (File.Exists(targetDirectory+fileEntry.FullName))
                    //            {
                    //                File.Delete(targetDirectory+fileEntry.FullName);
                    //            }
                    //        }
                    //    }
                    //}
                    //System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);

                    Console.WriteLine($"Extracting {filename}" +
                        $"\n\n#################################################################\n");
                    //ZipFile.ExtractToDirectory(tempDirectory+filename, targetDirectory);

                    //update 180423: implement sharpcompress for zip extraction
                    using (var zip = ZipArchive.Open(filename))
                    {
                        foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                        {
                            entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }
                return !isOk;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return isOk;
            }
        }

        public static bool SendNotifEmail(string senderEmail, string receiverEmail, string SmtpHost, string SmtpPort)
        {
            bool isOk = false;
            SmtpClient client = new SmtpClient();
            MailMessage mail = new MailMessage();
            try
            {
                mail = new MailMessage();

            }
            catch (Exception e)
            {

            }
            return !isOk;
        }
    }
}
