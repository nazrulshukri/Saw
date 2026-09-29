using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MySaw
{
    public class saw
    {
        private string sawprefix; // = "N/A";


        // Declare a Name property of type string:
        public string Name
        {
            get
            {
                return sawprefix;
            }
            set
            {
                sawprefix = value;
            }
        }
    }
}