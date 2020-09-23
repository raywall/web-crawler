﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerConsoleApp
{
    public class GitCrawlerClient
    {
        private HttpWebRequest client = null;
        private HtmlDocument doc = null;

        #region Events
        public delegate void ErrorOcurredHandler(Exception ex);
        public event ErrorOcurredHandler OnError;
        #endregion

        #region Properties
        public string BaseURL { get; set; }
        #endregion

        #region Crawler methods
        public async Task<List<string>> ListarProjetos()
        {
            var projetos = new List<string>();
            
            try
            {
                var pagina = await RequestAsync($"{BaseURL}/raywall?tab=repositories");
                doc.LoadHtml(pagina);

                foreach (var node in doc.DocumentNode.SelectNodes("//a[@itemprop='name codeRepository']"))
                    projetos.Add(node.InnerText.Replace("\n", string.Empty).Trim());
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            return projetos;
        }

        public async Task<string> GetProfileImnage(string file_path)
        {
            try
            {
                var pagina = await RequestAsync($"{BaseURL}/raywall");
                doc.LoadHtml(pagina);

                var avatar = doc.DocumentNode.SelectSingleNode("//img[@class='avatar avatar-user width-full border bg-white']");

                using (var client = new WebClient())
                    client.DownloadFile(avatar.Attributes["src"].Value, Path.Combine(file_path, "raywall.jpg"));

                return Path.Combine(file_path, "raywall.jpg");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            return string.Empty;
        }

        public async Task<bool> Conectar(string usuario, string senha)
        {
            try
            {
                var pagina = await RequestAsync($"{BaseURL}/login");
                doc.LoadHtml(pagina);

                var data = new Dictionary<string, string>
                {
                    { "commit", "Sign in" },
                    { "authenticity_token", doc.DocumentNode.SelectSingleNode("//input[@name='authenticity_token']").Attributes["value"].Value },
                    { "ga_id", "" },
                    { "login", "raywall.malheiros@gmail.com" },
                    { "password", "RAywall123#" },
                    { "webauthn-support", "supported" },
                    { "webauthn-iuvpaa-support", "unsupported" },
                    { "return_to", "" },
                    { "required_field_9e99", "" },
                    { "timestamp", doc.DocumentNode.SelectSingleNode("//input[@name='timestamp']").Attributes["value"].Value },
                    { "timestamp_secret", doc.DocumentNode.SelectSingleNode("//input[@name='timestamp_secret']").Attributes["value"].Value }
                };

                pagina = await RequestAsync($"{BaseURL}/login", data);
                doc.LoadHtml(pagina);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            return false;

        }
        #endregion

        public GitCrawlerClient(string base_url) 
        {
            BaseURL = base_url;
            doc = new HtmlDocument();
        }

        private async System.Threading.Tasks.Task<string> RequestAsync(string url, Dictionary<string, string> data = null)
        {
            var conteudo = string.Empty;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = null;

                if (data != null)
                {
                    var content = Encoding.ASCII.GetBytes(string.Join("&", data.Select(s => $"{s.Key}={s.Value}").ToArray()));

                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = content.Length;

                    using (var stream = request.GetRequestStream())
                        stream.Write(content, 0, content.Length);

                    response = (HttpWebResponse)request.GetResponse();
                }

                else
                    response = (HttpWebResponse)request.GetResponse();

                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader readStream = null;
                    var receiveStream = response.GetResponseStream();

                    readStream = string.IsNullOrEmpty(response.CharacterSet)
                        ? new StreamReader(receiveStream)
                        : new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                    conteudo = readStream.ReadToEnd().Replace("\n", string.Empty);

                    response.Close();
                    readStream.Close();
                }
            }
            catch (WebException ex)
            {
                OnError?.Invoke(ex);
            }

            return conteudo;
        } 
    }
}
