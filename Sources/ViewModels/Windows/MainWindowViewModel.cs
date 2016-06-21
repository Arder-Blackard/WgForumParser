using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ForumParserWPF.Models;
using ForumParserWPF.Services;
using WpfCommon.Commands;
using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels.Windows
{
    public class MainWindowViewModel : DependencyObjectViewModelBase, IViewModel
    {
        #region Constants

        public static readonly DependencyProperty TopicUrlProperty =
            DependencyProperty.Register( "TopicUrl", typeof (string), typeof (MainWindowViewModel), new PropertyMetadata( default(string) ) );

        public static readonly DependencyProperty ExcludeAdministratorsProperty = DependencyProperty.Register(
            "ExcludeAdministrators", typeof (bool), typeof (MainWindowViewModel), new PropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeCoordinatorsProperty = DependencyProperty.Register(
            "ExcludeCoordinators", typeof (bool), typeof (MainWindowViewModel), new PropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeDeletedMessagesProperty = DependencyProperty.Register(
            "ExcludeDeletedMessages", typeof (bool), typeof (MainWindowViewModel), new PropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ParsePollOnlyProperty = DependencyProperty.Register(
            "ParsePollOnly", typeof (bool), typeof (MainWindowViewModel), new PropertyMetadata( default(bool) ) );

        #endregion


        #region Fields

        private readonly ForumParser _forumParser;

        private readonly IViewProvider _viewProvider;
        private ICollectionView _allUsers;
        private ForumTopic _forumTopic;
        private UserViewModel _selectedUser;
        private ObservableCollection<UserViewModel> _users;
        private ICollectionView _usersWithFeedbackOnly;
        private ICollectionView _usersWithVoteAndFeedback;
        private ICollectionView _usersWithVoteOnly;
        private string _viewTitle;
        private ICollectionView _votedUsers;

        #endregion


        #region Auto-properties

        public string SessionId { get; set; } 

        public WpfLogger Logger { get; set; } = new WpfLogger();

        #endregion


        #region Properties

        public UserViewModel SelectedUser
        {
            get { return _selectedUser; }
            set { SetValue( ref _selectedUser, value ); }
        }

        public ForumTopic ForumTopic
        {
            get { return _forumTopic; }
            private set
            {
                if ( SetValue( ref _forumTopic, value ) )
                    SetUsers( _forumTopic.Users );
            }
        }

        public ICollectionView AllUsers
        {
            get { return _allUsers; }
            private set { SetValue( ref _allUsers, value ); }
        }

        public ICollectionView VotedUsers
        {
            get { return _votedUsers; }
            private set { SetValue( ref _votedUsers, value ); }
        }

        public ICollectionView UsersWithVoteAndFeedback
        {
            get { return _usersWithVoteAndFeedback; }
            private set { SetValue( ref _usersWithVoteAndFeedback, value ); }
        }

        public ICollectionView UsersWithFeedbackOnly
        {
            get { return _usersWithFeedbackOnly; }
            private set { SetValue( ref _usersWithFeedbackOnly, value ); }
        }

        public ICollectionView UsersWithVoteOnly
        {
            get { return _usersWithVoteOnly; }
            private set { SetValue( ref _usersWithVoteOnly, value ); }
        }

        public bool ExcludeAdministrators
        {
            get { return (bool) GetValue( ExcludeAdministratorsProperty ); }
            set { SetValue( ExcludeAdministratorsProperty, value ); }
        }

        public bool ExcludeCoordinators
        {
            get { return (bool) GetValue( ExcludeCoordinatorsProperty ); }
            set { SetValue( ExcludeCoordinatorsProperty, value ); }
        }

        public bool ExcludeDeletedMessages
        {
            get { return (bool) GetValue( ExcludeDeletedMessagesProperty ); }
            set { SetValue( ExcludeDeletedMessagesProperty, value ); }
        }

        public bool ParsePollOnly
        {
            get { return (bool) GetValue( ParsePollOnlyProperty ); }
            set { SetValue( ParsePollOnlyProperty, value ); }
        }

        public string TopicUrl
        {
            get { return (string) GetValue( TopicUrlProperty ); }
            set { SetValue( TopicUrlProperty, value ); }
        }

        public string ViewTitle => _viewTitle ?? (_viewTitle = $"ForumParser v.{GetType().Assembly.GetName().Version}");

        #endregion


        #region Initialization

        public MainWindowViewModel( IViewProvider viewProvider, ForumParser forumParser )
        {
            _viewProvider = viewProvider;
            _forumParser = forumParser;
            LoginCommand = new AsyncDelegateCommand( LoginCommandHandler );
            SaveIntermediateResultCommand = new DelegateCommand( SaveIntermediateResultCommandHandler );
            LoadIntermediateResultCommand = new AsyncDelegateCommand( LoadIntermediateResultCommandHandler );
            SetAllUsersMarksCommand = new DelegateCommand<int>( SetAllUsersMarksCommandHandler );
            EditTemplateCommand = new DelegateCommand( EditTemplateCommandHandler );
            CopyTableContentCommand = new DelegateCommand<IEnumerable>( CopyTableContentCommandHandler );
            DeleteSelectedUserCommand = new DelegateCommand( DeleteSelectedUserCommandHandler );

            TopicUrl = "http://supertest.worldoftanks.com/index.php?/topic/8281-тестирование-карты-112-eiffeltower-стандартный-бой/";
        }

        #endregion


        #region Non-public methods

        private void DeleteSelectedUserCommandHandler()
        {
            if ( SelectedUser == null )
                return;

            SelectedUser.IsDeleted = true;
            SelectedUser = FindNextSelectedUser();

            AllUsers.Refresh();
            VotedUsers.Refresh();
            UsersWithVoteAndFeedback.Refresh();
            UsersWithVoteOnly.Refresh();
            UsersWithFeedbackOnly.Refresh();
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

        private void UpdateUsers()
        {
        }

        private void CopyTableContentCommandHandler( IEnumerable users )
        {
            var content = users.OfType<UserViewModel>()
                               .Select( user => $"{user.Name}\t{user.Mark}\r\n" );

            Clipboard.SetText( string.Concat( content ) );
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


        #region Commands

        public ICommand EditTemplateCommand { get; }
        public ICommand LoadIntermediateResultCommand { get; }

        public ICommand LoginCommand { get; }
        public ICommand SaveIntermediateResultCommand { get; }
        public ICommand SetAllUsersMarksCommand { get; }
        public ICommand CopyTableContentCommand { get; }
        public ICommand DeleteSelectedUserCommand { get; }

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
        }

        private void SaveIntermediateResultCommandHandler()
        {
        }

        private void SetAllUsersMarksCommandHandler( int mark )
        {
            foreach ( var user in _users )
                user.Mark = mark;
        }

        #endregion
    }
}
