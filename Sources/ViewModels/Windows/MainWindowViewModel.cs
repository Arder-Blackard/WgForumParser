using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommonLib.Extensions;
using CommonLib.Logging;
using ForumParser.Exceptions;
using ForumParser.Models;
using ForumParser.Services;
using Newtonsoft.Json;
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
            "TopicUrl", typeof ( string ), typeof ( MainWindowViewModel ), new FrameworkPropertyMetadata( default(string) ) );

        public static readonly DependencyProperty ExcludeAdministratorsProperty = DependencyProperty.Register(
            "ExcludeAdministrators", typeof ( bool ), typeof ( MainWindowViewModel ), new FrameworkPropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeCoordinatorsProperty = DependencyProperty.Register(
            "ExcludeCoordinators", typeof ( bool ), typeof ( MainWindowViewModel ), new FrameworkPropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeDeletedMessagesProperty = DependencyProperty.Register(
            "ExcludeDeletedMessages", typeof ( bool ), typeof ( MainWindowViewModel ), new FrameworkPropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ParsePollOnlyProperty = DependencyProperty.Register(
            "ParsePollOnly", typeof ( bool ), typeof ( MainWindowViewModel ), new FrameworkPropertyMetadata( default(bool) ) );

        private static readonly Regex FixInvalidPathCharactersRegex = new Regex( @"[\\/\*\?\:""<>]" );

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All
        };

        #endregion


        #region Fields

        //  Services
        private readonly ForumTopicParser _forumParser;
        private readonly IViewProvider _viewProvider;

        private readonly Stack<KeyValuePair<int, User>> _deletedUsersStack = new Stack<KeyValuePair<int, User>>();

        //  Data
        private ForumTopic _forumTopic;
        private string _viewTitle;
        private UserViewModel _selectedUser;
        private ObservableCollection<UserViewModel> _users;
        private ICollectionView _allUsers;
        private ICollectionView _usersWithFeedbackOnly;
        private ICollectionView _usersWithVoteAndFeedback;
        private ICollectionView _usersWithVoteOnly;
        private ICollectionView _votedUsers;
        private int _usersWithFeedbackOnlyCount;
        private int _allUsersCount;
        private int _usersWithVoteAndFeedbackCount;
        private int _usersWithVoteOnlyCount;
        private int _votedUsersCount;

        #endregion


        #region Auto-properties

        /// <summary>
        ///     Stores 'frm_session_id' cookie value.
        /// </summary>
        public string SessionId { get; set; } = "000000000040000138790034aac9329c4e60c04b05d3fb2235cdd7";

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
                    SetUsers( _forumTopic.Users );
            }
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

        #endregion


        #region Commands

        public ICommand CopyUsersWithMarksCommand { get; }
        public ICommand CopyUsersWithoutMarksCommand { get; }
        public ICommand DeleteSelectedUserCommand { get; }

        public ICommand EditTemplateCommand { get; }

        public ICommand LoadForumTopicCommand { get; }
        public ICommand LoadIntermediateResultCommand { get; }
        public ICommand SaveFinalResultCommand { get; }
        public ICommand SaveIntermediateResultCommand { get; }
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
        private void EditTemplateCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var dialogResult = _viewProvider.Show<TemplateEditorViewModel>( this, viewModel => viewModel.ForumTopic = ForumTopic );

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
            var pageNumberIndex = topicUrl.IndexOf( "page__st__", StringComparison.OrdinalIgnoreCase );
            if ( pageNumberIndex == -1 )
                pageNumberIndex = topicUrl.IndexOf( "page__mode__show__st__", StringComparison.OrdinalIgnoreCase );

            if ( pageNumberIndex > -1 )
            {
                topicUrl = topicUrl.Substring( 0, pageNumberIndex );
                UiLogger.Warning( $"URL темы указывает не на первую страницу. Разбор темы будет начат со страницы {topicUrl}" );
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
                var saveFile = _viewProvider.ShowDialog<OpenFileViewModel>( this, viewModel =>
                {
                    viewModel.FileName = ForumTopicFile;
                    viewModel.Filter = "Parser intermediate results|*.wfp";
                    viewModel.Title = "Укажите путь к файлу";
                } );

                if ( saveFile.Result != true )
                    return;

                ForumTopicFile = saveFile.ViewModel.FileName;

                try
                {
                    ForumTopic = LoadForumTopicFromFile( ForumTopicFile );
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
                    inputDialog.Description =
                        "Укажите имя сохраняемых файлов (без расширения).\nСуществующие файлы будут заменены.\nНедопустимые символы в имени (\\/*?:\"<>) будут заменены на _.";
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

        private void SaveIntermediateResultCommandHandler()
        {
            ExecuteAndCatchExceptions( () =>
            {
                var saveFile = _viewProvider.ShowDialog<SaveFileViewModel>( this, viewModel =>
                {
                    viewModel.FileName = ForumTopicFile;
                    viewModel.Filter = "Parser intermediate results|*.wfp";
                    viewModel.Title = "Укажите путь к файлу";
                    viewModel.CheckFileExists = false;
                } );

                if ( saveFile.Result != true )
                    return;

                ForumTopicFile = saveFile.ViewModel.FileName;
                var json = JsonConvert.SerializeObject( ForumTopic, Formatting.Indented, JsonSerializerSettings );

                if ( File.Exists( ForumTopicFile ) )
                    File.Delete( ForumTopicFile );

                using ( var file = ZipFile.Open( ForumTopicFile, ZipArchiveMode.Create ) )
                using ( var stream = file.CreateEntry( "topic.json" ).Open() )
                using ( var writer = new StreamWriter( stream ) )
                    writer.Write( json );

                UiLogger.Info( $"Промежуточные результаты сохранены в файл '{ForumTopicFile}'" );
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

        public MainWindowViewModel( IViewProvider viewProvider, ForumTopicParser forumParser, ILogger uiLogger ) : base( uiLogger )
        {
            UiLogger = uiLogger;
            _viewProvider = viewProvider;
            _forumParser = forumParser;
            LoadForumTopicCommand = new AsyncDelegateCommand( LoadForumTopicCommandHandler );
            SaveIntermediateResultCommand = new DelegateCommand( SaveIntermediateResultCommandHandler );
            LoadIntermediateResultCommand = new DelegateCommand( LoadIntermediateResultCommandHandler );
            SetAllUsersMarksCommand = new DelegateCommand<int>( SetAllUsersMarksCommandHandler );
            EditTemplateCommand = new DelegateCommand( EditTemplateCommandHandler );
            CopyUsersWithMarksCommand = new DelegateCommand<IEnumerable>( CopyUsersWithMarksCommandHandler );
            CopyUsersWithoutMarksCommand = new DelegateCommand<IEnumerable>( CopyUsersWithoutMarksCommandHandler );
            DeleteSelectedUserCommand = new DelegateCommand( DeleteSelectedUserCommandHandler );
            SaveFinalResultCommand = new DelegateCommand( SaveFinalResultCommandHandler );
            UndoDeleteUserCommand = new DelegateCommand( UndoDeleteUserCommandHandler );
        }

        #endregion


        #region Non-public methods

        private ForumTopic LoadForumTopicFromFile( string fileName )
        {
            try
            {
                using ( var zipArchive = ZipFile.Open( fileName, ZipArchiveMode.Read ) )
                using ( var stream = zipArchive.GetEntry( "topic.json" )?.Open() )
                {
                    if ( stream == null )
                        throw new IOException( "Неверный формат сохраненных данных." );

                    using ( var reader = new StreamReader( stream ) )
                    {
                        var json = reader.ReadToEnd();
                        try
                        {
                            return JsonConvert.DeserializeObject<ForumTopic>( json, JsonSerializerSettings );
                        }
                        catch ( Exception ex )
                        {
                            throw new InvalidDataException( "Ошибка при чтении 'topic.json'.", ex );
                        }
                    }
                }
            }
            catch ( FileNotFoundException ex )
            {
                throw new ForumParserException( $"Файл не найден.", ex );
            }
            catch ( IOException ex )
            {
                throw new ForumParserException( $"Ошибка при загрузке файла.", ex );
            }
            catch ( InvalidDataException ex )
            {
                throw new ForumParserException( $"Неверный формат данных.", ex );
            }
            catch ( Exception ex )
            {
                throw new ForumParserException( $"Ошибка при загрузке: ", ex );
            }
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
            UsersWithVoteAndFeedbackCount = _users.Where(UsersWithVoteAndFeedbackFilter).Count();

            UsersWithVoteOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteOnly.Filter = UsersWithVoteOnlyFilter;
            UsersWithVoteOnlyCount = _users.Where(UsersWithVoteOnlyFilter).Count();

            UsersWithFeedbackOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithFeedbackOnly.Filter = UsersWithFeedbackOnlyFilter;
            UsersWithFeedbackOnlyCount = _users.Where(UsersWithFeedbackOnlyFilter).Count();
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
    }
}
