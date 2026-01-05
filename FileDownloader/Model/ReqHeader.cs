using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSMainForm.models
{
    public class ReqHeader
    {
        public string region { get; set; }
        public string branch { get; set; }
        public string outlet { get; set; }
        public string terminal { get; set; }
        public string teller { get; set; }
        public string overrideFlag { get; set; }
        public string messageHandlerFlag { get; set; }
        public string supervisorId { get; set; }
        public string mstid { get; set; }
        public string ipaddress { get; set; }
    }
    
    public class ReqHeaderAuth
    {
        public string key { get; set; }
    }
}
