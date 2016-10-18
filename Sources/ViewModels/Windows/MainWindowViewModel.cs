using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommonLib.Extensions;
using CommonLib.Logging;
using ForumParser.Collections;
using ForumParser.Exceptions;
using ForumParser.Models;
using ForumParser.Services;
using ForumParser.ViewModels.Controls;
using WpfCommon.Commands;
using WpfCommon.Services;
using WpfCommon.ViewModels.Base;
using WpfCommon.ViewModels.Dialogs;

namespace ForumParser.ViewModels.Windows
{
    /// <summary>
    ///     View-model of the main window.
    /// </summary>
    public class MainWindowViewModel : TaskExecutorViewModelBase, IWindowViewModel
    {
        #region Constants

        public static readonly DependencyProperty TopicUrlProperty = DependencyProperty.Register(
            "TopicUrl", typeof (string), typeof (MainWindowViewModel), new FrameworkPropertyMetadata( default(string) ) );

        public static readonly DependencyProperty ExcludeAdministratorsProperty = DependencyProperty.Register(
            "ExcludeAdministrators", typeof (bool), typeof (MainWindowViewModel), new FrameworkPropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeCoordinatorsProperty = DependencyProperty.Register(
            "ExcludeCoordinators", typeof (bool), typeof (MainWindowViewModel), new FrameworkPropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeDeletedMessagesProperty = DependencyProperty.Register(
            "ExcludeDeletedMessages", typeof (bool), typeof (MainWindowViewModel), new FrameworkPropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ParsePollOnlyProperty = DependencyProperty.Register(
            "ParsePollOnly", typeof (bool), typeof (MainWindowViewModel), new FrameworkPropertyMetadata( default(bool) ) );

        private static readonly Regex FixInvalidPathCharactersRegex = new Regex( @"[\\/\*\?\:""<>]" );

        #endregion


        #region Fields

        private readonly Stack<KeyValuePair<int, User>> _deletedUsersStack = new Stack<KeyValuePair<int, User>>();

        //  Services
        private readonly ForumTopicParser _forumParser;
        private readonly SaveLoadManager _saveLoadManager;
        private readonly TemplateMatcher _templateMatcher;
        private readonly IViewProvider _viewProvider;
        private ICollectionView _allUsers;
        private int _allUsersCount;

        //  Data
        private ForumTopic _forumTopic;
        private AsyncObservableCollection<PreviewChartTemplateViewModel> _previewTemplates = new AsyncObservableCollection<PreviewChartTemplateViewModel>();
        private UserViewModel _selectedUser;
        private string _templateState;
        private ObservableCollection<UserViewModel> _users;
        private ICollectionView _usersWithFeedbackOnly;
        private int _usersWithFeedbackOnlyCount;
        private ICollectionView _usersWithVoteAndFeedback;
        private int _usersWithVoteAndFeedbackCount;
        private ICollectionView _usersWithVoteOnly;
        private int _usersWithVoteOnlyCount;
        private string _viewTitle;
        private ICollectionView _votedUsers;
        private int _votedUsersCount;

        #endregion


        #region Auto-properties

        /// <summary>
        ///     Stores 'frm_session_id' cookie value.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        ///     True if .txt result format should be saved.
        /// </summary>
        public bool SaveTxt { get; set; }

        /// <summary>
        ///     True if .csv result format should be saved.
        /// </summary>
        public bool SaveCsv { get; set; }

        /// <summary>
        ///     Displays log messages in UI.
        /// </summary>
        public ILogger UiLogger { get; }

        /// <summary>
        ///     The last selected final results directory.
        /// </summary>
        public string FinalResultsDirectory { get; set; }

        public string ForumTopicFile { get; set; }

        public string ChartTemplatesFile { get; set; }

        public string ChartsDirectory { get; set; }

        #endregion


        #region Properties

        /// <summary>
        ///     User which is selected in Users data grid.
        /// </summary>
        public UserViewModel SelectedUser
        {
            get { return _selectedUser; }
            set { SetValue( ref _selectedUser, value ); }
        }

        /// <summary>
        ///     Parsed forum topic data.
        /// </summary>
        public ForumTopic ForumTopic
        {
            get { return _forumTopic; }
            private set
            {
                if ( SetValue( ref _forumTopic, value ) )
                {
                    SetUsers( _forumTopic.Users );
                    LoadTemplatesPreview( PreviewTemplates.Select( t => t.Template ).ToList() );
                }
            }
        }

        public AsyncObservableCollection<PreviewChartTemplateViewModel> PreviewTemplates
        {
            get { return _previewTemplates; }
            set { SetValue( ref _previewTemplates, value ); }
        }

        /// <summary>
        ///     All users view.
        /// </summary>
        public ICollectionView AllUsers
        {
            get { return _allUsers; }
            private set { SetValue( ref _allUsers, value ); }
        }

        /// <summary>
        ///     Voted users view.
        /// </summary>
        public ICollectionView VotedUsers
        {
            get { return _votedUsers; }
            private set { SetValue( ref _votedUsers, value ); }
        }

        /// <summary>
        ///     The view presenting users with both vote and feedback.
        /// </summary>
        public ICollectionView UsersWithVoteAndFeedback
        {
            get { return _usersWithVoteAndFeedback; }
            private set { SetValue( ref _usersWithVoteAndFeedback, value ); }
        }

        /// <summary>
        ///     The view presenting users with vote but without feedback.
        /// </summary>
        public ICollectionView UsersWithVoteOnly
        {
            get { return _usersWithVoteOnly; }
            private set { SetValue( ref _usersWithVoteOnly, value ); }
        }

        /// <summary>
        ///     The view presenting users with feedback but without vote.
        /// </summary>
        public ICollectionView UsersWithFeedbackOnly
        {
            get { return _usersWithFeedbackOnly; }
            private set { SetValue( ref _usersWithFeedbackOnly, value ); }
        }

        /// <summary>
        ///     All users view.
        /// </summary>
        public int AllUsersCount
        {
            get { return _allUsersCount; }
            private set { SetValue( ref _allUsersCount, value ); }
        }

        /// <summary>
        ///     Voted users view.
        /// </summary>
        public int VotedUsersCount
        {
            get { return _votedUsersCount; }
            private set { SetValue( ref _votedUsersCount, value ); }
        }

        /// <summary>
        ///     The view presenting users with both vote and feedback.
        /// </summary>
        public int UsersWithVoteAndFeedbackCount
        {
            get { return _usersWithVoteAndFeedbackCount; }
            private set { SetValue( ref _usersWithVoteAndFeedbackCount, value ); }
        }

        /// <summary>
        ///     The view presenting users with vote but without feedback.
        /// </summary>
        public int UsersWithVoteOnlyCount
        {
            get { return _usersWithVoteOnlyCount; }
            private set { SetValue( ref _usersWithVoteOnlyCount, value ); }
        }

        /// <summary>
        ///     The view presenting users with feedback but without vote.
        /// </summary>
        public int UsersWithFeedbackOnlyCount
        {
            get { return _usersWithFeedbackOnlyCount; }
            private set { SetValue( ref _usersWithFeedbackOnlyCount, value ); }
        }

        /// <summary>
        ///     Whether the administrators groups should be excluded from voting.
        /// </summary>
        public bool ExcludeAdministrators
        {
            get { return (bool) GetValue( ExcludeAdministratorsProperty ); }
            set { SetValue( ExcludeAdministratorsProperty, value ); }
        }

        /// <summary>
        ///     Whether the coordinators groups should be excluded from voting.
        /// </summary>
        public bool ExcludeCoordinators
        {
            get { return (bool) GetValue( ExcludeCoordinatorsProperty ); }
            set { SetValue( ExcludeCoordinatorsProperty, value ); }
        }

        /// <summary>
        ///     Whether the deleted messages should be excluded from processing.
        /// </summary>
        public bool ExcludeDeletedMessages
        {
            get { return (bool) GetValue( ExcludeDeletedMessagesProperty ); }
            set { SetValue( ExcludeDeletedMessagesProperty, value ); }
        }

        /// <summary>
        ///     Whether only the poll should be processed.
        /// </summary>
        public bool ParsePollOnly
        {
            get { return (bool) GetValue( ParsePollOnlyProperty ); }
            set { SetValue( ParsePollOnlyProperty, value ); }
        }

        /// <summary>
        ///     The URL of the topic to be processed.
        /// </summary>
        public string TopicUrl
        {
            get { return (string) GetValue( TopicUrlProperty ); }
            set { SetValue( TopicUrlProperty, value ); }
        }

        /// <summary>
        ///     Window title.
        /// </summary>
        public string ViewTitle => _viewTitle ?? (_viewTitle = $"ForumParser v.{GetType().Assembly.GetName().Version}");

        public string TemplateState
        {
            get { return _templateState; }
            set { SetValue( ref _templateState, value ); }
        }

        #endregion


        #region Events and invocation

        /// <summary>
        ///     Override this method to process unhandled exceptions thrown from executing task.
        /// </summary>
        /// <param name="exception">The exception thrown.</param>
        protected override void OnTaskFailed( Exception exception )
        {
            _viewProvider.ShowMessageBox( this, exception.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
        }

        /// <summary>
        ///     Called when the associated view has been just created.
        /// </summary>
        public void OnViewLoaded()
        {
            //  Do nothing
        }

        /// <summary>
        ///     Called when the associated view is about to close. Return false to disallow closing window.
        /// </summary>
        public bool OnViewClosing()
        {
            if ( IsBusy )
            {
                var cancelation = _viewProvider.ShowMessageBox( this, "Операция в процессе. Действительно завершить работу?", "Подтверждение выхода",
                                                                MessageBoxButton.OKCancel, MessageBoxImage.Warning );

                if ( cancelation != true )
                    return false;
            }

            CancelTask();
            return true;
        }

        #endregion


        #region Initialization

        public MainWindowViewModel( IViewProvider viewProvider,
                                    ForumTopicParser forumParser,
                                    ILogger uiLogger,
                                    SaveLoadManager saveLoadManager,
                                    TemplateMatcher templateMatcher )
            : base( uiLogger )
        {
            UiLogger = uiLogger;
            _saveLoadManager = saveLoadManager;
            _templateMatcher = templateMatcher;
            _viewProvider = viewProvider;
            _forumParser = forumParser;
            LoadForumTopicCommand = new AsyncDelegateCommand( LoadForumTopicCommandHandler );
            SaveIntermediateResultCommand = new DelegateCommand( SaveIntermediateResultCommandHandler );
            LoadIntermediateResultCommand = new DelegateCommand( LoadIntermediateResultCommandHandler );
            SetAllUsersMarksCommand = new DelegateCommand<int>( SetAllUsersMarksCommandHandler );
            EditTemplatesCommand = new DelegateCommand( EditTemplatesCommandHandler );
            SaveTemplatesCommand = new DelegateCommand( SaveTemplatesCommandHandler );
            SaveChartsCommand = new DelegateCommand<IEnumerable<BitmapSource>>( SaveChartsCommandHandler );
            LoadTemplatesCommand = new DelegateCommand( LoadTemplatesCommandHandler );
            CopyUsersWithMarksCommand = new DelegateCommand<IEnumerable>( CopyUsersWithMarksCommandHandler );
            CopyUsersWithoutMarksCommand = new DelegateCommand<IEnumerable>( CopyUsersWithoutMarksCommandHandler );
            DeleteSelectedUserCommand = new DelegateCommand( DeleteSelectedUserCommandHandler );
            SaveFinalResultCommand = new DelegateCommand( SaveFinalResultCommandHandler );
            UndoDeleteUserCommand = new DelegateCommand( UndoDeleteUserCommandHandler );
        }

        #endregion


        #region Non-public methods

        private void SaveChartsCommandHandler( IEnumerable<BitmapSource> chartImages )
        {
            ExecuteAndCatchExceptions( () =>
            {
                var directoryPath = _viewProvider.QueryDirectoryName( this, ChartsDirectory );
                if ( directoryPath == null )
                    return;

                ChartsDirectory = directoryPath;

                if ( !Directory.Exists( directoryPath ) )
                    Directory.CreateDirectory( directoryPath );

                var index = 0;
                var now = DateTime.Now.ToString( "dd-MM-yyyy" );
                foreach ( var chartImage in chartImages )
                {
                    var png = new PngBitmapEncoder { Frames = { BitmapFrame.Create( chartImage ) } };

                    using ( var stream = File.OpenWrite( Path.Combine( directoryPath, $"image_{now}_{++index}.png" ) ) )
                        png.Save( stream );
                }
            } );
        }

        private void LoadTemplatesPreview( ICollection<ChartTemplate> templates )
        {
            // Setup questions view models
            PreviewTemplates.Clear();

            var templateMatches = _templateMatcher.MatchTemplates( templates, ForumTopic.Poll.Questions );

            var fullMatches = 0;
            var partialMatches = 0;

            foreach ( var templateMatch in templateMatches )
            {
                if ( templateMatch.Key.Questions.Count == templateMatch.Value.Count )
                    fullMatches ++;
                else
                    partialMatches ++;

                var templateViewModel = new PreviewChartTemplateViewModel( templateMatch.Key, templateMatch.Value );
                PreviewTemplates.Add( templateViewModel );
            }

            TemplateState = $"Шаблоны загружены. " +
                            $"Точно совпало: {fullMatches}, " +
                            $"частично: {partialMatches}, " +
                            $"не совпало: {templates.Count - (fullMatches + partialMatches)}";
        }

        private void RefreshUsers()
        {
            AllUsers.Refresh();
            VotedUsers.Refresh();
            UsersWithVoteAndFeedback.Refresh();
            UsersWithVoteOnly.Refresh();
            UsersWithFeedbackOnly.Refresh();

            AllUsersCount = _users.Where( AllUsersFilter ).Count();
            VotedUsersCount = _users.Where( VotedUsersFilter ).Count();
            UsersWithVoteAndFeedbackCount = _users.Where( UsersWithVoteAndFeedbackFilter ).Count();
            UsersWithVoteOnlyCount = _users.Where( UsersWithVoteOnlyFilter ).Count();
            UsersWithFeedbackOnlyCount = _users.Where( UsersWithFeedbackOnlyFilter ).Count();
        }

        private void ExecuteAndCatchExceptions( Action action )
        {
            try
            {
                action();
            }
            catch ( OperationCanceledException )
            {
                UiLogger.Warning( "The task has been cancelled" );
            }
            catch ( Exception ex )
            {
                UiLogger.Error( $"Необработанное исключение: {ex.Message}", ex );
                OnTaskFailed( ex );
            }
        }

        private void WriteCsvFile( string filePath )
        {
            try
            {
                UiLogger.Info( $"Сохранение результатов в файл '{filePath}'" );

                var users = ForumTopic.Users.Where( user => !user.IsDeleted ).Select( user => $"{user.Name};{user.Mark}" );
                var content = "Ники;Баллы".AppendSequence( users ).JoinToString( "\r\n" );

                File.WriteAllText( filePath, content, new UTF8Encoding( true ) );
            }
            catch ( IOException ex )
            {
                UiLogger.Error( $"Ошибка при сохранении CSV-файла '{filePath}'", ex );
            }
        }

        private void WriteTxtFile( string topicName, string filePath )
        {
            try
            {
                UiLogger.Info( $"Сохранение результатов в файл '{filePath}'" );

                var users = ForumTopic.Users.Where( user => !user.IsDeleted ).Select( user => $"{user.Name} {user.Mark}" );
                var content = $"{topicName}\r\n{"Ники Баллы"}".AppendSequence( users ).JoinToString( "\r\n" );

                File.WriteAllText( filePath, content, new UTF8Encoding( true ) );
            }
            catch ( IOException ex )
            {
                UiLogger.Error( $"Ошибка при сохранении TXT-файла '{filePath}'", ex );
            }
        }

        private UserViewModel FindNextSelectedUser()
        {
            var isSelectedUserFound = false;
            var newSelectedUser = (UserViewModel) null;
            foreach ( var user in _users )
            {
                if ( user == SelectedUser )
                    isSelectedUserFound = true;
                else if ( !user.IsDeleted )
                {
                    newSelectedUser = user;
                    if ( isSelectedUserFound )
                        break;
                }
            }
            return newSelectedUser;
        }

        private void SetUsers( IEnumerable<User> users )
        {
            _users = new ObservableCollection<UserViewModel>( users.Where( user => !user.IsDeleted ).Select( user => new UserViewModel( user ) ) );
            RecreateFilters();
        }

        private void RecreateFilters()
        {
            AllUsers = CollectionViewSource.GetDefaultView( _users );
            AllUsers.Filter = AllUsersFilter;
            AllUsersCount = _users.Where( AllUsersFilter ).Count();

            VotedUsers = new CollectionViewSource { Source = _users }.View;
            VotedUsers.Filter = VotedUsersFilter;
            VotedUsersCount = _users.Where( VotedUsersFilter ).Count();

            UsersWithVoteAndFeedback = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteAndFeedback.Filter = UsersWithVoteAndFeedbackFilter;
            UsersWithVoteAndFeedbackCount = _users.Where( UsersWithVoteAndFeedbackFilter ).Count();

            UsersWithVoteOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteOnly.Filter = UsersWithVoteOnlyFilter;
            UsersWithVoteOnlyCount = _users.Where( UsersWithVoteOnlyFilter ).Count();

            UsersWithFeedbackOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithFeedbackOnly.Filter = UsersWithFeedbackOnlyFilter;
            UsersWithFeedbackOnlyCount = _users.Where( UsersWithFeedbackOnlyFilter ).Count();
        }

        private static bool UsersWithFeedbackOnlyFilter( object u )
        {
            var user = (UserViewModel) u;
            return !user.IsDeleted && user.HasFeedbackOnly;
        }

        private static bool UsersWithVoteOnlyFilter( object u )
        {
            var user = (UserViewModel) u;
            return !user.IsDeleted && user.HasVoteOnly;
        }

        private static bool UsersWithVoteAndFeedbackFilter( object u )
        {
            var user = (UserViewModel) u;
            return !user.IsDeleted && user.HasVoteAndFeedback;
        }

        private static bool VotedUsersFilter( object u )
        {
            var user = (UserViewModel) u;
            return !user.IsDeleted && user.HasVote;
        }

        private static bool AllUsersFilter( object u )
        {
            var user = (UserViewModel) u;
            return !user.IsDeleted;
        }

        #endregion


        #region Commands

        public ICommand CopyUsersWithMarksCommand { get; }
        public ICommand CopyUsersWithoutMarksCommand { get; }
        public ICommand DeleteSelectedUserCommand { get; }

        public ICommand EditTemplatesCommand { get; }

        public ICommand LoadForumTopicCommand { get; }
        public ICommand LoadIntermediateResultCommand { get; }
        public ICommand LoadTemplatesCommand { get; }
        public ICommand SaveChartsCommand { get; }
        public ICommand SaveFinalResultCommand { get; }
        public ICommand SaveIntermediateResultCommand { get; }
        public ICommand SaveTemplatesCommand { get; }
        public ICommand SetAllUsersMarksCommand { get; }

        public ICommand UndoDeleteUserCommand { get; }

        /// <summary>
        ///     Copies names of the passed users along with their marks to the clipboard.
        /// </summary>
        /// <param name="users">Users enumeration.</param>
        private void CopyUsersWithMarksCommandHandler( IEnumerable users )
        {
            ExecuteAndCatchExceptions( () =>
            {
                var content = users.OfType<UserViewModel>().Select( user => $"{user.Name}\t{user.Mark}" ).JoinToString( "\r\n" );
                Clipboard.SetText( content );
            } );
        }

        /// <summary>
        ///     Copies names of the passed users to the clipboard.
        /// </summary>
        /// <param name="users">Users enumeration.</param>
        private void CopyUsersWithoutMarksCommandHandler( IEnumerable users )
        {
            ExecuteAndCatchExceptions( () =>
            {
                var content = users.OfType<UserViewModel>().Select( user => user.Name ).JoinToString( "\r\n" );
                Clipboard.SetText( content );
            } );
        }

        /// <summary>
        ///     Deletes selected user.
        /// </summary>
        private void DeleteSelectedUserCommandHandler()
        {
            if ( SelectedUser == null )
                return;

            var userIndex = _users.IndexOf( SelectedUser );
            if ( userIndex == -1 )
            {
                UiLogger.Warning( $"Невозможно удалить пользователя {SelectedUser?.Name ?? "null"}." );
                return;
            }

            SelectedUser.IsDeleted = true;

            _deletedUsersStack.Push( new KeyValuePair<int, User>( userIndex, SelectedUser.User ) );
            SelectedUser = FindNextSelectedUser();
            _users.RemoveAt( userIndex );
        }

        /// <summary>
        ///     Displays charts template editor.
        /// </summary>
        private void EditTemplatesCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var dialogResult = _viewProvider.Show<TemplateEditorViewModel>( this, viewModel =>
                {
                    var templates = PreviewTemplates.Select( model => model.Template ).ToList();
                    viewModel.LoadEditor( templates, ForumTopic.Poll.Questions );
                } );
                if ( dialogResult.Result == true )
                    LoadTemplatesPreview( dialogResult.ViewModel.EditorTemplates.Select( editorTemplate => editorTemplate.Template ).ToList() );
            } );
        }

        /// <summary>
        ///     Performs login to forum.
        /// </summary>
        /// <returns></returns>
        private async Task LoadForumTopicCommandHandler()
        {
            var topicUrl = TopicUrl;

            if ( string.IsNullOrEmpty( topicUrl?.Trim() ) )
            {
                _viewProvider.ShowMessageBox( this, "Не задан URL темы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            //  Start from the first page
            //  TODO: Move to ForumParser class
            var pageNumberIndex = topicUrl.IndexOf( "page__st__", StringComparison.OrdinalIgnoreCase );
            if ( pageNumberIndex == -1 )
                pageNumberIndex = topicUrl.IndexOf( "page__mode__show__st__", StringComparison.OrdinalIgnoreCase );

            if ( pageNumberIndex > -1 )
            {
                topicUrl = topicUrl.Substring( 0, pageNumberIndex );
                UiLogger.Warning( $"URL темы указывает не на первую страницу. Разбор темы будет начат со страницы {Uri.UnescapeDataString( topicUrl )}" );
            }

            if ( SessionId == null )
            {
                var login = _viewProvider.Show<LoginHelperWindowViewModel>( this, loginHelper => loginHelper.InitialAddress = topicUrl );

                if ( login.Result != true )
                    return;

                SessionId = login.ViewModel.SessionId;
            }

            await ExecuteAsync( async () =>
            {
                try
                {
                    ForumTopic = await _forumParser.Parse(
                        topicUrl,
                        SessionId,
                        new ParserSettings(),
                        new ParseOptions
                        {
                            ExcludeCoordinators = ExcludeCoordinators,
                            ExcludeAdministrators = ExcludeAdministrators,
                            ParsePollOnly = ParsePollOnly,
                            ExcludeDeletedMessages = ExcludeDeletedMessages
                        },
                        logger: Logger,
                        cancellationToken: CancellationToken );
                }
                catch ( Exception ex )
                {
                    UiLogger.Error( ex.Message, ex );
                    _viewProvider.ShowMessageBox( this, ex.Message, "Ошибка загрузки темы", MessageBoxButton.OK, MessageBoxImage.Error );
                }
            }, ExecutionParameters.SuppressProgress );
        }

        /// <summary>
        ///     Loads intermediate results
        /// </summary>
        /// <returns></returns>
        private void LoadIntermediateResultCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var saveFile = _viewProvider.QueryOpenFileName( this, ForumTopicFile, "Parser intermediate results|*.wfp", "Укажите путь к файлу" );
                if ( saveFile == null )
                    return;

                try
                {
                    ForumTopicFile = saveFile;
                    ForumTopic = _saveLoadManager.LoadIntermediateResults( ForumTopicFile );
                    UiLogger.Info( $"Промежуточные результаты загружены из файла '{ForumTopicFile}'" );
                }
                catch ( ForumParserException ex )
                {
                    _viewProvider.ShowMessageBox( this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
                    UiLogger.Error( ex.Message, ex );
                }
            } );
        }

        /// <summary>
        ///     Displays charts template editor.
        /// </summary>
        private void LoadTemplatesCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var saveFile = _viewProvider.QueryOpenFileName( this, ChartTemplatesFile, "Parser chart templates|*.wct", "Укажите путь к файлу" );
                if ( saveFile == null )
                    return;

                try
                {
                    ChartTemplatesFile = saveFile;
                    LoadTemplatesPreview( _saveLoadManager.LoadChartTemplates( ChartTemplatesFile ) );
                    UiLogger.Info( $"Шаблоны графиков загружены из файла '{ChartTemplatesFile}'" );
                }
                catch ( ForumParserException ex )
                {
                    _viewProvider.ShowMessageBox( this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
                    UiLogger.Error( ex.Message, ex );
                }
            } );
        }

        /// <summary>
        ///     Saves final result in specified formats.
        /// </summary>
        private void SaveFinalResultCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                if ( !SaveTxt && !SaveCsv )
                {
                    _viewProvider.ShowMessageBox( this, "Не выбран ни один из форматов сохранения.", "Ошибка сохранения",
                                                  MessageBoxButton.OK, MessageBoxImage.Asterisk );
                    return;
                }

                //  Get files name
                var fileNameResult = _viewProvider.Show<StringInputDialogViewModel>( this, inputDialog =>
                {
                    inputDialog.ViewTitle = "Укажите имя сохраняемых файлов";
                    inputDialog.Description = "Укажите имя сохраняемых файлов (без расширения).\n" +
                                              "Существующие файлы будут заменены.\n" +
                                              "Недопустимые символы в имени (\\/*?:\"<>) будут заменены на _.";
                    inputDialog.StringInput = FixInvalidPathCharactersRegex.Replace( ForumTopic.Name, "_" );
                } );

                if ( fileNameResult.Result != true )
                    return;

                var directoryName = _viewProvider.QueryDirectoryName( this, FinalResultsDirectory );
                if ( directoryName == null )
                    return;

                FinalResultsDirectory = directoryName;

                var fileName = FixInvalidPathCharactersRegex.Replace( fileNameResult.ViewModel.StringInput, "_" );

                if ( SaveCsv )
                    WriteCsvFile( Path.Combine( FinalResultsDirectory, fileName + ".csv" ) );
                if ( SaveTxt )
                    WriteTxtFile( ForumTopic.Name, Path.Combine( FinalResultsDirectory, fileName + ".txt" ) );
            } );
        }

        /// <summary>
        ///     Saves intermediate result .
        /// </summary>
        private void SaveIntermediateResultCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var saveFile = _viewProvider.QuerySaveFileName( this, ForumTopicFile, "Parser intermediate results|*.wfp", "Укажите путь к файлу" );
                if ( saveFile == null )
                    return;

                try
                {
                    ForumTopicFile = saveFile;
                    _saveLoadManager.SaveIntermediateResults( ForumTopic, ForumTopicFile );
                    UiLogger.Info( $"Промежуточные результаты сохранены в файл '{ForumTopicFile}'" );
                }
                catch ( ForumParserException ex )
                {
                    _viewProvider.ShowMessageBox( this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
                    UiLogger.Error( ex.Message, ex );
                }
            } );
        }

        /// <summary>
        ///     Displays charts template editor.
        /// </summary>
        private void SaveTemplatesCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var saveFile = _viewProvider.QuerySaveFileName( this, ChartTemplatesFile, "Parser chart templates|*.wct", "Укажите путь к файлу" );
                if ( saveFile == null )
                    return;

                try
                {
                    ChartTemplatesFile = saveFile;
                    _saveLoadManager.SaveChartTemplates( PreviewTemplates.Select( templateVm => templateVm.Template ), ChartTemplatesFile );
                    UiLogger.Info( $"Шаблоны графиков сохранены в файл '{ChartTemplatesFile}'" );
                }
                catch ( ForumParserException ex )
                {
                    _viewProvider.ShowMessageBox( this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
                    UiLogger.Error( ex.Message, ex );
                }
            } );
        }

        /// <summary>
        ///     Sets the <paramref name="mark" /> value for all the users.
        /// </summary>
        /// <param name="mark"></param>
        private void SetAllUsersMarksCommandHandler( int mark )
        {
            foreach ( var user in _users )
                user.Mark = mark;
        }

        /// <summary>
        ///     Restores the last deleted user.
        /// </summary>
        private void UndoDeleteUserCommandHandler()
        {
            if ( _deletedUsersStack.Count == 0 )
                return;

            var deletedUser = _deletedUsersStack.Pop();
            _users.Insert( deletedUser.Key, SelectedUser = new UserViewModel( deletedUser.Value ) );
            SelectedUser.IsDeleted = false;

            RefreshUsers();
        }

        #endregion
    }
}
