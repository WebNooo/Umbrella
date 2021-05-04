using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Core;
using DeviceId;
using PropertyChanged;
using ReactiveUI;
using Umbrella.Interfaces;
using Umbrella.Model;
using Umbrella.Utils;
using Settings = Umbrella.Properties.Settings;

namespace Umbrella.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainVM : ILogin, IViewModel
    {
        public Action<string> UpdateTitle { get; set; }

        public string Version = "18";

        public class TimeTableItem
        {
            public int Index { get; set; }
            public string Text { get; set; }
            public DateTime Time { get; set; }
            public SolidColorBrush Background { get; set; }
            public bool Selected { get; set; }
            public bool skip { get; set; }
            public ReactiveCommand<int, Unit> Command { get; set; }
        }

        public bool CompareDateTable { 
            get  => Settings.Default.CompareDateTable; 
            set {
                Settings.Default.CompareDateTable = value;
                Settings.Default.Save();
            } 
        }

        public int PromocodeCount { get; set; }
        public int PromocodeBecameCount { get; set; }
        public int PromocodeAlreadyActive { get; set; }
        public Action<int> LoginStatus { get; set; }
        public string ActivationStateText { get; set; } = "Начать активацию";
        public string EndActivation { get; set; }
        public User UserData { get; set; }
        private static readonly ObservableCollection<TimeTableItem> TimeTableData = new ObservableCollection<TimeTableItem>();
        public ICollectionView SomeCollection { get; set; } = CollectionViewSource.GetDefaultView(TimeTableData);
        public MainVM()
        {

            Log.Write(Log.Level.Success, "Program is running...");
            try
            {
                UserData = new User();
                TimeTable();
                var tableTimer = new System.Timers.Timer {Interval = 60000};
                tableTimer.Elapsed += async (sender, args) =>
                {
                    if (Settings.Default.auto_start)
                    {
                        await UserData.GetUser();
                        if (_activation == null && ActivationThread == null)
                        {
                            if (UserData.IsActive == false)
                            {
                                Activation();
                            }
                            else
                            {
                                var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()
                                    .AddSeconds(UserData.EndUpgrade);

                                if ((int) (dt - DateTime.Now).TotalSeconds < 70)
                                {
                                    Activation();
                                }
                            }
                        }
                    }

                    TimeTable();
                };
                tableTimer.Start();

                CheckAuth();
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "MainVM: " + e.Message);
            }
        }
        public void TimeTable()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 5, 59, 0);


                    

                    var current = true;

                    if (xfToken == null)
                    {
                        var uc = await RequestManager.SendGet(RequestManager.Api.UcZone, "", true);
                        var regexXfToken = new Regex("csrf: '([a-z,0-9]+)',").Match(uc.DataHtml);

                        xfToken = regexXfToken.Groups[1].Value;
                    }

                    var result = await RequestManager.SendGet(RequestManager.Api.UcZone,
    $"cheat-statuses/games/DotA2/load-promocode?_xfRequestUri=/&_xfWithData=1&_xfToken={xfToken}&_xfResponseType=json",
    true);
                    var resultClear = ((string)result.Data["html"]?["content"])?.Replace("\t", "").Replace("\n", "");

                    var countPromo = Regex.Match(resultClear ?? string.Empty,
                        "<li class=\"item\">Промокодов сегодня будет сгенерировано: ([0-9]+)</li>",
                        RegexOptions.IgnoreCase).Groups[1].Value;

                    var countAlreadyPromo = Regex.Match(resultClear ?? string.Empty,
                        "<li class=\"item\">Уже сгенерировано: ([0-9]+)</li>",
                        RegexOptions.IgnoreCase).Groups[1].Value;



                    PromocodeAlreadyActive = !int.TryParse(countAlreadyPromo, out var pCA) ? 0 : pCA;
                    PromocodeCount = !int.TryParse(countPromo, out var pC) ? 59 : pC;
                    PromocodeBecameCount = PromocodeCount - PromocodeAlreadyActive;

                    TimeTableData.Clear();

                    for (var i = 0; i < PromocodeCount; i++)
                    {
                        //test if, check start activation time
                        //if (i > 0)  date = date.AddSeconds((18 * 60 / (float)PromocodeCount) * 60);
                        date = date.AddSeconds((18 * 60 / (float)PromocodeCount) * 60);
                        var selected = false;

                        var color = new SolidColorBrush(Color.FromRgb(28, 157, 60));

                        if (date < DateTime.Now)
                        {
                            color = new SolidColorBrush(Color.FromRgb(173, 35, 35));
                        }
                        else
                        {
                            if (current)
                            {
                                color = new SolidColorBrush(Color.FromRgb(33, 173, 194));
                                current = false;
                            }
                        }

                        if (Settings.Default.actIndex == i)
                            selected = true;

                        TimeTableData.Add(new TimeTableItem
                        {
                            Time = date,
                            Text = $"{date.Hour:D2}:{date.Minute:D2}",
                            Background = color,
                            Selected = !selected,
                            skip = date > DateTime.Now,
                            Command = ReactiveCommand.Create<int>((index) =>
                            {
                                Settings.Default.actIndex = index;
                                Settings.Default.Save();
                                TimeTable();
                            }),
                            Index = i
                        });
                    }
                });
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "TimeTable: " + e.Message);
            }
        }
        public async void CheckAuth()
        {
            try
            {
                var ver = await RequestManager.SendGet(RequestManager.Api.PWS, "?version");
                //Notify.Show("fgdsg", "Gfdgdf");
                if (Version != ver.DataHtml)
                {
                    LoginStatus?.Invoke(2);
                    return;
                }

                var device = new DeviceIdBuilder().AddMachineName().AddProcessorId().AddMotherboardSerialNumber()
                    .AddSystemDriveSerialNumber().ToString();

                var access = await RequestManager.SendGet(RequestManager.Api.PWS, "?auth=" + device);

                var dd = access.Data.ContainsKey("allow");
                var bb = (bool) access.Data["allow"];

                if (access.Data.ContainsKey("name"))
                {
                    UpdateTitle?.Invoke($"Версия: {Version}, Лицензия для {access.Data["name"]}");
                }

                if (dd && bb == false)
                {
                    MessageBox.Show("Доступ к программе заблокирован!");
                    LoginStatus?.Invoke(0);
                    return;
                }

                var u = await RequestManager.IsAuth(RequestManager.Api.UcZone, "");
                var d = await RequestManager.IsAuth(RequestManager.Api.DotaCheat, "user");

                if (u && d)
                {
                    await UserData.GetUser();
                    if (UserData.IsActive)
                    {
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
                        EndActivation = epoch.AddSeconds((double) UserData.EndUpgrade).ToString("dd/MM/yy HH:mm");
                    }
                    else
                    {
                        EndActivation = "";
                    }
                }
                else
                {
                    LoginStatus?.Invoke(0);
                }
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "CheckAuth: " + e.Message);
            }
        }
        public ICommand Exit => ReactiveCommand.Create(() =>
        {
            try
            {
                StopActivation();
                Settings.Default.Reset();
                LoginStatus?.Invoke(0);
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "Exit: " + e.Message);
            }
        });
        public string ActivationStatus { get; set; }
        private System.Timers.Timer _activation;
        public int ActivationIndex { get; set; }
        private string xfToken;
        private System.Timers.Timer timer;
        private Task ActivationThread;
        private CancellationTokenSource ActivationSourceToken = new CancellationTokenSource();
        private CancellationToken ActivationToken;
        public void Activation()
        {
            try
            {
                ActivationToken = ActivationSourceToken.Token;
                ActivationThread = new Task(async () =>
                {
                    var uc = await RequestManager.SendGet(RequestManager.Api.UcZone, "", true);
                    var regexXfToken = new Regex("csrf: '([a-z,0-9]+)',").Match(uc.DataHtml);

                    if (regexXfToken.Groups.Count > 1)
                    {
                        if (Settings.Default.auto_start)
                        {
                            var date = TimeTableData.First(x => x.skip);
                            Settings.Default.actIndex = date.Index;
                            Settings.Default.Save();
                        }

                        if (TimeTableData[Settings.Default.actIndex].Time > DateTime.Now)
                        {
                            ActivationStateText = "Остановить активацию";
                            _activation = new System.Timers.Timer {Interval = 1000};
                            var timeDiff = (TimeTableData[Settings.Default.actIndex].Time - DateTime.Now)
                                .TotalMilliseconds;
                            if (timeDiff > 100)
                            {
                                timer = new System.Timers.Timer {Interval = 1000};

                                timer.Elapsed += (sender, args) =>
                                {
                                    var timeSleep = TimeSpan.FromSeconds(
                                        (TimeTableData[Settings.Default.actIndex].Time - DateTime.Now).TotalSeconds);
                                    ActivationStatus =
                                        $"Начнется через: {timeSleep.Hours:D2}:{timeSleep.Minutes:D2}:{timeSleep.Seconds:D2}";
                                };
                                timer.Start();

                                Thread.Sleep((int) timeDiff);
                                timer.Stop();
                            }

                            xfToken = regexXfToken.Groups[1].Value;

                            _activation.Elapsed += ActivationOnElapsed;
                            _activation.Start();
                            Log.Write(Log.Level.Info, "Activation start");

                        }
                        else
                        {
                            ActivationStatus = $"Выбранная дата пропущена";
                        }
                    }
                    else
                    {
                        ActivationStatus = "Ошибка при получении токена";
                    }
                }, ActivationToken);
                ActivationThread.Start();
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "Activation: " + e.Message);
            }
        }

        private bool _reActivate;
        public ICommand ActivationState => ReactiveCommand.Create(() =>
        {

            try
            {
                if (_activation != null && _activation.Enabled || ActivationThread != null && ActivationThread.IsCompleted)
                {
                    StopActivation();
                    return;
                }

                if (!Settings.Default.CompareDateTable)
                {
                    _activation = new System.Timers.Timer { Interval = 1000 };
                    _activation.Elapsed += ActivationOnElapsed;
                    _activation.Start();
                    ActivationStateText = "Остановить активацию";

                }
                else 
                {

                    _reActivate = false;
                    ActivationIndex = 0;
                    CheckAuth();

                    if (UserData.IsActive)
                    {
                        ActivationStatus = "Промокод уже активирован";
                        return;
                    }

                    Activation();

                }

            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "ActivationState: " + e.Message);
            }
        });
        private async void ActivationOnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (UserData.IsActive)
                    return;

                ActivationStatus = $"Ищем код. Проверок: {ActivationIndex}";


                var result = await RequestManager.SendGet(RequestManager.Api.UcZone,
                    $"cheat-statuses/games/DotA2/load-promocode?_xfRequestUri=/&_xfWithData=1&_xfToken={xfToken}&_xfResponseType=json",
                    true);
                var resultClear = ((string) result.Data["html"]?["content"])?.Replace("\t", "").Replace("\n", "");

                var key = Regex.Match(resultClear ?? string.Empty,
                    "<div class=\"gamePromocode\"><div class=\"gamePromocodeItem gamePromocode--promocode is-activated\">(.+?)</div>",
                    RegexOptions.IgnoreCase).Groups[1].Value;

                if (key != "")
                {
                    var resultCode = await RequestManager.SendPost(RequestManager.Api.DotaCheat, "user/set-promocode",
                        new Dictionary<string, string> {{"promocode", key}}, true);
                    if (resultCode.Data.ContainsKey("success") && (bool) resultCode.Data["success"])
                    {
                        Notify.Show("Активация", "Промокод успешно активирован");
                        ActivationStatus = "Промокод успешно активирован";
                        await UserData.GetUser();
                        StopActivation(false);
                        return;
                    }
                    else
                    {
                        if ((string) resultCode.Data["errors"]?[0]?["code"] ==
                            "promo_generator_you_must_wait_x_seconds_for_activate_promocode")
                        {
                            Notify.Show("Активация", "Время активации еще не пришло");
                            ActivationStatus = "Время активации еще не пришло";
                            _reActivate = true;
                        }
                        else if ((string) resultCode.Data["errors"]?[0]?["code"] ==
                                 "promo_generator_promocode_not_exists")
                        {
                            Notify.Show("Активация", "Данного промокода не существует");
                            ActivationStatus = "Данного промокода не существует";
                        }
                        else
                        {
                            Notify.Show("Активация", "Промокод был кем-то активирован");
                            ActivationStatus = "Промокод был кем-то активирован";
                            _reActivate = true;
                        }
                    }

                    StopActivation(false);
                }

                if (_reActivate)
                {
                    Settings.Default.actIndex += 1;
                    Settings.Default.Save();
                    Activation();
                }


                // if (ActivationIndex > 300)
                // {
                //     StopActivation();
                // }

                ActivationIndex++;
            }
            catch (Exception ex)
            {
                Log.Write(Log.Level.Error, "ActivationOnElapsed: " + ex.Message);

            }
        }
        private void StopActivation(bool withStatus = true)
        {
            try
            {
                Log.Write(Log.Level.Info, "Activation stop");

                if (_activation != null)
                {
                    _activation.Stop();
                    _activation.Elapsed -= ActivationOnElapsed;
                    _activation = null;
                }

                ActivationSourceToken.Cancel();
                ActivationSourceToken.Dispose();
                ActivationSourceToken = new CancellationTokenSource();
                ActivationThread?.Dispose();
                ActivationThread = null;
                ActivationStateText = "Начать активацию";
                if (withStatus) ActivationStatus = "Остановлено";
                timer?.Stop();
                ActivationIndex = 0;
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, "StopActivation: " + e.Message);
            }
        }
    }
}