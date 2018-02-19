namespace MiniBus.Core
{
    public interface IFilter<in T> where T : BaseMessage
    {
        void Execute(T msg);
    }
}
