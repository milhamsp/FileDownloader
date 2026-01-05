using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSMainForm.models
{
    class RspSearch : RspErr
    {   
        public string coreJournal { get; set; }
        public string cifNumber { get; set; }
    }

    public class FmtAuthToken
    {
        public string token { get; set; }
    }
}
