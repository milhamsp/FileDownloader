using DigiCSLiteUpdater.Config;
using FileDownloader.Config;
using SharpConfig;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinSCP;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Web.Script.Serialization;
using POSMainForm.models;
using System.Runtime.Remoting.Contexts;
using System.Net.Sockets;
using DigiCSLiteUpdater;
using System.Security.Cryptography;
using DigiCSLiteUpdater.Model;

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
                FtpConfig.Fingerprint = section["Fingerprint"].StringValue.Trim();

                section = config["DirectoryConfig"];
                DirectoryConfig.RemoteDirectory = section["RemoteDirectory"].StringValue.Trim();
                DirectoryConfig.DownloadDirectory = section["DownloadDirectory"].StringValue.Trim();
                DirectoryConfig.TempDirectory = section["TempDirectory"].StringValue.Trim();
                DirectoryConfig.TargetDirectory = section["TargetDirectory"].StringValue.Trim();
                DirectoryConfig.LogDirectory = section["LogDirectory"].StringValue.Trim();
                DirectoryConfig.AppExeDirectory = section["AppExeDirectory"].StringValue.Trim();

                section = config["ConnectionConfig"];
                ConnectionConfig.ConnectionUrl = section["ConnectionUrl"].StringValue.Trim();
                ConnectionConfig.DownloadTimeoutMinutes = int.Parse(section["DownloadTimeoutMinutes"].StringValue.Trim());
                //ConnectionConfig.PathGetExecTime = section["PathGetExecTime"].StringValue.Trim();

                section = config["ClientConfig"];
                ClientConfig.Mode = section["Mode"].StringValue.Trim();
                ClientConfig.IpAddress = Util.GetLocalIpAddress();
                ClientConfig.Branch = section["Branch"].StringValue.Trim();
                ClientConfig.Terminal = section["Terminal"].StringValue.Trim();
                ClientConfig.Outlet = section["Outlet"].StringValue.Trim();
                ClientConfig.AppVersion = Helper.GetClientVersion(DirectoryConfig.AppExeDirectory);

                bool DoneGetEncKey = GetEncKey();
                if (!DoneGetEncKey)
                {
                    isOk = false;
                    return isOk;
                }

                bool DoneAuth = PostGetJwtToken();
                if (!DoneAuth)
                {
                    isOk = false;
                    return isOk;
                }

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

                            if (remoteDirectoryInfo != null) 
                                foreach (RemoteFileInfo remoteFileInfo in remoteDirectoryInfo.Files)
                                {
                                    //update 311224 : filter file . and ..
                                    if (remoteFileInfo.Name != "." && remoteFileInfo.Name != "..")
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

                        if (remoteDirectoryInfo != null)
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
                Util.WriteLog("CheckFTPFiles: Error => " + e.Message);
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
                        Util.WriteLog("Fingerprint in the Config.ini is empty..");
                        DialogResult ans = MessageBox.Show("Fingerprint is empty! Please fill the Config.ini file correctly!",
                                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (ans == DialogResult.OK)
                        {
                            Util.WriteLog($"Process aborted, program will shut down..");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                        else
                        {
                            Util.WriteLog($"Process aborted, program will shut down..");
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
                Util.WriteLog("GetRemoteFileDate: Error => "+e.Message);
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
                        Util.WriteLog("Fingerprint in the Config.ini is empty..");
                        DialogResult ans = MessageBox.Show("Fingerprint is empty! Please fill the Config.ini file correctly!",
                                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (ans == DialogResult.OK)
                        {
                            Util.WriteLog($"Process aborted, program will shut down..");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                        else
                        {
                            Util.WriteLog($"Process aborted, program will shut down..");
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
                Util.WriteLog("GetRemoteFileSize: Error => "+e.Message);
                return remoteFileSize;
            }
        }
        #endregion

        #region update 081225 : add utils from digicslite
        public static string MaskedBaseUrl(string url)
        {
            string maskedUrl = url;

            try
            {
                Uri uri = new Uri(url);
                string host = uri.Host;
                string[] ipParts = host.Split('.');

                ipParts[0] = "***";
                ipParts[1] = "***";

                var maskedIp = string.Join(".", ipParts);
                maskedUrl = url.Replace(host, maskedIp);

                return maskedUrl;
            }
            catch (Exception ex)
            {
                return maskedUrl;
            }
        }

        public static string EncryptData(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text) && text.Length > 4)
                {
                    text.Trim();
                    string firstSegment = text.Substring(0, 4);
                    int textLength = text.Length;
                    string trimmedText = text.Substring(textLength - (textLength - 4), textLength - 4);

                    using (TripleDES tripleDes = TripleDES.Create())
                    {
                        tripleDes.Key = Encoding.UTF8.GetBytes(AppData.Key);
                        tripleDes.IV = Encoding.UTF8.GetBytes(AppData.Iv);
                        tripleDes.Mode = CipherMode.CBC;
                        tripleDes.Padding = PaddingMode.PKCS7;

                        using (ICryptoTransform encryptor = tripleDes.CreateEncryptor())
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    byte[] toEncrypt = Encoding.UTF8.GetBytes(trimmedText);
                                    cryptoStream.Write(toEncrypt, 0, toEncrypt.Length);
                                }
                                return string.Concat(firstSegment, Convert.ToBase64String(memoryStream.ToArray()));
                            }
                        }
                    }
                }
                else
                {
                    return text;
                }
            }
            catch (Exception ex)
            {
                Util.WriteLog($"EncryptData: Error => Encrypt String {text}, {ex.Message}");
                return text;
            }
        }

        public static string DecryptData(string encryptedText)
        {
            try
            {
                if (!string.IsNullOrEmpty(encryptedText) && encryptedText.Length > 4)
                {
                    string firstSegment = encryptedText.Substring(0, 4);
                    string encryptedSegment = encryptedText.Substring(4);

                    byte[] encryptedBytes = Convert.FromBase64String(encryptedSegment);

                    using (TripleDES tripleDes = TripleDES.Create())
                    {
                        tripleDes.Key = Encoding.UTF8.GetBytes(AppData.Key);
                        tripleDes.IV = Encoding.UTF8.GetBytes(AppData.Iv);
                        tripleDes.Mode = CipherMode.CBC;
                        tripleDes.Padding = PaddingMode.PKCS7;

                        using (ICryptoTransform decryptor = tripleDes.CreateDecryptor())
                        {
                            using (MemoryStream memoryStream = new MemoryStream(encryptedBytes))
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                                    {
                                        string decrypted = streamReader.ReadToEnd();
                                        return string.Concat(firstSegment, decrypted);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    return encryptedText;
                }
            }
            catch (Exception ex)
            {
                Util.WriteLog($"DecryptData: Error => Decrypt String {encryptedText}, {ex.Message}");
                return encryptedText;
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
        //        Util.WriteLog("GetFTPFilesVersion: Error => "+e.Message);
        //        return lastModified;
        //    }
        //}
        #endregion

        #region http endpoint update
        public static (bool, RspCheckUpdate) CheckClientUpdates(string url, string strReq)
        {
            RspCheckUpdate rsp = new RspCheckUpdate();
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    double timeoutSetup = ConnectionConfig.DownloadTimeoutMinutes > 0
                        ? ConnectionConfig.DownloadTimeoutMinutes : 30; //default 30 minutes timeout downloading
                    httpClient.Timeout = TimeSpan.FromMinutes(timeoutSetup);

                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    //add auth
                    if (!string.IsNullOrEmpty(AppData.Token))
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", AppData.Token);

                    request.Content = new StringContent(
                            strReq,
                            Encoding.UTF8,
                            "application/json"
                        );

                    using (var response = httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead
                    ).GetAwaiter().GetResult())
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            bool retry = (int)response.StatusCode >= 500;

                            rsp.Success = false;
                            rsp.Message = retry ? "Internal server error" : "Client error";

                            return (retry, rsp); //retry if 500 error
                        }

                        //check content type
                        var contentType = response.Content.Headers.ContentType?.MediaType;

                        if (!string.IsNullOrEmpty(contentType) &&
                            contentType.Contains("application/json"))
                        {
                            string strRsp = response.Content
                                       .ReadAsStringAsync()
                                       .GetAwaiter()
                                       .GetResult();

                            if (!string.IsNullOrEmpty(strRsp))
                            {
                                Util.WriteLog($"Rsp Check Update : " + strRsp);

                                JavaScriptSerializer js = new JavaScriptSerializer();
                                var rspJson = js.Deserialize<RspSingle<object>>(strRsp);

                                if (rspJson != null && rspJson.code.Equals("00"))
                                {
                                    rsp.Success = false;
                                    rsp.Message = rspJson.message;
                                    rsp.HasUpdate = false;

                                    return (true, rsp); //no update
                                }
                            }
                            else
                            {
                                rsp.Success = false;
                                rsp.Message = "Invalid response format";

                                return (false, rsp); //retry due to corrupted response
                            }
                        }

                        //file response
                        rsp.HasUpdate = true;

                        var headerMetadata = Process.ValidateFileHeaders(response);
                        if (!headerMetadata.ok)
                        {
                            rsp.Message = headerMetadata.error;
                            rsp.Success = headerMetadata.ok;

                            return (true, rsp); //no retry
                        }

                        if (response.Content.Headers.ContentDisposition != null)
                        {
                            string fileName = response.Content.Headers.ContentDisposition.FileNameStar 
                                ?? response.Content.Headers.ContentDisposition.FileName;

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var rspFile = Process.ResponseDownloadFile(response, fileName,
                                    headerMetadata.size, headerMetadata.hash);

                                rsp.Success = rspFile.success;
                                rsp.Message = rspFile.message;

                                return (rspFile.success, rsp);
                                
                            }
                        }

                        rsp.Success = false;
                        rsp.Message = "Filename empty";

                        return (false, rsp);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                rsp.Success = false;
                rsp.Message = "CheckClientUpdates: Error => Timeout reached";
                return (false, rsp);
            }
            catch (HttpRequestException)
            {
                rsp.Success = false;
                rsp.Message = "CheckClientUpdates: Error => Network error";
                return (false, rsp);
            }
            catch (IOException ex)
            {
                rsp.Success = false;
                rsp.Message = $"CheckClientUpdates: Error => {ex.Message}";
                return (true, rsp); //no retry
            }
            catch (Exception ex)
            {
                rsp.Success = false;
                rsp.Message = $"CheckClientUpdates: Error => {ex.Message}";
                return (true, rsp); //no retry
            }
        }

        public static string SendToMiddleWare(string url, HttpMethod method, string strReq = "")
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(method, url);

                    request.Headers.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    //add auth
                    if (!string.IsNullOrEmpty(AppData.Token))
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", AppData.Token);

                    if (!string.IsNullOrEmpty(strReq) && method != HttpMethod.Get)
                    {
                        request.Content = new StringContent(
                            strReq,
                            Encoding.UTF8,
                            "application/json"
                        );
                    }

                    using (var response = httpClient.SendAsync(request)
                                                    .GetAwaiter()
                                                    .GetResult())
                    {
                        if (!response.IsSuccessStatusCode)
                            return null;

                        return response.Content
                                       .ReadAsStringAsync()
                                       .GetAwaiter()
                                       .GetResult();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static string GetClientVersion(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return "0.0.0.0";

            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
            return info.FileVersion ?? "0.0.0.0";
        }

        public static string ComputeSha256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash)
                    .Replace("-", "")
                    .ToLowerInvariant();
            }
        }

        public static bool IsAddressAvailable(string address)
        {
            try
            {
                System.Net.WebClient client = new WebClient();
                byte[] bytes = client.DownloadData(address);
                if (bytes.Length > 0)
                {
                    string result = System.Text.Encoding.UTF8.GetString(bytes);
                    // FlexibleMessageBox.Show(result);

                    Util.WriteLog($"Connected to Remote : {address}");
                    return true;
                }

                Util.WriteLog($"Connection failed to Remote : {address}");
                return false;
            }
            catch
            {
                Util.WriteLog($"Connection failed to Remote : {address}");
                return false;
            }
            return false;
        }
        #endregion

        #region REQUEST
        public static string GenReqGetToken()
        {

            string strReq = "";
            ReqHeaderAuth req = null;
            DateTime dateTime = DateTime.UtcNow.Date;

            try
            {
                req = new ReqHeaderAuth();

                //req.branch = LandingPage.reqHeader.branch;
                //req.outlet = LandingPage.reqHeader.outlet;
                //req.terminal = LandingPage.reqHeader.terminal;
                //req.ipAddress = LandingPage.reqConfig.ipAddress;

                //req.mstid = DataNasabah.vMstid;

                //generate random 6 digit w/ every 1 changed to 9
                Random rnd = new Random();
                int number = rnd.Next(100000, 1000000);

                string result = number.ToString().Replace('1', '9');

                //generate 2 minute expiry time
                long nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long expMillis = nowMillis + (200 * 60 * 1000);

                string expiryStr = expMillis.ToString();

                string key = $"{result}" +
                    $"-{ClientConfig.Branch}" +
                    $"-{ClientConfig.Outlet}" +
                    $"-{ClientConfig.IpAddress}" +
                    $"-{expiryStr}" +
                    $"-{ClientConfig.Terminal}";

                //encrypt key
                key = EncryptData(key);

                //encode to base64
                byte[] bytes = Encoding.UTF8.GetBytes(key);

                string b64key = Convert.ToBase64String(bytes);

                req.key = b64key;

                strReq = new JavaScriptSerializer().Serialize(req);

            }
            catch (Exception ex)
            {
                Util.WriteLog("GenReqGetToken: Error => " + ex.Message);
            }
            return strReq;

        }
        
        public static string GenReqCheckUpdate()
        {

            string strReq = "";
            ReqCheckUpdate req = null;
            DateTime dateTime = DateTime.UtcNow.Date;

            try
            {
                req = new ReqCheckUpdate();

                req.ipAddress = ClientConfig.IpAddress;
                req.lastVersion = ClientConfig.AppVersion;

                strReq = new JavaScriptSerializer().Serialize(req);

            }
            catch (Exception ex)
            {
                Util.WriteLog("GenReqCheckUpdate: Error => " + ex.Message);
            }
            return strReq;

        }
        
        public static string GenReqUpdateStatus(int step)
        {

            string strReq = "";
            ReqUpdateStatus req = null;
            DateTime dateTime = DateTime.UtcNow.Date;

            try
            {
                req = new ReqUpdateStatus();

                req.ipAddress = ClientConfig.IpAddress;
                if (step == 3)
                    req.lastVersion = GetClientVersion(DirectoryConfig.AppExeDirectory);
                else
                    req.lastVersion = ClientConfig.AppVersion;
                req.step = step;

                strReq = new JavaScriptSerializer().Serialize(req);

            }
            catch (Exception ex)
            {
                Util.WriteLog("GenReqUpdateStatus: Error => " + ex.Message);
            }
            return strReq;

        }
        #endregion

        #region HIT TO ENDPOINT
        private static bool GetEncKey()
        {
            try
            {
                Util.WriteLog("Req Get Enc Key : " + Helper.MaskedBaseUrl(ConnectionConfig.ConnectionUrl) + ConnectionConfig.PathGetEncKey);
                var _ = SendToMiddleWare(ConnectionConfig.ConnectionUrl + ConnectionConfig.PathGetEncKey, HttpMethod.Get);

                JavaScriptSerializer js = new JavaScriptSerializer();
                js = new JavaScriptSerializer();
                var _rspEncKey = js.Deserialize<RspAll<DropdownPropVM>>(_);

                if (_rspEncKey != null && _rspEncKey.code != null && _rspEncKey.code.Equals("00"))
                {
                    for (int i = 0; i < _rspEncKey.data.Count; i++)
                    {
                        string val = _rspEncKey.data[i].name.Trim();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var split = val.Split('-');

                            AppData.Key = split[0];
                            AppData.Iv = split[1];
                        }
                    }

                    string _log = EncryptData(_);
                    if (_log != null)
                    {
                        var _rspEncKeyLog = js.Deserialize<RspAll<DropdownPropVM>>(_);
                        if (_rspEncKeyLog.code.Equals("00"))
                        {
                            for (int i = 0; i < _rspEncKeyLog.data.Count; i++)
                            {
                                string val = _rspEncKeyLog.data[i].name.Trim();
                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    _rspEncKeyLog.data[i].name = Helper.EncryptData(val);
                                }
                            }
                        }

                        _log = js.Serialize(_rspEncKeyLog);
                        Util.WriteLog("Resp Get Enc Key : " + _log);
                    }

                    return true;
                }
                else
                {
                   Util.WriteLog("Resp Get Enc Key : " + _);
                }


            }
            catch (Exception ex)
            {
                Util.WriteLog("GetEncKey: Error => " + ex.Message);
                return false;
            }

            return false;
        }

        private static bool PostGetJwtToken()
        {
            try
            {
                string strReq = GenReqGetToken();

                Util.WriteLog($"Req Get Token : " + Helper.MaskedBaseUrl(ConnectionConfig.ConnectionUrl) + ConnectionConfig.PathAuth 
                    + " =>\n" + strReq);
                var _ = SendToMiddleWare(ConnectionConfig.ConnectionUrl + ConnectionConfig.PathAuth, HttpMethod.Post, strReq);

                if (string.IsNullOrEmpty(_))
                    return false;

                Util.WriteLog($"Res Get Token : " + _);

                JavaScriptSerializer js = new JavaScriptSerializer();
                var rsp = js.Deserialize<RspSingle<FmtAuthToken>>(_);

                if (rsp.code != null && rsp.code.Equals("00"))
                {
                    AppData.Token = rsp.data.token;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
               Util.WriteLog("PostGetJwtToken: Error => " + ex.Message);
            }

            return true;
        }

        public static bool GetExecDatetime()
        {
            try
            {
               Util.WriteLog("Req Get Exec Datetime : " + MaskedBaseUrl(ConnectionConfig.ConnectionUrl) + ConnectionConfig.PathGetExecTime);
                var _ = SendToMiddleWare(ConnectionConfig.ConnectionUrl + ConnectionConfig.PathGetExecTime, HttpMethod.Get);

                JavaScriptSerializer js = new JavaScriptSerializer();
                js = new JavaScriptSerializer();
                var _rspExecTime = js.Deserialize<RspAll<DropdownPropVM>>(_);

                if (_rspExecTime != null && _rspExecTime.code != null && _rspExecTime.code.Equals("00"))
                {
                    for (int i = 0; i < _rspExecTime.data.Count; i++)
                    {
                        string val = _rspExecTime.data[i].name.Trim();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            AppData.ExecTime = val;
                        }
                    }
                    
                   Util.WriteLog("Resp Get Exec Time : " + _);
                    return true;

                }
                else
                {
                   Util.WriteLog("Resp Get Exec Time : " + _); 
                    return false;
                }


            }
            catch (Exception ex)
            {
               Util.WriteLog("GetExecDatetime: Error => " + ex.Message);
                return false;
            }

        }

        public static (bool, RspCheckUpdate) PostCheckUpdate()
        {
            bool status;
            RspCheckUpdate rsp = new RspCheckUpdate();

            try
            {
                //step 2.1: generate req
                string strReq = GenReqCheckUpdate();

                Util.WriteLog($"Req Check Update : " + Helper.MaskedBaseUrl(ConnectionConfig.ConnectionUrl) + ConnectionConfig.PathCheckVersion
                    + " =>\n" + strReq);

                //step 2.2: loop hit to endpoint while status equals false
                do
                {
                    (status, rsp) =
                        CheckClientUpdates(
                            ConnectionConfig.ConnectionUrl + ConnectionConfig.PathCheckVersion,
                            strReq);

                    if (!status)
                        Thread.Sleep(5000); //delay for 5 sec

                }
                while (!status);

                return (status, rsp);
            }
            catch (Exception ex)
            {
                Util.WriteLog("PostCheckUpdate: Error => " + ex.Message);
            }

            return (false, rsp);
        }
        
        public static bool PostUpdateStatus(int step)
        {
            try
            {
                //step 3.1: generate req
                string strReq = GenReqUpdateStatus(step);

                Util.WriteLog($"Req Update Status : " + Helper.MaskedBaseUrl(ConnectionConfig.ConnectionUrl) + ConnectionConfig.PathUpdateStep
                    + " =>\n" + strReq);

                var _ = SendToMiddleWare(ConnectionConfig.ConnectionUrl + ConnectionConfig.PathUpdateStep, HttpMethod.Post, strReq);

                if (string.IsNullOrEmpty(_))
                    return false;

                Util.WriteLog($"Res Update Status : " + _);

                JavaScriptSerializer js = new JavaScriptSerializer();
                var rsp = js.Deserialize<RspSingle<FmtAuthToken>>(_);

                if (rsp.code != null && rsp.code.Equals("00"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Util.WriteLog("PostUpdateStatus: Error => " + ex.Message);
            }

            return true;
        }

        #endregion
    }
}
