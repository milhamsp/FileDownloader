namespace FileDownloader.Config
{
    internal class FtpConfig
    {
        public static string Protocol {  get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string Fingerprint {  get; set; }
        public static string Host { get; set; }
        public static string RemoteDirectory { get; set; }
        public static string DownloadDirectory { get; set; }
        public static string TempDirectory { get; set; }
        public static string TargetDirectory { get; set; }
        public static string LogDirectory { get; set; }
    }
}
