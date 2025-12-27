using ClientService.Entities;

namespace ClientService.Interfaces;


public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken ct = default);
}