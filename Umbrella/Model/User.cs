using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using Umbrella.Utils;

namespace Umbrella.Model
{
    [AddINotifyPropertyChangedInterface]
    public class User
    {
        public string Username { get; set; }
        public bool IsActive { get; set; }
        public string IsActiveText { get; set; }
        public int EndUpgrade { get; set; }

        public async Task GetUser()
        {
            var user = await RequestManager.SendGet(RequestManager.Api.DotaCheat, "user", true);
            if (user.Data != null)
            {
                if (user.Data.ContainsKey("user"))
                {
                    Username = (string)user.Data["user"]?["username"];
                    EndUpgrade = (int)user.Data["user"]?["user_upgrades"]?["6"]?["end_sub"];

                    var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddSeconds(EndUpgrade);
                    IsActive = dt > DateTime.Now;
                    IsActiveText = IsActive ? "Активирован" : "Не активирован";
                }
            }
            
        }
    }
}
