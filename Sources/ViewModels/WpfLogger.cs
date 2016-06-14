using System.Collections.ObjectModel;
using CommonLib.Logging;

namespace ForumParserWPF.ViewModels
{
    public class WpfLogger : ILogger
    {
        #region Auto-properties

        public ObservableCollection<LogEntryBase> Entries { get; set; } = new ObservableCollection<LogEntryBase>();

        public LogSeverity LogLevel { get; set; }

        #endregion


        #region Public methods

        public void Log( LogEntryBase entry ) => Entries.Add( entry );

        #endregion
    }
}