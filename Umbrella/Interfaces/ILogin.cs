using System;
using System.Collections.Generic;
using System.Text;

namespace Umbrella.Interfaces
{
    interface ILogin
    {
        //delegate void StatusDelegate<in T>(T obj);
        //public StatusDelegate<bool> LoginStatus { get; set; }
        public Action<int> LoginStatus { get; set; }
        //public int LoginStatus { get; set; }
    }
}
