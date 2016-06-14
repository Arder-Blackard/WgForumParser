using System;
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
using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels.Windows
{
    public class MainWindowViewModel : DependencyObjectViewModelBase, IViewModel
    {
        #region Constants

        public static readonly DependencyProperty TopicUrlProperty =
            DependencyProperty.Register( "TopicUrl", typeof( string ), typeof( MainWindowViewModel ), new PropertyMetadata( default(string) ) );

        public static readonly DependencyProperty ExcludeAdministratorsProperty = DependencyProperty.Register(
            "ExcludeAdministrators", typeof( bool ), typeof( MainWindowViewModel ), new PropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeCoordinatorsProperty = DependencyProperty.Register(
            "ExcludeCoordinators", typeof( bool ), typeof( MainWindowViewModel ), new PropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ExcludeDeletedMessagesProperty = DependencyProperty.Register(
            "ExcludeDeletedMessages", typeof( bool ), typeof( MainWindowViewModel ), new PropertyMetadata( default(bool) ) );

        public static readonly DependencyProperty ParsePollOnlyProperty = DependencyProperty.Register(
            "ParsePollOnly", typeof( bool ), typeof( MainWindowViewModel ), new PropertyMetadata( default(bool) ) );

        #endregion


        #region Fields

        private readonly ForumParser _forumParser;

        private readonly IViewProvider _viewProvider;
        private ICollectionView _allUsers;
        private ForumTopic _forumTopic;
        private ObservableCollection<UserViewModel> _users;
        private ICollectionView _usersWithFeedbackOnly;
        private ICollectionView _usersWithVoteAndFeedback;
        private ICollectionView _usersWithVoteOnly;
        private string _viewTitle;
        private ICollectionView _votedUsers;

        #endregion


        #region Auto-properties

        public string SessionId { get; set; } = "000000000040000138790088ad58e1f3863ad81f277e1bf9e48be2";

        public WpfLogger Logger { get; set; } = new WpfLogger();

        #endregion


        #region Properties

        public ForumTopic ForumTopic
        {
            get { return _forumTopic; }
            private set { SetValue( ref _forumTopic, value ); }
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


        #region Commands

        public ICommand EditTemplateCommand { get; }
        public ICommand LoadIntermediateResultCommand { get; }

        public ICommand LoginCommand { get; }
        public ICommand SaveIntermediateResultCommand { get; }
        public ICommand SetAllUsersMarksCommand { get; }

        private Task LoadIntermediateResultCommandHandler( object arg )
        {
            return Task.CompletedTask;
        }

        private async Task LoginCommandHandler( object o )
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

            SetUsers( ForumTopic.Users );
        }

        private Task SaveIntermediateResultCommandHandler( object arg )
        {
            return Task.CompletedTask;
        }

        private Task SetAllUsersMarksCommandHandler( object arg )
        {
            try
            {
                var mark = Convert.ToInt32( arg );
                foreach ( var user in _users )
                    user.Mark = mark;
            }
            catch
            {
                //  Do nothing
            }

            return Task.CompletedTask;
        }

        private Task EditTemplateCommandHandler( object arg )
        {
            try
            {
                _viewProvider.Show<TemplateEditorViewModel>( this, viewModel => viewModel.ForumTopic = ForumTopic );
            }
            catch
            {
                //  Do nothing
            }

            return Task.CompletedTask;
        }

        #endregion


        #region Initialization

        public MainWindowViewModel( IViewProvider viewProvider, ForumParser forumParser )
        {
            _viewProvider = viewProvider;
            _forumParser = forumParser;
            LoginCommand = new AsyncDelegateCommand( LoginCommandHandler );
            SaveIntermediateResultCommand = new AsyncDelegateCommand( SaveIntermediateResultCommandHandler );
            LoadIntermediateResultCommand = new AsyncDelegateCommand( LoadIntermediateResultCommandHandler );
            SetAllUsersMarksCommand = new AsyncDelegateCommand( SetAllUsersMarksCommandHandler );
            EditTemplateCommand = new AsyncDelegateCommand( EditTemplateCommandHandler );

            TopicUrl = "http://supertest.worldoftanks.com/index.php?/topic/8281-тестирование-карты-112-eiffeltower-стандартный-бой/";
        }

        #endregion


        #region Non-public methods

        private void SetUsers( IEnumerable<User> users )
        {
            _users = new ObservableCollection<UserViewModel>( users.Select( user => new UserViewModel( user ) ) );
            UpdateFilters();
        }

        private void UpdateFilters()
        {
            AllUsers = CollectionViewSource.GetDefaultView( _users );

            VotedUsers = new CollectionViewSource { Source = _users }.View;
            VotedUsers.Filter = u => (u as UserViewModel)?.HasVote == true;

            UsersWithVoteAndFeedback = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteAndFeedback.Filter = u => (u as UserViewModel)?.HasVoteAndFeedback == true;

            UsersWithFeedbackOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithFeedbackOnly.Filter = u => (u as UserViewModel)?.HasVoteOnly == true;

            UsersWithVoteOnly = new CollectionViewSource { Source = _users }.View;
            UsersWithVoteOnly.Filter = u => (u as UserViewModel)?.HasFeedbackOnly == true;
        }

        #endregion
    }
}
