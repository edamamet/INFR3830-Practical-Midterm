using System;
namespace Hub.Hooks {
    public readonly struct Message {
        /// <summary>
        /// This will be provided to you by the server. The server will not accept your message if you don't use the one it gave you.
        /// </summary>
        public readonly MessageType Header;
        public readonly Guid SenderId;
        public readonly DateTime Timestamp;
        public readonly byte[] Content;

        // you might be asking why we use a byte array instead of a generic type, along with a MessageType instead of the generic type
        // this is because there is no way to prevent the end user from deserializing the message into a different type than what it was.
        // there are also boxing issues with running generics in a switch expression, and i'd rather not deal with unnecessary heap allocations, so
        // the best solution is to provide a static utility class that acts as a factory + visitor to create, serialize, and deserialize messages.
        // that uses the MessageType (which is a known value) to determine the type of the message. for brevity, though, i had to make a few 
        // allocations for new byte arrays. this is a tradeoff i'm willing to make for the sake of simplicity and readability, and to avoid the user
        // from having to do more than they have to.

        // Prevent anyone from creating a message without using the factory provided by MessageUtils
        internal Message(MessageType header, Guid senderId, DateTime timestamp, byte[] content) {
            Header = header;
            SenderId = senderId;
            Timestamp = timestamp;
            Content = content;
        }

        public override string ToString() {
            return $"[{Timestamp}] [{Header}] <{SenderId}>";
        }
    }
    public enum MessageType : byte {
        Registration,
        Text,
        Position,
        Error,
    }
}