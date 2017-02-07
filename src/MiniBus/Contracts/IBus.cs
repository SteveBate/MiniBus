using System;

namespace MiniBus.Contracts
{
    public interface IBus : IDisposable
    {
        void Copy(string messageId);
        void RegisterHandler<T>(IHandleMessage<T> handler);
        void Send<T>(T message);
        void Receive<T>();
        void ReceiveAsync<T>();
        void ReturnAllErrorMessages();
        void ReturnErrorMessage(string id);
        void StopReceiving();
    }
}