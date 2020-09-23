using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace WebCrawlerConsoleApp
{
    public class GitCrawlerClient
    {
        private RequisicaoHttp client = null;
        private HtmlDocument doc = null;

        #region Events
        public delegate void ErrorOcurredHandler(Exception ex);
        public event ErrorOcurredHandler OnError;
        #endregion

        #region Properties
        public string BaseURL { get; set; }
        #endregion

        #region Crawler methods
        public List<string> ListarProjetos()
        {
            var projetos = new List<string>();
            
            try
            {
                var pagina = client.CarregarHtml($"{BaseURL}/raywall?tab=repositories");
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

        public string GetProfileImnage(string file_path)
        {
            try
            {
                var pagina = client.CarregarHtml($"{BaseURL}/raywall");
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

        public bool Conectar(string usuario, string senha)
        {
            try
            {
                var pagina = client.CarregarHtml($"{BaseURL}/login");
                doc.LoadHtml(pagina);

                var data = new NameValueCollection()
                {
                    { "commit", "Sign in" },
                    { "authenticity_token", doc.DocumentNode.SelectSingleNode("//input[@name='authenticity_token']").Attributes["value"].Value },
                    { "ga_id", "" },
                    { "login", usuario },
                    { "password", senha },
                    { "webauthn-support", "supported" },
                    { "webauthn-iuvpaa-support", "unsupported" },
                    { "return_to", "" },
                    { "required_field_9e99", "" },
                    { "timestamp", doc.DocumentNode.SelectSingleNode("//input[@name='timestamp']").Attributes["value"].Value },
                    { "timestamp_secret", doc.DocumentNode.SelectSingleNode("//input[@name='timestamp_secret']").Attributes["value"].Value }
                };

                pagina = client.CarregarHtml($"{BaseURL}/session", data, $"{BaseURL}/login");

                return (pagina.Contains("You are being") & pagina.Contains("redirected"));
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

            client = new RequisicaoHttp(true);

            client.AcceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36";
            client.AcceptLanguage = "pt-BR,pt;q=0.9";
            client.Timeout = 3000;
        }
    }
}
