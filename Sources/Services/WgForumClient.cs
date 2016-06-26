using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using ForumParser.Exceptions;

namespace ForumParser.Services
{
    public class WgForumClient : WebClient
    {
        #region Fields

        private readonly string _baseUrl;

        #endregion

        #region Properties

        private CookieContainer CookieContainer { get; set; }

        #endregion


        /// <summary>
        ///     Creates instance of the Wargaming forum client.
        /// </summary>
        /// <param name="baseUrl">URL of any forum page</param>
        /// <param name="forumSessionId"></param>
        public WgForumClient(string baseUrl, string forumSessionId)
        {
            _baseUrl = baseUrl;
            CookieContainer = new CookieContainer();
            CookieContainer.SetCookies(getUriDomain(baseUrl), forumSessionId);
        }

        #region Overrides of WebClient

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);

            if (request != null)
                request.CookieContainer = CookieContainer;

            return request;
        }

        #endregion

        #region Common routines

        private static string CookiesToString(CookieContainer container)
        {
            var table = (Hashtable)container.GetType().InvokeMember("m_domainTable",
                                                                    BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance,
                                                                    null,
                                                                    container,
                                                                    new object[] { });

            var sb = new StringBuilder();
            foreach (DictionaryEntry domain in table)
            {
                var list = (SortedList)domain.Value.GetType().InvokeMember("m_list", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, domain.Value, new object[] { });

                foreach (DictionaryEntry value in list)
                {
                    var cookieCollection = (CookieCollection)value.Value;
                    foreach (Cookie cookie in cookieCollection)
                        sb.AppendFormat("    {0}={1} ; Domain = {2}{3}\r\n", cookie.Name, cookie.Value, cookie.Domain, value.Key);
                }
            }

            return sb.ToString();
        }

        private static bool IsAbsoluteUrl(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }

        private static Uri getUriDomain(string uri)
        {
            return new Uri(new Uri(uri).GetLeftPart(UriPartial.Authority));
        }

        private static string CombineUriWithHost(Uri baseUri, string relativePath)
        {
            var host = new Uri(baseUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
            return new Uri(host, relativePath).ToString();
        }

/*        /// <summary>
        ///     Loads an image from the given URL using the preinitialized WebClient.
        /// </summary>
        private Image LoadImage(string url)
        {
            var bytes = DownloadData(url);

            var contentType = new ContentType(ResponseHeaders[HttpResponseHeader.ContentType]);
            if (!contentType.MediaType.StartsWith("image/"))
                throw new ForumParserException("Загруженный файл не является изображением");

            try
            {
                return Image.FromStream(new MemoryStream(bytes));
            }
            catch (ArgumentException ex)
            {
                throw new ForumParserException("Загруженный файл не является изображением", ex);
            }
        }*/

        /// <summary>
        ///     Loads an HTML page calling the given function to get the bytes stream.
        /// </summary>
        private string GetHtmlResponse(Func<byte[]> requestFunc)
        {
            Stream responseStream;
            ContentType contentType;
            try
            {
                responseStream = new MemoryStream(requestFunc());
                contentType = new ContentType(ResponseHeaders[HttpResponseHeader.ContentType]);
            }
            catch (WebException ex) //  parse the response page anyway ( we could get the 404 error )
            {
                if (ex.Response == null)
                    throw new ForumParserException("Ошибка при загрузке страницы", ex);

                //  catch response cookies 
                CookieContainer.SetCookies(ex.Response.ResponseUri, ex.Response.Headers[HttpResponseHeader.SetCookie]);
                responseStream = ex.Response.GetResponseStream();
                contentType = new ContentType(ex.Response.Headers[HttpResponseHeader.ContentType]);
            }

            if (responseStream == null)
                throw new ForumParserException("Ошибка при загрузке страницы. Невозможно прочитать ответ сервера");

            //  Load the HTML document using correct encoding

            if (contentType.MediaType != MediaTypeNames.Text.Html)
                throw new ForumParserException("Загруженный документ не является HTML страницей");

            var encoding = Encoding.GetEncoding( contentType.CharSet ?? Encoding.UTF8.WebName );

            return new StreamReader(responseStream, encoding).ReadToEnd();
        }

        public string DownloadHtmlPage(string url)
        {
            return GetHtmlResponse(() => DownloadData(url));
        }

        public string PostHtmlForm(string url, NameValueCollection formData, string referer = null)
        {
            return GetHtmlResponse(() => UploadValues(url, "POST", formData));
        }

        #endregion

        #region Authentication


        #endregion
    }
    
}
