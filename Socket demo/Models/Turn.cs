using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo.Models
{
    public class Turn : BodyResponse
    {
        public string turn { get; set; }
        public Branch branch { get; set; }
    }
}
