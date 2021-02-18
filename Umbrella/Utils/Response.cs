using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Umbrella.Utils
{
    public struct Response
    {
        public Dictionary<string, string> Cookies;
        public JObject Data;
        public string DataHtml;
        public int StatusCode;
    }
}
