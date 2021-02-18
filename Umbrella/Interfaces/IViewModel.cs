using System;
using System.Collections.Generic;
using System.Text;

namespace Umbrella.Interfaces
{
    public interface IViewModel
    {
        public Action<string> UpdateTitle { get; set; }

    }
}
