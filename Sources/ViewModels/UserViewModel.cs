using ForumParser.Models;
using ForumParser.Utils;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels
{
    public class UserViewModel : SimpleViewModelBase
    {
        #region Fields

        private string _cachedMessage;

        #endregion


        #region Auto-properties

        public User User { get; }

        #endregion


        #region Properties

        public string Name => User.Name;

        public string Group => User.Group;

        public string Message
        {
            get
            {
                if ( _cachedMessage == null )
                    _cachedMessage = HtmlFormatter.FormatUserMessages( User.Messages );

                return _cachedMessage;
            }
        }

        public int Mark
        {
            get { return User.Mark; }
            set
            {
                if ( User.Mark == value )
                    return;
                User.Mark = value;
                OnPropertyChanged();
            }
        }

        public bool IsDeleted
        {
            get { return User.IsDeleted; }
            set
            {
                if ( User.IsDeleted == value )
                    return;
                User.IsDeleted = value;
                OnPropertyChanged();
            }
        }

        public bool HasVote => User.HasVote;
        public bool HasVoteAndFeedback => User.HasVote && User.HasFeedback;
        public bool HasVoteOnly => User.HasVote && !User.HasFeedback;
        public bool HasFeedbackOnly => !User.HasVote && User.HasFeedback;

        #endregion


        #region Initialization

        public UserViewModel( User user )
        {
            User = user;
        }

        #endregion
    }
}
