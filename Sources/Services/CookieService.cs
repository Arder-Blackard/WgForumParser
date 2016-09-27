using System.Collections.Generic;
using System.Threading.Tasks;
using CefSharp;

namespace ForumParser.Services
{
    public sealed class CookieService : ISingletonService
    {
        #region Public methods

        public Task<IList<Cookie>> GetCookies( string address )
        {
            var cookieVisitor = new CookieVisitior();
            Cef.GetGlobalCookieManager().VisitUrlCookies( address, true, cookieVisitor );
            return cookieVisitor.GetCookies();
        }

        #endregion


        #region Nested type: CookieVisitior

        private class CookieVisitior : ICookieVisitor
        {
            #region Fields

            private readonly IList<Cookie> _cookies = new List<Cookie>();
            private readonly TaskCompletionSource<IList<Cookie>> _taskCompletionSource = new TaskCompletionSource<IList<Cookie>>();

            #endregion


            #region Public methods

            public bool Visit( Cookie cookie, int count, int total, ref bool deleteCookie )
            {
                _cookies.Add( cookie );
                if ( count == total - 1 )
                    _taskCompletionSource.SetResult( _cookies );
                return true;
            }

            public Task<IList<Cookie>> GetCookies() => _taskCompletionSource.Task;

            #endregion
        }

        #endregion
    }
}
