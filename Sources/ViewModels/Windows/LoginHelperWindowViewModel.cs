using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CefSharp;
using ForumParser.Services;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Windows
{
    public class LoginHelperWindowViewModel : SimpleWindowViewModelBase
    {
        #region Fields

        private readonly CookieService _cookieService;

        private bool _isLoginSuccessful;

        private ILoadHandler _loadHandler;

        private string _sessionId;
        private string _address;
        private string _initialAddress;

        #endregion


        #region Properties

        public string SessionId
        {
            get { return _sessionId; }
            private set
            {
                if ( _sessionId == value )
                    return;
                _sessionId = value;
                OnPropertyChanged();
            }
        }

        public ILoadHandler LoadHandler
        {
            get { return _loadHandler; }
            set
            {
                if ( _loadHandler == value )
                    return;
                _loadHandler = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoginSuccessful
        {
            get { return _isLoginSuccessful; }
            set
            {
                if ( _isLoginSuccessful == value )
                    return;
                _isLoginSuccessful = value;
                OnPropertyChanged();
            }
        }

        public string Address
        {
            get { return _address; }
            set { SetValue( ref _address, value ); }
        }

        public string InitialAddress
        {
            get { return _initialAddress; }
            set
            {
                _initialAddress = new Uri( value ).GetComponents( UriComponents.SchemeAndServer, UriFormat.Unescaped );
                Address = value;
            }
        }

        #endregion


        #region Initialization

        public LoginHelperWindowViewModel( CookieService cookieService )
        {
            var cefLoadHandler = new CefLoadHandler();
            cefLoadHandler.MainFrameUrlLoaded += CefLoadHandler_MainFrameUrlLoaded;
            LoadHandler = cefLoadHandler;
            _cookieService = cookieService;
        }

        #endregion


        #region Event handlers

        private void CefLoadHandler_MainFrameUrlLoaded( string url )
        {
            if ( string.Equals( new Uri( url ).GetComponents( UriComponents.SchemeAndServer, UriFormat.Unescaped ), /*"http://supertest.worldoftanks.com/"*/ _initialAddress, StringComparison.OrdinalIgnoreCase ) )
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                Task.Run( async () =>
                {
                    var cookies = await _cookieService.GetCookies( _initialAddress + "/" /*"http://supertest.worldoftanks.com/"*/ );
                    var sessionIdCookie = cookies.FirstOrDefault( c => c.Name == "frm_session_id" );
                    if ( sessionIdCookie != null && sessionIdCookie.Value.Length > 50 )
                    {
                        dispatcher.Invoke( () =>
                        {
                            SessionId = sessionIdCookie.Value;
                            IsLoginSuccessful = true;
                        } );
                    }
                } );
            }
        }

        #endregion


        #region Nested type: CefLoadHandler

        private sealed class CefLoadHandler : ILoadHandler
        {
            #region Events and invocation

            public event Action<string> MainFrameUrlLoaded;

            public void OnLoadingStateChange( IWebBrowser browserControl, LoadingStateChangedEventArgs loadingStateChangedArgs )
            {
            }

            public void OnFrameLoadStart( IWebBrowser browserControl, FrameLoadStartEventArgs frameLoadStartArgs )
            {
            }

            public void OnFrameLoadEnd( IWebBrowser browserControl, FrameLoadEndEventArgs args )
            {
                if ( args.Frame.Identifier == args.Browser.MainFrame.Identifier && args.HttpStatusCode == 200 )
                    MainFrameUrlLoaded?.Invoke( args.Url );
            }

            public void OnLoadError( IWebBrowser browserControl, LoadErrorEventArgs loadErrorArgs )
            {
            }

            #endregion
        }

        #endregion
    }
}
