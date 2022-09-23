using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo.Models
{
    public class Branch : BodyResponse
    {
        public string name { get; set; }
        public string color { get; set; }
        public int timeLimit { get; set; }
        public string idBrand { get; set; }
        public string messageTicket { get; set; }
    }
}
