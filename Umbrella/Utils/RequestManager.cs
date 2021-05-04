using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Settings = Umbrella.Properties.Settings;

namespace Umbrella.Utils
{
    public class RequestManager
    {
        private static readonly string ApiV1 = "https://dota-cheats.ru/api/v1/";
        private static readonly string ApiV2 = "https://uc.zone/";
        private static readonly string ApiV3 = "http://s1.pwserver.ru/uca/api.php";

        public enum Api
        {
            DotaCheat,
            UcZone,
            PWS
        }

        public static string ApiUri(Api api)
        {
            return api switch
            {
                Api.DotaCheat => ApiV1,
                Api.UcZone => ApiV2,
                Api.PWS => ApiV3,
                _ => ApiV3
            };
        }


        public static async Task<Response> SendPost(Api api, string uri, Dictionary<string, string> data,
            bool auth = false)
        {
            try
            {
                var cookies = new CookieContainer();
                using var handler = new HttpClientHandler {CookieContainer = cookies};
                using var client = new HttpClient(handler);
                if (auth)
                {
                    if (api == Api.DotaCheat)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", Settings.Default.token);
                    }

                    if (api == Api.UcZone)
                    {
                        client.DefaultRequestHeaders.Add("Cookie",
                            $"xf_session={Settings.Default.session}&xf_user={Settings.Default.user}&xf_csrf={Settings.Default.csrf}");
                    }
                }

                var content = new FormUrlEncodedContent(data);
                var response = await client.PostAsync(ApiUri(api) + uri, content);
                var dataContent = await response.Content.ReadAsStringAsync();
                JObject result = null;
                try
                {
                    result = (JObject) JsonConvert.DeserializeObject(dataContent);
                }
                catch
                {
                    // ignored
                }

                var cookiesDictionary = cookies.GetCookies(new Uri(ApiUri(api) + uri))
                    .ToDictionary(cookie => cookie.Name, cookie => cookie.Value);

                return new Response
                {
                    Data = result,
                    Cookies = cookiesDictionary,
                    DataHtml = dataContent,
                    StatusCode = (int) response.StatusCode
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendPost: " + ex.Message);
                return new Response();
            }
        }

        public static async Task<Response> SendGet(Api api, string uri, bool auth = false)
        {
            try
            {
                var client = new HttpClient();
                if (auth)
                {
                    if (api == Api.DotaCheat)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", Settings.Default.token);
                    }

                    if (api == Api.UcZone)
                    {
                        client.DefaultRequestHeaders.Add("Cookie",
                            $"xf_session={Settings.Default.session};xf_user={Settings.Default.user};xf_csrf={Settings.Default.csrf};");
                    }
                }

                var response = await client.GetAsync(ApiUri(api) + uri);
                JObject result = null;
                var dataContent = response.Content.ReadAsStringAsync().Result;
                try
                {
                    result = JsonConvert.DeserializeObject(dataContent) as JObject;
                }
                catch
                {
                    // ignored
                }

                return new Response
                {
                    Data = result,
                    DataHtml = await response.Content.ReadAsStringAsync(),
                    StatusCode = (int) response.StatusCode
                };
            }
            catch (Exception ex)
            {
                Log.Write(Log.Level.Error, "SendGet: " + ex.Message);
                return new Response();
            }
        }

        public static async Task<bool> IsAuth(Api api, string uri)
        {
            try
            {
                var response = await SendGet(api, uri, true);

                if (api == Api.DotaCheat && response.StatusCode == 200)
                    return true;


                if (api == Api.UcZone)
                {
                    var regex = new Regex("userId: ([0-9]+)").Match(response.DataHtml);
                    if (regex.Groups[1].Value != "0") return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Write(Log.Level.Error, "IsAuth: " + ex.Message);
                return false;
            }
        }
    }
}