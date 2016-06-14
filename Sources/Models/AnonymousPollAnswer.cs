using System.Collections.Generic;

namespace ForumParserWPF.Models
{
    public class AnonymousPollAnswer : PollAnswerBase
    {
        private static readonly ICollection<User> EmptyUsersCollection = new User[0];

        public override int Count { get; set; }
        public override ICollection<User> Users => EmptyUsersCollection;

        public AnonymousPollAnswer( string text, int count ) : base(text)
        {
            Count = count;
        }
    }
}