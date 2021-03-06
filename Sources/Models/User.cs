﻿using System.Collections.Generic;

namespace ForumParser.Models
{
    public class User
    {
        #region Auto-properties

        public string Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; } = string.Empty;
        public bool HasVote { get; set; }
        public bool HasFeedback { get; set; }
        public bool HasDeletedFeedback { get; set; }
        public bool HasDeletedPost { get; set; }
        public int Mark { get; set; }
        public bool IsDeleted { get; set; }
        public string FeedbackUrl { get; set; }
        public IList<Message> Messages { get; set; } = new List<Message>();

        #endregion


        #region Public methods

        public override string ToString() => $"{Name} ({Messages?.Count ?? 0})";

        #endregion
    }

    public class Message
    {
        #region Auto-properties

        public string Html { get; set; }
        public bool IsDeleted { get; set; }

        #endregion
    }
}
