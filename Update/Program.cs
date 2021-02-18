using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Update
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists(Environment.CurrentDirectory + "/update.zip"))
            {
                Update();
            }
            else
            {
                try
                {
                    Console.WriteLine("Download files update...");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile("http://s1.pwserver.ru/uca/api.php?download", "update.zip");
                    }
                    Console.WriteLine("Download end.");
                    Console.WriteLine("Start update.");

                    Update();
                    Console.WriteLine("End update.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("update.zip not found");
                    Console.ReadKey();
                }
            }
        }

        public static void Update()
        {
            string[] files = Directory.GetFiles(Environment.CurrentDirectory);

            foreach (var file in files)
            {
                if (!file.Contains("Update.exe") && !file.Contains("update.zip"))
                {
                    File.Delete(file);
                }
            }


            ZipFile.ExtractToDirectory("update.zip", Environment.CurrentDirectory);

            Process.Start("Umbrella.exe");
            Process.GetCurrentProcess().Kill();
        }
    }
}