﻿using ForumParserWPF.Models;
using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels
{
    public class UserViewModel : SimpleViewModelBase
    {
        #region Auto-properties

        public User User { get; }

        #endregion


        #region Properties

        public string Name => User.Name;

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