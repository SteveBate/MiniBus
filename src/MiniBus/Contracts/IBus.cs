using System;

namespace MiniBus.Contracts
{
    public interface IBus : IDisposable
    {
        void Copy(string messageId);
        void Delete(string messageId);
        void RegisterHandler<T>(IHandleMessage<T> handler);
        void Send<T>(T message, string destination = "");
        void Receive<T>();
        void ReceiveOne<T>(string id);
        void ReceiveAsync<T>();
        void ReturnAllErrorMessages();
        void ReturnErrorMessage(string id);
        void StopReceiving();
        string ViewMessageBody(string messageId);
    }
}