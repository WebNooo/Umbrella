using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Core.Models
{
    public class Response
    {
        public string Html { get; set; }
        public object Json { get; set; } = null;
        public HttpStatusCode Code { get; set; }
        public Dictionary<string, string> Cookies { get; set; }
    }
}
