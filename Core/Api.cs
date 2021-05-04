using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Core.Models;
using Newtonsoft.Json;
using Version = Core.Models.Version;

namespace Core
{
    public class Api
    {
        public enum ApiType
        {
            Uc,
            DotaCheats,
            Pws
        }

        public string GetApi(ApiType type)
        {
            return type switch
            {
                ApiType.Uc => "https://uc.zone/",
                ApiType.DotaCheats => "https://dota-cheats.ru/api/v1/",
                ApiType.Pws => "http://s1.pwserver.ru/uca/api.php",
                _ => ""
            };
        }

        public int PromocodeCount()
        {
            return 60;
        }


        public async Task<Response> Post(ApiType type, string uri, Dictionary<string, string> data, bool auth = false)
        {
            var api = GetApi(type);
            var response = new Response();
            try
            {
                var cookies = new CookieContainer();
                using var handler = new HttpClientHandler { CookieContainer = cookies };
                using var client = new HttpClient(handler);

                if (auth)
                {
                    if (type == ApiType.DotaCheats)
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", App.Config.App.DcToken);

                    if (type == ApiType.Uc)
                        client.DefaultRequestHeaders.Add("Cookie", $"xf_session={App.Config.App.UcSession}&xf_user={App.Config.App.UcUser}&xf_csrf={App.Config.App.UcCsrf}");
                }

                var res = await client.PostAsync(api + uri, new FormUrlEncodedContent(data));
                response.Html = await res.Content.ReadAsStringAsync();
                response.Code = res.StatusCode;
                try
                {
                    response.Json = JsonConvert.DeserializeObject(response.Html);
                }
                catch (Exception ej)
                {
                    Log.Write(Log.Level.Error, $"Json convert error: {ej.Message}");
                }
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, $"Api: {Enum.GetName(typeof(ApiType), type)}, Uri: {api}{uri}, Data:{string.Join(';', data.Select(x => $"{x.Key}={x.Value}" ))}, Text: {e.Message}");
            }

            return response;
        }

        public Response Get(ApiType type, string uri, bool auth = false)
        {
            var api = GetApi(type);
            var response = new Response();
            try
            {
                var handler = new HttpClientHandler();
                var client = new HttpClient(handler);
            }
            catch (Exception e)
            {
                Log.Write(Log.Level.Error, $"Api: {Enum.GetName(typeof(ApiType), type)}, Uri: {api}{uri}, Text: {e.Message}");
            }

            return response;
        }


        public Version GetVersion() => Get(ApiType.Pws, "?version").Json as Version;
        
    }
}