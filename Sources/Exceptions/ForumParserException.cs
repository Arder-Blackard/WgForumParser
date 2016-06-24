using System;
using System.Runtime.Serialization;

namespace ForumParserWPF.Exceptions
{
    [Serializable]
    public class ForumParserException : Exception
    {
        #region Initialization

        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ForumParserException()
        {
        }

        public ForumParserException( string message ) : base( message )
        {
        }

        public ForumParserException( string message, Exception inner ) : base( message, inner )
        {
        }

        protected ForumParserException(
            SerializationInfo info,
            StreamingContext context ) : base( info, context )
        {
        }

        #endregion
    }
}
