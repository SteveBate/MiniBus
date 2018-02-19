namespace MiniBus.Core
{
    public interface IAspect<T> where T : BaseMessage
    {
        void Execute(T msg);
        IAspect<T> Next { get; set; }
    }
}
