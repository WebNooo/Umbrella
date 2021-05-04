using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Core.Models;

namespace Core
{
    public class App
    {
        public static Settings Config;
        public Api Api;
        public List<TimeTable> DateTimes { get; set; } = new List<TimeTable>();

        public App()
        {
            Config = new Settings();
            Config.Load($"{Environment.CurrentDirectory}/config.json");
            Api = new Api();



            GenerateTimeTable();
        }


        public void ActivationStart()
        {

        }

        public void ActivationStop()
        {

        }

        public void GenerateTimeTable()
        {
            new Thread(() =>
            {
                while (true)
                {
                    DateTimes.Clear();


                    var promocodeCount = Api.PromocodeCount();

                    var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 5, 59, 0);

                    for (var i = 0; i < promocodeCount; i++)
                    {
                        date = date.AddSeconds(18 * 60 / promocodeCount * 60);
                        DateTimes.Add(new TimeTable { Time = date, Skip = date < DateTime.Now });
                    }


                    Thread.Sleep(Config.App.UpdateDateTableTimeout * 1000);
                }
            }).Start();
            
        }
    }
}