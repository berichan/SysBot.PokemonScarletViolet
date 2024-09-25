using PKHeX.Core;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SysBot.Base;
using System.Linq;
using System.Threading;

namespace SysBot.Pokemon.Web
{
    /// <summary>
    /// Notifies the server using the SignalR client base
    /// </summary>
    public class SignalRNotify<T> : IWebNotify<T> where T : PKM, new()
    {
        private HubConnection Connection { get; }
        private string AuthID { get; }
        private string AuthString { get; }
        private string URI { get; }
        private bool Connected { get; set; }

        private static readonly SemaphoreSlim asyncLock = new SemaphoreSlim(1, 1);

        public SignalRNotify(string authid, string authString, string uriEndpoint)
        {
            AuthID = authid;
            AuthString = authString;
            URI = uriEndpoint;
            Connection = new HubConnectionBuilder()
                .WithUrl(URI)
                .WithAutomaticReconnect()
                .Build();

            Task.Run(AttemptConnection);
        }

        private async void AttemptConnection()
        {
            try
            {
                await Connection.StartAsync();
                LogUtil.LogInfo("Connected succesfully " + Connection.ConnectionId, "SignalR");
                Connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
                paramsToSend.Add("index", result.ToString().WebSafeBase64Encode());
            Task.Run(() => NotifyServerEndpoint(paramsToSend.ToArray()));
        }

        public void NotifyServerOfSeedInfo(SeedSearchResult r, T Result)
        {
            try
            {
                var paramsToSend = new Dictionary<string, string>();
                paramsToSend.Add("seedState", r.Type.ToString().WebSafeBase64Encode());
                paramsToSend.Add("seed", r.Seed.ToString("X16").WebSafeBase64Encode());
                paramsToSend.Add("ot", Result.OriginalTrainerName.WebSafeBase64Encode());
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

                Task.Run(() =>NotifyServerEndpoint(paramsToSend.ToArray()));
            }
            catch { }
        }

        private async void NotifyServerEndpoint(params KeyValuePair<string, string>[] urlParams)
        {
            var authToken = string.Format("&{0}={1}", AuthID, AuthString);
            var uriTry = encodeUriParams(URI, urlParams) + authToken;
            await asyncLock.WaitAsync();
            try
            {
                await Connection.InvokeAsync("ReceiveViewMessage",
                    AuthString, uriTry);
            }
            catch (Exception e) { LogUtil.LogText(e.Message); }
            finally { asyncLock.Release(); }
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
