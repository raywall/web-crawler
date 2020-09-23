using Mono.Web;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Security;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerConsoleApp
{
    [Serializable]
    public class RequisicaoHttp
    {
        [NonSerialized]
        public NetworkCredential Credenciais;

        public bool RaiseException = false;

        public bool Redirecionamento302 = true;

        public CookieCollection Cookies = new CookieCollection();

        public bool ValidarDominioCookie = false;

        private readonly object SyncRoot = new object();

        private static readonly object ConnSyncRoot = new object();

        public string AcceptHeader { get; set; }

        public string CookieHeader
        {
            get
            {
                var str = new StringBuilder();

                foreach (Cookie c in Cookies)
                    str.Append($"{c.Name}={c.Value}&");

                return str.ToString();
            }
        }

        public Encoding DefaultEncoding { get; set; }

        public IWebProxy Proxy { get; set; }

        public Version VersaoHttp { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        #region Propriedades da requisição
        public string UserAgent { get; set; }

        public string AcceptLanguage { get; set; }

        public string AcceptEncoding { get; set; }

        public string CacheControl { get; set; }

        public string Expect { get; set; }

        public bool KeepAlive { get; set; }

        public bool PreAuthenticate { get; set; }

        public bool AllowAutoRedirect { get; set; }

        public int Timeout { get; set; }

        public string UACPU { get; set; }
        #endregion

        #region Constructors
        public RequisicaoHttp(bool sslErrorIgnore = true)
        {
            ServicePointManager.Expect100Continue = false;
            DefaultEncoding = Encoding.Default;

            ConfigurarSSL(sslErrorIgnore);
            AumentarLimiteConexao();
        }

        public RequisicaoHttp() : this(true) { }

        ~RequisicaoHttp()
        {
            DiminuirLimiteConexao();
        }
        #endregion

        private void ConfigurarSSL(bool config)
        {
            if (config)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            }
        }

        public static void AumentarLimiteConexao()
        {
            try
            {
                lock (ConnSyncRoot)
                    ServicePointManager.DefaultConnectionLimit = ServicePointManager.DefaultConnectionLimit + 1;
            }
            catch { }
        }

        public static void DiminuirLimiteConexao()
        {
            try
            {
                lock (ConnSyncRoot)
                {
                    int novoLimite = ServicePointManager.DefaultConnectionLimit - 1;

                    if (novoLimite < ServicePointManager.DefaultPersistentConnectionLimit)
                        ServicePointManager.DefaultConnectionLimit = ServicePointManager.DefaultPersistentConnectionLimit;

                    else
                        ServicePointManager.DefaultConnectionLimit = novoLimite;
                }
            }
            catch { }
        }

        public void AdicionarCookie(Cookie cookie)
        {
            if (Cookies[cookie.Name] != null && Cookies[cookie.Name].Domain == cookie.Domain)
                Cookies[cookie.Name].Value = cookie.Value;

            else
                Cookies.Add(cookie);
        }

        public void AdicionarCookies(CookieCollection collection)
        {
            if (collection == null)
                return;

            foreach (Cookie cookie in collection)
                if (Cookies[cookie.Name] != null && Cookies[cookie.Name].Domain == cookie.Domain)
                    Cookies[cookie.Name].Value = cookie.Value;

                else
                    Cookies.Add(cookie);
        }

        public void AdicionarHeader(string header, string value)
        {
            if (Headers == null)
                Headers = new Dictionary<string, string>();

            if (Headers.ContainsKey(header))
                Headers[header] = value;

            else
                Headers.Add(header, value);
        }

        public byte[] CarregarDados(string url, NameValueCollection collection, string referer)
        {
            var parameters = new StringBuilder();
            var result = string.Empty;

            foreach (string param in collection.AllKeys.Where(w => w != null))
                parameters.AppendFormat("{0}={1}&", param.DataEncoder(), (collection[param] == null ? string.Empty : collection[param]).DataEncoder());

            result = parameters.ToString();
            result = result.Remove(result.Length - 1);

            return CarregarDados(url, referer, result);
        }

        public byte[] CarregarDados(string url, string referer)
        {
            return CarregarDados(url, referer, null);
        }

        public byte[] CarregarDados(string url, string referer, string parameters)
        {
            lock (SyncRoot)
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);

                    req.UserAgent = UserAgent;
                    req.PreAuthenticate = PreAuthenticate;
                    req.Accept = "*/*";
                    req.Timeout = Timeout;
                    req.KeepAlive = KeepAlive;
                    req.ContentLength = 0;
                    req.AllowAutoRedirect = AllowAutoRedirect;

                    req.Headers["Accept-Encoding"] = AcceptEncoding;
                    req.Headers["Accept-Language"] = AcceptLanguage;
                    req.Headers["UA-CPU"] = UACPU;
                    req.Headers["Cache-Control"] = CacheControl;

                    if (Proxy != null)
                        req.Proxy = Proxy;

                    if (Headers != null)
                        foreach (var item in Headers)
                            req.Headers.Add(item.Key, item.Value);

                    if (!string.IsNullOrEmpty(referer))
                        req.Referer = referer;

                    req.CookieContainer = new CookieContainer();

                    if (Cookies.Count > 0)
                        foreach (Cookie cookie in Cookies)
                            if (ValidarDominioCookie)
                            {
                                if (req.RequestUri.Host == cookie.Domain)
                                    req.CookieContainer.Add(cookie);
                            }

                            else
                                req.CookieContainer.Add(cookie);

                    if (!string.IsNullOrEmpty(parameters))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(parameters);

                        req.Method = "POST";
                        req.ContentType = "application/x-www-form-urlencoded";
                        req.ContentLength = data.Length;

                        using (Stream stream = req.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                            stream.Close();
                        }

                        var res = (HttpWebResponse)req.GetResponse();
                        byte[] result;

                        using (Stream response = res.GetResponseStream())
                        {
                            Stream str;

                            if (!string.IsNullOrEmpty(res.ContentEncoding) && res.ContentEncoding.ToLower().Contains("gzip"))
                                str = new GZipStream(response, CompressionMode.Decompress);

                            else if (!string.IsNullOrEmpty(res.ContentEncoding) && res.ContentEncoding.ToLower().Contains("defalte"))
                                str = new DeflateStream(response, CompressionMode.Decompress);

                            else
                                str = response;

                            using (var ms = new MemoryStream())
                            {
                                str.CopyTo(ms);
                                result = ms.ToArray();
                            }
                        }

                        AdicionarCookies(res.Cookies);
                        res.Close();

                        return result;
                    }
                }
                catch
                {
                    throw;
                }

                return null;
            }
        }

        public string CarregarHtml(string url)
        {
            return CarregarHtml(url, string.Empty, null);
        }

        public string CarregarHtml(string url, string referer)
        {
            return CarregarHtml(url, string.Empty, referer);
        }

        public string CarregarHtml(string url, NameValueCollection collection = null, string referer = null)
        {
            var parameters = new StringBuilder();
            var result = string.Empty;

            foreach (string param in collection.AllKeys.Where(w => w != null))
                parameters.AppendFormat("{0}={1}&", param.DataEncoder(), (collection[param] == null ? string.Empty : collection[param]).DataEncoder());

            result = parameters.ToString();
            result = result.Remove(result.Length - 1);

            return CarregarHtml(url, result, referer);
        }

        public string CarregarHtml(string url, string parameters, string referer, string contentType = "application/x-www-form-urlencoded", string requestedWith = null)
        {
            lock (SyncRoot)
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);

                    if (Credenciais != null)
                        req.Credentials = Credenciais;

                    if (VersaoHttp != null)
                        req.ProtocolVersion = VersaoHttp;

                    if (Proxy != null)
                        req.Proxy = Proxy;

                    if (!string.IsNullOrEmpty(requestedWith) && requestedWith.Contains(":"))
                        req.Headers[requestedWith.Split(':').FirstOrDefault()] = requestedWith.Split(':').LastOrDefault();

                    req.UserAgent = UserAgent;
                    req.PreAuthenticate = PreAuthenticate;
                    req.Accept = string.IsNullOrEmpty(AcceptHeader) ? "*/*" : AcceptHeader;
                    req.Timeout = Timeout;
                    req.KeepAlive = KeepAlive;
                    req.ContentLength = 0;
                    req.AllowAutoRedirect = AllowAutoRedirect;
                    req.CookieContainer = new CookieContainer();

                    if (Headers != null)
                        foreach (var header in Headers)
                            req.Headers.Add(header.Key, header.Value);

                    if (!string.IsNullOrEmpty(referer))
                        req.Referer = referer;

                    if (Cookies.Count > 0)
                        foreach (Cookie cookie in Cookies)
                            if (ValidarDominioCookie)
                            {
                                if (req.RequestUri.Host == cookie.Domain)
                                    req.CookieContainer.Add(cookie);
                            }

                            else
                                req.CookieContainer.Add(cookie);

                    if (!string.IsNullOrEmpty(parameters))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(parameters);

                        req.Method = "POST";
                        req.ContentType = contentType;
                        req.ContentLength = data.Length;

                        using (Stream stream = req.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                            stream.Close();
                        }
                    }

                    string 
                        headerLocation = string.Empty, 
                        result = string.Empty;

                    using (var res = (HttpWebResponse)req.GetResponse())
                    {
                        if (res.ContentType.StartsWith("image"))
                            using (Stream response = res.GetResponseStream())
                                result = string.Format("data:{0};base64,{1}", res.ContentType, Convert.ToBase64String(response.ReadAllBytes()));

                        else if (res.ContentType.ToLower().Contains("pdf"))
                            using (Stream response = res.GetResponseStream())
                                result = string.Format("pdf:{0}", Convert.ToBase64String(response.ReadAllBytes()));

                        else
                            using (Stream response = res.GetResponseStream())
                            {
                                Stream str = null;

                                try
                                {
                                    if (!string.IsNullOrEmpty(res.ContentEncoding) && res.ContentEncoding.ToLower().Contains("gzip"))
                                        str = new GZipStream(response, CompressionMode.Decompress);

                                    else if (!string.IsNullOrEmpty(res.ContentEncoding) && res.ContentEncoding.ToLower().Contains("deflate"))
                                        str = new DeflateStream(response, CompressionMode.Decompress);

                                    else
                                        str = response;

                                    using(var reader = new StreamReader(str, DefaultEncoding))
                                    {
                                        result = reader.ReadToEnd();
                                        reader.Close();
                                    }

                                    response.Close();
                                }
                                finally
                                {
                                    if (str != null)
                                        str.Dispose();
                                }
                            }

                        if (res.Cookies == null || res.Cookies.Count == 0)
                        {
                            if (!string.IsNullOrEmpty(res.Headers["Set-Cookie"]))
                            {
                                var cookies = res.Headers["Set-Cookie"].Replace("path=/;", string.Empty).Replace("path=/", string.Empty).Replace("HttpOnly", string.Empty).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (var c in cookies)
                                {
                                    if (string.IsNullOrEmpty(c))
                                        continue;

                                    var val = c.Trim();
                                    var rel = val.IndexOf('=');

                                    if (rel >= 0)
                                        AdicionarCookie(new Cookie(val.Substring(0, rel), val.Substring(rel + 1), "/", new Uri(url).Host));
                                }
                            }
                        }

                        else
                            AdicionarCookies(res.Cookies);

                        if (!string.IsNullOrEmpty(headerLocation) && Uri.IsWellFormedUriString(headerLocation, UriKind.Absolute))
                        {
                            if (Redirecionamento302)
                                return CarregarHtml(headerLocation, url);

                            return headerLocation;
                        }

                        return result;
                    }
                }
                catch (WebException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }

    #region Extension methods
    public static class ExtensionMethods
    {
        private const int BUFFER_SIZE = 1024;

        public static string DataEncoder(this string value)
        {
            try
            {
                return HttpUtility.UrlEncode(Encoding.GetEncoding("ISO-8859-1").GetBytes(value));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static byte[] ReadAllBytes(this Stream stream)
        {
            var buffer = new byte[BUFFER_SIZE];
            var bytesRead = 0;
            var inStream = new BufferedStream(stream);
            var outStream = new MemoryStream();

            while ((bytesRead = inStream.Read(buffer, 0, BUFFER_SIZE)) > 0)
                outStream.Write(buffer, 0, bytesRead);

            return outStream.GetBuffer();
        }

        public static Dictionary<string, string> ToDictionary(this NameValueCollection collection)
        {
            var dict = new Dictionary<string, string>();

            try
            {
                foreach (var key in collection.AllKeys)
                    dict.Add(key, collection[key]);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return dict;
        }
    }
    #endregion
}
