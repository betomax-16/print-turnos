using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo.Models
{
    public class Brand : BodyResponse
    {
        public string name { get; set; }
        public string color { get; set; }
        public string apiKey { get; set; }
        public string logo { get; set; }
        public string url { get; set; }
        public string mimeType { get; set; }
    }
}
