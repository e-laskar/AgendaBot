using AgendaBot.Application.Ports;
using AgendaBot.Testing.Contracts;
using AgendaBot.Testing.Fakes;

namespace AgendaBot.UnitTests.Application;

public class InMemoryEventRepositoryContractTests : EventRepositoryContract
{
    protected override Task<IEventRepository> CreateEmptyRepositoryAsync() =>
        Task.FromResult<IEventRepository>(new InMemoryEventRepository());
}
