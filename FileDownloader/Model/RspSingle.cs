using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSMainForm.models
{
    public class RspSingle<T> : RspErr
    {      
        public string code { get; set; }
        public string message { get; set; }
        public T data { get; set; }
    }


    public class RspSingleLogin<T> : RspErr
    {
        public string message { get; set; }
        public string status { get; set; }
        public string code { get; set; }       
        public T data { get; set; }
    }
}
