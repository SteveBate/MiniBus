using System;
using System.IO;
using System.Text;
using MSMQ.Messaging;

namespace MiniBus.Formatters
{
    /// <summary>
    /// BodyAsStringFormatter dumps the body as a string to help debug or interrogate the contents of a message
    /// </summary>
    internal class BodyAsStringFormatter : IMessageFormatter
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
                return reader.ReadToEnd()
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "")
                    .Replace("\"{", "{")
                    .Replace("}\"", "}")
                    .Replace("\\t", "")
                    .Replace("\\r", "");
            }            
        }

        public void Write(Message message, object obj)
        {
            // no op
        }

        public object Clone()
        {
            return new BodyAsStringFormatter();
        }
    }
}