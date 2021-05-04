using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Core
{
    public class Settings
    {

        public class Config
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public string DcToken { get; set; }
            public string UcCsrf { get; set; }
            public string UcSession { get; set; }
            public string UcUser { get; set; }

            public int LastActivateIndex { get; set; } = 0;
            public bool AutoStartActivation { get; set; } = true;
            public bool AutoRun { get; set; } = true;
            public int Version { get; set; } = 0;
            public int UpdateDateTableTimeout { get; set; } = 30;

            public bool CompareDateTable { get; set; } = false;


        }

        public Config App = new Config();

        private string _currentConfig;

        public void Load(string path)
        {
            _currentConfig = path;
            if (File.Exists(path))
            {
                var file = File.ReadAllText(path);
                App = JsonConvert.DeserializeObject<Config>(file);
            }
            else
            {
                Save();
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_currentConfig, JsonConvert.SerializeObject(App));
            }
            catch
            {
                throw new Exception("Config: Error save");
            }
        }
    }
}
