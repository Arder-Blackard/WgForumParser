using System.Collections.Generic;

namespace ForumParserWPF.Models
{
    public class PollAnswer : PollAnswerBase
    {
        #region Auto-properties

        public override ICollection<User> Users { get; }

        #endregion


        #region Properties

        public override int Count
        {
            get { return Users?.Count ?? 0; }
            set { }
        }

        #endregion


        #region Initialization

        public PollAnswer( string text, ICollection<User> users ) : base( text )
        {
            Users = users;
        }

        #endregion
    }
}
