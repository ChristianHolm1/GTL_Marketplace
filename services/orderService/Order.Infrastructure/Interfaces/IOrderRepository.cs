using Order.Models.Models;
namespace Order.Infrastructure.Interfaces;

public interface IOrderRepository
{
    Task<OrderEntity?> GetByIdAsync(Guid id);
    Task UpdateAsync(OrderEntity order);
    Task DeleteAsync(Guid id);
    Task CreateAsync(OrderEntity order);
}
