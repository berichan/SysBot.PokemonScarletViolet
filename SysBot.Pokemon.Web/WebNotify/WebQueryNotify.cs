using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SysBot.Pokemon.Web
{
    /// <summary>
    /// Notifies the server using Uri-encoded queries
    /// </summary>
    public class WebQueryNotify<T> : IWebNotify<T> where T : PKM, new()
    {
        private string AuthID { get; }
        private string AuthString { get; }
        private string URI { get; }
        public WebQueryNotify(string authid, string authString, string uriEndpoint)
        {
            AuthID = authid;
            AuthString = authString;
            URI = uriEndpoint;
        }

        public void NotifyServerOfState(WebTradeState state, string trainerName, params KeyValuePair<string, string>[] additionalParams)
        {
            var paramsToSend = new Dictionary<string, string>();
            paramsToSend.Add("wts", state.ToString().WebSafeBase64Encode());
            foreach (var p in additionalParams)
                paramsToSend.Add(p.Key, p.Value.WebSafeBase64Encode());
            var trainerNameNumber = trainerName[trainerName.Length - 1];
            var hasNum = int.TryParse(trainerNameNumber.ToString(), out var result);
            if (hasNum)
                paramsToSend.Add("index", result.ToString());
            NotifyServerEndpoint(paramsToSend.ToArray());
        }

        public void NotifyServerOfSeedInfo(SeedSearchResult r, T Result)
        {
            try
            {
                var paramsToSend = new Dictionary<string, string>();
                paramsToSend.Add("seedState", r.Type.ToString().WebSafeBase64Encode());
                paramsToSend.Add("seed", r.Seed.ToString("X16").WebSafeBase64Encode());
                paramsToSend.Add("ot", Result.OT_Name.WebSafeBase64Encode());
                paramsToSend.Add("dex", Result.Species.ToString().WebSafeBase64Encode());

                var shinyState = "None";
                if (r.Type == Z3SearchResult.Success)
                {
                    SeedSearchUtil.GetShinyFrames(r.Seed, out var frames, out var type, out var ivs, r.Mode);
                    if (frames != null && type != null)
                    {
                        if (frames.Length > 0 && type.Length > 0)
                            shinyState = "";

                        for (int i = 0; i < frames.Length; ++i)
                            if (type[i] != 0)
                                shinyState += $"{frames[i]}@{type[i]}@";
                    }
                }
                paramsToSend.Add("shiny", shinyState.WebSafeBase64Encode());

                NotifyServerEndpoint(paramsToSend.ToArray());
            }
            catch { }
        }

        private void NotifyServerEndpoint(params KeyValuePair<string, string>[] urlParams)
        {
            var authToken = string.Format("&{0}={1}", AuthID, AuthString);
            var uriTry = encodeUriParams(URI, urlParams) + authToken;
            var request = (HttpWebRequest)WebRequest.Create(uriTry);
            try
            {
                request.Method = WebRequestMethods.Http.Head;
                request.Timeout = 20000;

                request.ServicePoint.ConnectionLeaseTimeout = 5000;
                request.ServicePoint.MaxIdleTime = 5000;

                using (var response = request.GetResponse())
                {
                    //dispose
                    Console.WriteLine("Connected to endpoint: " + response.ResponseUri.ToString());
                }
            }
            catch (Exception e) { request.Abort(); LogUtil.LogText(e.Message); Environment.Exit(42069); }
        }

        private string encodeUriParams(string uriBase, params KeyValuePair<string, string>[] urlParams)
        {
            if (urlParams.Length < 1)
                return uriBase;
            if (uriBase[uriBase.Length - 1] != '?')
                uriBase += "?";
            foreach (var kvp in urlParams)
                uriBase += string.Format("{0}={1}&", kvp.Key, kvp.Value);

            // remove trailing &
            return uriBase.Remove(uriBase.Length - 1, 1);
        }
    }
}
