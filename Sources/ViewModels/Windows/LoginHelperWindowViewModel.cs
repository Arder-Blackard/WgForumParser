using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CefSharp;
using ForumParser.Services;
using ForumParser.Services.CefHandlers;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Windows
{
    public class LoginHelperWindowViewModel : SimpleWindowViewModelBase
    {
        #region Fields

        private readonly CookieService _cookieService;
        private readonly SettingsManager _settingsManager;

        private bool _isLoginSuccessful;

        private ILoadHandler _loadHandler;
        private IRequestHandler _requestHandler;

        private string _sessionId;
        private string _address;
        private string _rootAddress;

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

        public string RootAddress
        {
            get { return _rootAddress; }
            set { SetValue( ref _rootAddress, value ); }
        }

        public string InitialAddress
        {
            set
            {
                Address = value;
                RootAddress = new Uri( value ).GetComponents( UriComponents.SchemeAndServer, UriFormat.Unescaped );
            }
        }

        public IRequestHandler RequestHandler
        {
            get { return _requestHandler; }
            set
            {
                if ( _requestHandler == value )
                    return;
                _requestHandler = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region Initialization

        public LoginHelperWindowViewModel( CookieService cookieService, SettingsManager settingsManager )
        {
            var cefLoadHandler = new CefLoadHandler();
            cefLoadHandler.MainFrameLoadEnd += CefLoadHandler_MainFrameLoadEnd;
            cefLoadHandler.MainFrameLoadStart += CefLoadHandler_MainFrameLoadStart;
            LoadHandler = cefLoadHandler;

            var cefRequestHandler = new CefRequestHandler();
            cefRequestHandler.BeforeBrowse += CefRequestHandler_BeforeBrowse;
            RequestHandler = cefRequestHandler;

            _cookieService = cookieService;
            _settingsManager = settingsManager;
        }

        #endregion


        #region Event handlers

        private async void CefRequestHandler_BeforeBrowse( IBrowser browser, IFrame frame, IRequest request, bool isRedirect )
        {
            if ( frame.IsMain )
            {
                var response = await frame.EvaluateScriptAsync( "(function(){var input=document.querySelector(\"[type='email']\"); return input ? input.value : null;})()" );

                if ( response?.Result != null )
                    _settingsManager.Settings.Email = response.Result?.ToString();

                _settingsManager.Save();
            }
        }

        private void CefLoadHandler_MainFrameLoadStart( FrameLoadStartEventArgs args )
        {
        }

        private async void CefLoadHandler_MainFrameLoadEnd( FrameLoadEndEventArgs args )
        {
            var result = await args.Frame.EvaluateScriptAsync( "document.querySelector(\".js-submit-title\").innerText" );

////            await args.Frame.EvaluateScriptAsync( $"document.querySelector(\"[type='email']\").value = \"{_settingsManager.Settings.Email ?? ""}\"" );

            var dispatcher = Dispatcher.CurrentDispatcher;
            await Task.Run( async () =>
            {
                var cookies = await _cookieService.GetCookies( RootAddress + "/" );
                var sessionIdCookie = cookies.FirstOrDefault( c => c.Name == "frm_session_id" );
                if ( sessionIdCookie != null && sessionIdCookie.Value.Length > 50 )
                {
                    dispatcher.Invoke( () =>
                    {
                        SessionId = sessionIdCookie.Value;
                        IsLoginSuccessful = true;
                    }, DispatcherPriority.DataBind );
                }
            } );
        }

        #endregion
    }
}
