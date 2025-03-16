using System;
using System.Text;
namespace Hub.Hooks {
    public static class MessageUtils {
        public static Message CreateText(Guid senderId, string content) {
            return new Message(MessageType.Text, senderId, DateTime.UtcNow, SerializeText(content));
        }
        public static Message CreateRegistration(Guid senderId, Guid content) =>
            new Message(MessageType.Registration, senderId, DateTime.UtcNow, SerializeGuid(content));
        public static Message CreatePosition(Guid senderId, float x, float y, float z) =>
            new Message(MessageType.Position, senderId, DateTime.UtcNow, SerializePosition(x, y, z));
        
        static byte[] SerializeText(string content) {
            var bytes = Encoding.UTF8.GetBytes(content);
            return bytes;
        }
        public static string DeserializeText(this byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }
        static byte[] SerializeGuid(Guid guid) {
            return guid.ToByteArray();
        }
        public static Guid DeserializeGuid(this byte[] bytes) {
            return new Guid(bytes);
        }
        static byte[] SerializePosition(float x, float y, float z) {
            var xBytes = BitConverter.GetBytes(x);
            var yBytes = BitConverter.GetBytes(y);
            var zBytes = BitConverter.GetBytes(z);
            
            var byteLength = xBytes.Length + yBytes.Length + zBytes.Length;
            var result = new byte[byteLength];
            
            Buffer.BlockCopy(xBytes, 0, result, 0, xBytes.Length);
            Buffer.BlockCopy(yBytes, 0, result, xBytes.Length, yBytes.Length);
            Buffer.BlockCopy(zBytes, 0, result, xBytes.Length + yBytes.Length, zBytes.Length);
            
            return result;
        }
        public static (float, float, float) DeserializePosition(this byte[] bytes) {
            var x = BitConverter.ToSingle(bytes, 0);
            var y = BitConverter.ToSingle(bytes, 4);
            var z = BitConverter.ToSingle(bytes, 8);
            return (x, y, z);
        }
        
        public static byte[] SerializeMessage(this Message message) {
            var headerByte = (byte)message.Header;
            var senderIdBytes = message.SenderId.ToByteArray();
            var timestampBytes = BitConverter.GetBytes(message.Timestamp.Ticks);
            var contentBytes = message.Content;

            var byteLength = 1 + senderIdBytes.Length + timestampBytes.Length + contentBytes.Length; // 1 byte for the header
            var result = new byte[byteLength];

            result[0] = headerByte;
            Buffer.BlockCopy(senderIdBytes, 0, result, 1, senderIdBytes.Length);
            Buffer.BlockCopy(timestampBytes, 0, result, 1 + senderIdBytes.Length, timestampBytes.Length);
            Buffer.BlockCopy(contentBytes, 0, result, 1 + senderIdBytes.Length + timestampBytes.Length, contentBytes.Length);

            return result;
        }
        public static Message DeserializeMessage(this byte[] bytes) {
            // I CANT USE SPANS NOOOOOOOOOOOOOOOOOOOO screw you unity and your old .net version
            
            var header = (MessageType)bytes[0];
            
            var senderIdBytes = new byte[16];
            Buffer.BlockCopy(bytes, 1, senderIdBytes, 0, 16);
            var senderId = new Guid(senderIdBytes);
            
            //var timestamp = DateTime.FromBinary(BitConverter.ToInt32(bytes, 17));
            var timestampBytes = new byte[8];
            Buffer.BlockCopy(bytes, 17, timestampBytes, 0, 8);
            var timestamp = DateTime.FromBinary(BitConverter.ToInt64(timestampBytes, 0));
            
            var contentBytes = new byte[bytes.Length - 25];
            Buffer.BlockCopy(bytes, 25, contentBytes, 0, contentBytes.Length);
            
            return new Message(header, senderId, timestamp, contentBytes);
        }
    }
}
