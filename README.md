# web-crawler
Raspagem de tela de uma página WEB
.NET Framework 4.7.2

Neste exemplo existem três exemplo de raspagem de tela, de duas formas diferentes.<br/>
Vou tentar explicar cada um dos exemplos.<br/><br/><br/>

# Extração de resultado da mega-sena

1. Abra a página da Caixa que exibe o resultado da mega-sena:<br/>
http://loterias.caixa.gov.br/wps/portal/loterias/landing/megasena/<br/><br/>
![mega-sena](https://github.com/raywall/web-crawler/blob/master/WebCrawlerConsoleApp/images/caixa-megasena.jpg)

2. No Fiddler, acompanhe as requisições realizadas no carregamento da página<br/>
![fiddler](https://github.com/raywall/web-crawler/blob/master/WebCrawlerConsoleApp/images/fiddler.jpg)<br/>

3. A partir do log de requisições, podemos identificar uma requisição AJAX retornando os dados do concurso em formato JSON<br/>
![json](https://github.com/raywall/web-crawler/blob/master/WebCrawlerConsoleApp/images/resultado-json.jpg)<br/>

4. Efetuamos duas requisições, uma para carregar os cookies no método RequisicaoHttp, e na sequencia a requisição efetuada pelo AJAX
```
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
```

5. Pronto! Já estamos extraindo os dados do sorteio do site da Caixa.
![resultado](https://github.com/raywall/web-crawler/blob/master/WebCrawlerConsoleApp/images/resultado-extraido.jpg)
