using System;
using CefSharp;

namespace ForumParser.Services.CefHandlers
{
    internal sealed class CefLoadHandler : ILoadHandler
    {
        #region Events and invocation

        public event Action<FrameLoadStartEventArgs> MainFrameLoadStart;
        public event Action<FrameLoadEndEventArgs> MainFrameLoadEnd;

        public void OnLoadingStateChange( IWebBrowser browserControl, LoadingStateChangedEventArgs args )
        {
        }

        public void OnFrameLoadStart( IWebBrowser browserControl, FrameLoadStartEventArgs args )
        {
            if ( args.Frame.Identifier == args.Browser.MainFrame.Identifier )
                MainFrameLoadStart?.Invoke( args );
        }

        public void OnFrameLoadEnd( IWebBrowser browserControl, FrameLoadEndEventArgs args )
        {
            if ( args.Frame.Identifier == args.Browser.MainFrame.Identifier && args.HttpStatusCode == 200 )
                MainFrameLoadEnd?.Invoke( args );
        }

        public void OnLoadError( IWebBrowser browserControl, LoadErrorEventArgs args )
        {
        }

        #endregion
    }
}
