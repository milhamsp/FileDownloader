using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace DigiCSLiteUpdater.Config
{
    internal class DirectoryConfig
    {
        public static string RemoteDirectory { get; set; }
        public static string DownloadDirectory { get; set; }
        public static string TempDirectory { get; set; }
        public static string TargetDirectory { get; set; }
        public static string LogDirectory { get; set; }
        public static string AppExeDirectory { get; set; }
    }
}
