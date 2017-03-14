using System;
using System.Messaging;
using System.Text;
using MiniBus.Serialization;
using System.IO;

namespace MiniBus.Formatters
{
    internal class JsonFormatter<T> : IMessageFormatter
    {
        public bool CanRead(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var stream = message.BodyStream;

            return stream != null
                && stream.CanRead
                && stream.Length > 0;
        }

        public object Read(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (CanRead(message) == false)
            {
                return null;
            }

            using (var reader = new StreamReader(message.BodyStream, Encoding.Default))
            {
                var json = reader.ReadToEnd();
                return SimpleJson.DeserializeObject<T>(json);
            }            
        }

        public void Write(Message message, object obj)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            string json = SimpleJson.SerializeObject(obj);

            message.BodyStream = new MemoryStream(Encoding.Default.GetBytes(json));

            //Need to reset the body type, in case the same message is reused by some other formatter.
            message.BodyType = 0;
        }

        public object Clone()
        {
            return new JsonFormatter<T>();
        }
    }
}
