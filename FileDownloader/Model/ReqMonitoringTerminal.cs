using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiCSLiteUpdater.Model
{
    public class ReqMonitoringTerminal
    {
    }

    public class ReqCheckUpdate
    {
        public string ipAddress {  get; set; }
        public string lastVersion { get; set; }
    }
    
    public class ReqUpdateStatus
    {
        public string ipAddress {  get; set; }
        public string lastVersion { get; set; }
        public int step { get; set; }
    }
    
    public class RspCheckUpdate
    {
        public bool Success { get; set; }
        public bool HasUpdate { get; set; }
        public string Message { get; set; }

    }
}
