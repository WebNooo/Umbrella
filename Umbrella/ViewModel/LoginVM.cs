using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using Umbrella.Interfaces;
using Umbrella.Properties;
using Umbrella.Utils;

namespace Umbrella.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    class LoginVM : ILogin
    {
        public LoginVM()
        {
            Username = Settings.Default.username;
            Password = Settings.Default.password;
        }

        public Action<int> LoginStatus { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string LoginResult { get; set; }

        public ICommand LoginCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            if (Username.Length > 0)
            {
                if (Password.Length > 0)
                {
                    try
                    {
                        var resultDotaCheat = await RequestManager.SendPost(RequestManager.Api.DotaCheat, "auth",
                            new Dictionary<string, string>
                                {{"username", Username}, {"password", Password}});
                        if ((bool) resultDotaCheat.Data["success"])
                        {
                            var resultUcZone = await RequestManager.SendPost(RequestManager.Api.UcZone,
                                "login/login",
                                new Dictionary<string, string>
                                {
                                    {"login", Username},
                                    {"password", Password},
                                    {"remember", "1"}
                                });
                            if (resultUcZone.Cookies.Count() != 3)
                            {
                                LoginResult = "Ошибка авторизации на uc.zone";
                            }
                            else
                            {
                                Settings.Default.username = Username;
                                Settings.Default.password = Password;
                                Settings.Default.token = (string) resultDotaCheat.Data["token"];
                                Settings.Default.user = resultUcZone.Cookies["xf_user"];
                                Settings.Default.csrf = resultUcZone.Cookies["xf_csrf"];
                                Settings.Default.session = resultUcZone.Cookies["xf_session"];
                                Settings.Default.Save();
                                LoginStatus?.Invoke((bool) resultDotaCheat.Data["success"]? 1 : 0);
                            }
                        }
                        else
                        {
                            LoginResult = (string) resultDotaCheat.Data["message"];
                        }
                    }
                    catch
                    {
                        LoginResult = "Возникла ошибка при отправке запроса";
                    }
                }
                else
                {
                    LoginResult = "Заполните пароль";
                }
            }
            else
            {
                LoginResult = "Заполните имя пользователя";
            }
        });
    }
}