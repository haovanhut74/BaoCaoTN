using MyWebApp.Models;

namespace MyWebApp.Interface.Service;

public interface IGhnService
{
    Task<string> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
}