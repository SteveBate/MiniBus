using System;
using MiniBus.Contracts;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeExceptionThrowingUserHandler : IHandleMessage<FakeDto>
    {
        public void Handle(FakeDto msg)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FakeEnvironmentExceptionThrowingUserHandler : IHandleMessage<FakeDto>
    {
        public void Handle(FakeDto msg)
        {
            throw new NotImplementedException("deadlocked");
        }
    }
}
