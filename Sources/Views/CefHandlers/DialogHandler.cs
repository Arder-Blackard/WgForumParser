using CefSharp;
using System.Collections.Generic;

namespace ForumParserWPF.Views.CefHandlers
{
    public class DialogHandler : IDialogHandler
    {
        public bool OnFileDialog( IWebBrowser browserControl,
                                  IBrowser browser,
                                  CefFileDialogMode mode,
                                  string title,
                                  string defaultFilePath,
                                  List<string> acceptFilters,
                                  int selectedAcceptFilter,
                                  IFileDialogCallback callback )
        {
            return false;
        }
    }
}