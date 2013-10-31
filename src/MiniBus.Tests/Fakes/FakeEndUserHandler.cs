using MiniBus.Contracts;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeEndUserHandler : IHandleMessage<FakeDto>
    {
        public void Handle(FakeDto msg)
        {
            InvokeCount += 1;
        }

        public int InvokeCount { get; set; }
    }
}
