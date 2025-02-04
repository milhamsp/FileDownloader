using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSMainForm.models
{
    public class DropdownVM
    {
        public int id { get; set; }
        public string value { get; set; }
    }

    public class DropdownPropVM
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class DropdownJenisKartuVM
    {
        public int id { get; set; }
        public string value { get; set; }
        public string isactive { get; set; }
    }

    public class DropdownComplainVM
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string deskripsi { get; set; }
    }

}
