using System.Collections.Generic;

namespace ForumParser.Models
{
    public class Poll
    {
        #region Auto-properties

        public IList<PollQuestion> Questions { get; set; } = new List<PollQuestion>();

        #endregion
    }
}