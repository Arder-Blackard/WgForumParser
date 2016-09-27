using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace ForumParser.Views.Controls
{
    public class CefWebBrowser : ChromiumWebBrowser
    {
        #region Constants

        public static readonly DependencyProperty LoadHandlerProperty = DependencyProperty.Register(
            "LoadHandler",
            typeof ( ILoadHandler ),
            typeof ( CefWebBrowser ),
            new FrameworkPropertyMetadata( default(ILoadHandler), LoadHandler_PropertyChanged ) );

        public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register(
            "Html",
            typeof ( string ),
            typeof ( CefWebBrowser ),
            new FrameworkPropertyMetadata( default(string), ( d, args ) => (d as CefWebBrowser)?.LoadHtml( args.NewValue as string ?? "", "http://rendering/" ) ) );

        public static readonly DependencyProperty RequestHandlerProperty = DependencyProperty.Register(
            "RequestHandler",
            typeof ( IRequestHandler ),
            typeof ( CefWebBrowser ),
            new FrameworkPropertyMetadata( default(IRequestHandler), RequestHandler_PropertyChanged ) );

        #endregion


        #region Properties

        public string Html
        {
            get { return (string) GetValue( HtmlProperty ); }
            set { SetValue( HtmlProperty, value ); }
        }

        public new ILoadHandler LoadHandler
        {
            get { return (ILoadHandler) GetValue( LoadHandlerProperty ); }
            set { SetValue( LoadHandlerProperty, value ); }
        }

        public new IRequestHandler RequestHandler
        {
            get { return (IRequestHandler) GetValue( RequestHandlerProperty ); }
            set { SetValue( RequestHandlerProperty, value ); }
        }

        #endregion


        #region Event handlers

        private static void RequestHandler_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs args )
        {
            var browser = d as CefWebBrowser;
            if ( browser != null )
                ((ChromiumWebBrowser) browser).RequestHandler = args.NewValue as IRequestHandler;
        }

        private static void LoadHandler_PropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs args )
        {
            var browser = d as CefWebBrowser;
            if ( browser != null )
                ((ChromiumWebBrowser) browser).LoadHandler = args.NewValue as ILoadHandler;
        }

        #endregion
    }
}
