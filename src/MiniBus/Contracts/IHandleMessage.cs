namespace MiniBus.Contracts
{
    /// <summary>
    /// IHandleMessage is the interface that the library consumer must implement in their transport messages
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHandleMessage<in T>
    {
        void Handle(T msg);
    }
}