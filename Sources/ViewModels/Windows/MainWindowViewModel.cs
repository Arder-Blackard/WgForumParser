﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommonLib.Extensions;
using CommonLib.Logging;
using ForumParserWPF.Models;
using ForumParserWPF.Services;
using WpfCommon.Commands;
using WpfCommon.Services;
using WpfCommon.ViewModels.Base;
using WpfCommon.ViewModels.Dialogs;

namespace ForumParserWPF.ViewModels.Windows
{
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

        #endregion


        #region Fields

        //  Services
        private readonly ForumParser _forumParser;
        private readonly IViewProvider _viewProvider;

        //  Data
        private ForumTopic _forumTopic;
        private ICollectionView _allUsers;
        private string _viewTitle;
        private UserViewModel _selectedUser;
        private ObservableCollection<UserViewModel> _users;
        private ICollectionView _usersWithFeedbackOnly;
        private ICollectionView _usersWithVoteAndFeedback;
        private ICollectionView _usersWithVoteOnly;
        private ICollectionView _votedUsers;

        private Stack<User> _deletedUsersStack = new Stack<User>();

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

        public ILogger UiLogger { get; } 

        public string FinalResultsDirectory { get; set; }

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
        public ICommand LoadIntermediateResultCommand { get; }

        public ICommand LoginCommand { get; }
        public ICommand SaveFinalResultCommand { get; }
        public ICommand SaveIntermediateResultCommand { get; }
        public ICommand SetAllUsersMarksCommand { get; }

        public ICommand UndoDeleteUserCommand { get; }

        /// <summary>
        ///     Copies the content of the passed users along with their marks to clipboard.
        /// </summary>
        /// <param name="users">Users enumeration.</param>
        private void CopyUsersWithMarksCommandHandler( IEnumerable users )
        {
            Execute( () =>
            {
                var content = users.OfType<UserViewModel>().Select( user => $"{user.Name}\t{user.Mark}" ).JoinToString( "\r\n" );
                Clipboard.SetText( content );
            } );
        }

        private void CopyUsersWithoutMarksCommandHandler( IEnumerable users )
        {
            Execute( () =>
            {
                var content = users.OfType<UserViewModel>().Select( user => user.Name ).JoinToString( "\r\n" );
                Clipboard.SetText( content );
            } );
        }

        private void DeleteSelectedUserCommandHandler()
        {
            if ( SelectedUser == null )
                return;

            SelectedUser.IsDeleted = true;
            _deletedUsersStack.Push( SelectedUser.User );
            SelectedUser = FindNextSelectedUser();

            RefreshUsers();
        }

        private void RefreshUsers()
        {
            AllUsers.Refresh();
            VotedUsers.Refresh();
            UsersWithVoteAndFeedback.Refresh();
            UsersWithVoteOnly.Refresh();
            UsersWithFeedbackOnly.Refresh();
        }

        private void EditTemplateCommandHandler()
        {
            try
            {
                _viewProvider.Show<TemplateEditorViewModel>( this, viewModel => viewModel.ForumTopic = ForumTopic );
            }
            catch
            {
                //  Do nothing
            }
        }

        private Task LoadIntermediateResultCommandHandler()
        {
            return Task.CompletedTask;
        }

        private async Task LoginCommandHandler()
        {
            if ( SessionId == null )
            {
                var login = _viewProvider.Show<LoginHelperWindowViewModel>( this );
                if ( login.Result != true )
                    return;

                SessionId = login.ViewModel.SessionId;
            }

            await ExecuteAsync( async () =>
            {
                ForumTopic = await _forumParser.Parse(
                    TopicUrl,
                    SessionId,
                    new ParserSettings(),
                    new ParseOptions
                    {
                        SkipCoordinators = true,
                        SkipAdministrators = true,
                        PollOnly = false,
                        SkipDeleted = true
                    },
                    logger: Logger );
            }, ExecutionParameters.SuppressProgress );
        }

        /// <summary>
        ///     Saves final result in specified formats.
        /// </summary>
        private void SaveFinalResultCommandHandler()
        {
            Execute( () =>
            {
                if ( !SaveTxt && !SaveTxt )
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

        private void SetAllUsersMarksCommandHandler( int mark )
        {
            foreach ( var user in _users )
                user.Mark = mark;
        }

        private void SaveIntermediateResultCommandHandler()
        {
            
        }

        private void UndoDeleteUserCommandHandler()
        {
            if ( _deletedUsersStack.Count == 0 )
                return;

            _deletedUsersStack.Pop().IsDeleted = false;
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

        public MainWindowViewModel( IViewProvider viewProvider, ForumParser forumParser, ILogger logger ) : base( logger )
        {
            UiLogger = logger;
            _viewProvider = viewProvider;
            _forumParser = forumParser;
            LoginCommand = new AsyncDelegateCommand( LoginCommandHandler );
            SaveIntermediateResultCommand = new DelegateCommand( SaveIntermediateResultCommandHandler );
            LoadIntermediateResultCommand = new AsyncDelegateCommand( LoadIntermediateResultCommandHandler );
            SetAllUsersMarksCommand = new DelegateCommand<int>( SetAllUsersMarksCommandHandler );
            EditTemplateCommand = new DelegateCommand( EditTemplateCommandHandler );
            CopyUsersWithMarksCommand = new DelegateCommand<IEnumerable>( CopyUsersWithMarksCommandHandler );
            CopyUsersWithoutMarksCommand = new DelegateCommand<IEnumerable>( CopyUsersWithoutMarksCommandHandler );
            DeleteSelectedUserCommand = new DelegateCommand( DeleteSelectedUserCommandHandler );
            SaveFinalResultCommand = new DelegateCommand( SaveFinalResultCommandHandler );
            UndoDeleteUserCommand = new DelegateCommand( UndoDeleteUserCommandHandler );

            TopicUrl = "http://supertest.worldoftanks.com/index.php?/topic/8281-тестирование-карты-112-eiffeltower-стандартный-бой/";
        }

        #endregion


        #region Non-public methods

        private void Execute( Action action )
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
                UiLogger.Error( "Unhandled exception", ex );
                OnTaskFailed( ex );
            }
        }

        private void WriteCsvFile( string filePath )
        {
            try
            {
                Logger.Info( $"Сохранение результатов в файл '{filePath}'" );
                var content = ForumTopic.Users.Where( user => !user.IsDeleted ).Select( user => $"{user.Name};{user.Mark}" ).JoinToString( "\r\n" );
                File.WriteAllText( filePath, content );
            }
            catch ( IOException ex )
            {
                Logger.Error( $"Ошибка при сохранении CSV-файла '{filePath}'", ex );
            }
        }

        private void WriteTxtFile( string topicName, string filePath )
        {
            try
            {
                Logger.Info( $"Сохранение результатов в файл '{filePath}'" );
                var content = topicName.AppendSequence( ForumTopic.Users.Where( user => !user.IsDeleted ).Select( user => $"{user.Name} {user.Mark}" ) )
                                       .JoinToString( "\r\n" );
                File.WriteAllText( filePath, content );
            }
            catch ( IOException ex )
            {
                Logger.Error( $"Ошибка при сохранении TXT-файла '{filePath}'", ex );
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
            _users = new ObservableCollection<UserViewModel>( users.Select( user => new UserViewModel( user ) ) );
            RecreateFilters();
        }

        private void RecreateFilters()
        {
            AllUsers = CollectionViewSource.GetDefaultView( _users );
            AllUsers.Filter = u =>
            {
                var user = (UserViewModel) u;
                return !user.IsDeleted;
            };

            VotedUsers = new CollectionViewSource { Source = _users }.View;
            VotedUsers.Filter = u =>
            {
                var user = (UserViewModel) u;
                return !user.IsDeleted && user.HasVote;
            };

            UsersWithVoteAndFeedback = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteAndFeedback.Filter = u =>
            {
                var user = (UserViewModel) u;
                return !user.IsDeleted && user.HasVoteAndFeedback;
            };

            UsersWithVoteOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteOnly.Filter = u =>
            {
                var user = (UserViewModel) u;
                return !user.IsDeleted && user.HasVoteOnly;
            };

            UsersWithFeedbackOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithFeedbackOnly.Filter = u =>
            {
                var user = (UserViewModel) u;
                return !user.IsDeleted && user.HasFeedbackOnly;
            };
        }

        #endregion
    }
}
