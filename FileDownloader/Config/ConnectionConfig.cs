using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace DigiCSLiteUpdater.Config
{
    internal class ConnectionConfig
    {
        public static string ConnectionUrl { get; set; }
        public static int DownloadTimeoutMinutes { get; set; }

        public static string PathGetExecTime = "/util/getbygroup/EXECTIME";
        public static string PathGetEncKey = "/util/getbygroup/ENCKEY";
        public static string PathCheckVersion = "/monitoringterminal/updates/check";
        public static string PathUpdateStep = "/monitoringterminal/updates/status";
        public static string PathAuth = "/auth/get-token";
    }
}
