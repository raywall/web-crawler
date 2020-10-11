using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WebCrawlerConsoleApp
{
    public class CaixaCrawlerClient
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

        #region Crawler Methods
        public List<int> Megasena()
        {
            var dezenas = new List<int>();

            try
            {
                var pagina = client.CarregarHtml($"{BaseURL}/wps/portal/loterias/landing/megasena/");
                pagina = client.CarregarHtml($"{BaseURL}/wps/portal/loterias/landing/megasena/!ut/p/a1/04_Sj9CPykssy0xPLMnMz0vMAfGjzOLNDH0MPAzcDbwMPI0sDBxNXAOMwrzCjA0sjIEKIoEKnN0dPUzMfQwMDEwsjAw8XZw8XMwtfQ0MPM2I02-AAzgaENIfrh-FqsQ9wNnUwNHfxcnSwBgIDUyhCvA5EawAjxsKckMjDDI9FQE-F4ca/dl5/d5/L2dBISEvZ0FBIS9nQSEh/pw/Z7_HGK818G0KO6H80AU71KG7J0072/res/id=buscaResultado/c=cacheLevelPage/?timestampAjax=1602443186876");

                if (string.IsNullOrEmpty(pagina))
                    return dezenas;

                var result = JObject.Parse(pagina);

                dezenas = new List<int> {
                    ((string)result["dezenasSorteadasOrdemSorteio"][0]).ToInt(),
                    ((string)result["dezenasSorteadasOrdemSorteio"][1]).ToInt(),
                    ((string)result["dezenasSorteadasOrdemSorteio"][2]).ToInt(),
                    ((string)result["dezenasSorteadasOrdemSorteio"][3]).ToInt(),
                    ((string)result["dezenasSorteadasOrdemSorteio"][4]).ToInt(),
                    ((string)result["dezenasSorteadasOrdemSorteio"][5]).ToInt()
                };

                Console.WriteLine($"Tipo de jogo: {result["tipoJogo"]}");
                Console.WriteLine($"Número do concurso: {result["numero"]}");
                Console.WriteLine($"Data do sorteio: {result["dataApuracao"]}");
                Console.WriteLine($"Números sorteados: {string.Join(", ", dezenas.ToArray())}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            return dezenas;
        }
        #endregion

        public CaixaCrawlerClient(string base_url)
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

    public static class CaixaExtensionMethods
    {
        public static int ToInt(this string value)
        {
            int.TryParse(value, out int result);
            return result;
        }
    }
}
