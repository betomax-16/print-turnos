using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo.Models
{
    public class ResponseWreapper<T>
    {
        public int statusCode { get; set; }
        public T body { get; set; }
        public string message { get; set; }
    }

    public class ResponseWreapperComparer<T> : IComparer
    {
        public int Compare(object x, object y)
        {
            if (((T)x).GetType() == typeof(Brand))
            {
                Brand brand = ((T)x) as Brand;
                return (new CaseInsensitiveComparer()).Compare(brand.name, brand.name);
            }
            else if (((T)x).GetType() == typeof(Branch))
            {
                Branch branch = ((T)x) as Branch;
                return (new CaseInsensitiveComparer()).Compare(branch.name, branch.name);
            }
            else
            {
                return 0;
            }
        }
    }
}
