using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSMainForm.models
{
    public class RspAll<T> : RspErr
    {      
        public string code { get; set; }
        public string message { get; set; }
        public List<T> data { get; set; }
    }
}
