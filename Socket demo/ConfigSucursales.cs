using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo
{
    public class ConfigSucursales
    {
        public int statusCode { get; set; }
        public Sucursal[] body { get; set; }
        public string message { get; set; }
    }

    public class SucursalComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return (new CaseInsensitiveComparer()).Compare(((Sucursal)x).name, ((Sucursal)y).name);
        }
    }
}
