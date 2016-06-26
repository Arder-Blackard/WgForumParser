using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace ForumParser.Views.Controls
{
    public class CefWebBrowser : ChromiumWebBrowser
    {
        #region Constants

        public static readonly DependencyProperty LoadHandlerProperty =
            DependencyProperty.Register( "LoadHandler",
                                         typeof (ILoadHandler),
                                         typeof (CefWebBrowser),
                                         new FrameworkPropertyMetadata( default(ILoadHandler), LoadHandler_PropertyChanged ) );

        #endregion


        #region Properties

        public new ILoadHandler LoadHandler
        {
            get { return (ILoadHandler) GetValue( LoadHandlerProperty ); }
            set { SetValue( LoadHandlerProperty, value ); }
        }

        #endregion


        #region Event handlers

        private static void LoadHandler_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs args )
        {
            var browser = d as CefWebBrowser;
            if ( browser != null )
                ((ChromiumWebBrowser) browser).LoadHandler = args.NewValue as ILoadHandler;
        }

        #endregion
    }
}
