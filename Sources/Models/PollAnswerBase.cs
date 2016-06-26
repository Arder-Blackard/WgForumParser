using System.Collections.Generic;

namespace ForumParser.Models
{
    public abstract class PollAnswerBase
    {
        #region Auto-properties

        public string Text { get; set; } 

        #endregion


        #region Properties

        public abstract ICollection<User> Users { get; }
        public abstract int Count { get; set; }

        #endregion


        #region Initialization

        protected PollAnswerBase( string text )
        {
            Text = text;
        }

        #endregion


        #region Public methods

        public override string ToString() => Text;

        #endregion
    }
}
