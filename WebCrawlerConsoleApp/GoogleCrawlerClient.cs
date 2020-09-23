using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebCrawlerConsoleApp
{
    public class GoogleCrawlerClient
    {
        private HtmlWeb request = null;

        #region Events
        public delegate void ErrorOcurredHandler(Exception ex);
        public event ErrorOcurredHandler OnError;
        #endregion

        #region Properties
        public string BaseURL { get; set; }
        #endregion

        #region Crawler methods
        public List<string> BuscarUrlPor(string busca)
        {
            var links = new List<string>();

            try
            {
                var pagina = request.Load($"{BaseURL}/search?q={busca}");
                var parentNode = pagina.DocumentNode.SelectNodes(@"//div[@class='rc']");

                foreach (var node in parentNode[0].SelectNodes(@"//a"))
                    links.Add(node.Attributes["href"].Value);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            return links.Where(w => w.Contains("http")).ToList();
        }
        #endregion

        public GoogleCrawlerClient(string base_url)
        {
            BaseURL = base_url;
            request = new HtmlWeb();
        }
    }
}
