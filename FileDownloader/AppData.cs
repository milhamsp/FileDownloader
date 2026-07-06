using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiCSLiteUpdater
{
    internal class AppData
    {
        public static bool SuccessProcess {  get; set; }
        public static string Token { get; set; }
        public static string Key { get; set; }
        public static string Iv { get; set; }
        public static string ExecTime { get; set; }
        public static readonly string RunId = Guid.NewGuid().ToString("N");
    }
}
