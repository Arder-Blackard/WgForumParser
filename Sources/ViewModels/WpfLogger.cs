using CommonLib.Logging;
using ForumParser.Collections;

namespace ForumParser.ViewModels
{
    public class WpfLogger : ILogger
    {
        private readonly DefaultLogger _fileLogger;


        #region Auto-properties

        public AsyncObservableCollection<LogEntryBase> Entries { get; } = new AsyncObservableCollection<LogEntryBase>();

        public LogSeverity LogLevel { get; set; }

        #endregion


        #region Public methods

        public void Log( LogEntryBase entry )
        {
            _fileLogger.Log( entry );
            Entries.Add( entry );
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public WpfLogger()
        {
            _fileLogger = new DefaultLogger( "ForumParser.log", "ForumParser", LogSeverity.Debug );
        }
    }
}
