namespace ForumParserWPF.Models
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

        #endregion


        #region Public methods

        public override string ToString() => Name;

        #endregion
    }
}
