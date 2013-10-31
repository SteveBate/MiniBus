namespace MiniBus.Contracts
{
    public interface IHandleMessage<in T>
    {
        void Handle(T msg);
    }
}