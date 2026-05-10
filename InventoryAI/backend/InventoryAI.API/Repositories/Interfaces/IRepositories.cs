// ============================================================
// Repository Pattern — Interfaces
// ============================================================
using InventoryAI.API.Models.Entities;

namespace InventoryAI.API.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(bool includeInactive = false);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySKUAsync(string sku);
    Task<IEnumerable<Product>> GetLowStockAsync();
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateStockAsync(int productId, int qty, string type, int userId, int? refId = null, string? notes = null);
    Task<List<object>> GetInventoryContextAsync(); // for RAG
}

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order> CreateAsync(Order order, IEnumerable<OrderItem> items);
    Task<Order> UpdateStatusAsync(int id, string status);
    Task<bool> DeleteAsync(int id);
}

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category> CreateAsync(Category cat);
    Task<Category> UpdateAsync(Category cat);
    Task<bool> DeleteAsync(int id);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<MenuItem>> GetMenuByRoleAsync(int roleId);
}

public interface IReportRepository
{
    Task<object> GetDailySummaryAsync(DateTime date);
    Task<object> GetWeeklySummaryAsync(DateTime from, DateTime to);
    Task<object> GetMonthlySummaryAsync(int year, int month);
    Task<object> GetTopProductsAsync(int count = 10);
    Task<object> GetStockLevelsAsync();
}

public interface IChatRepository
{
    Task<ChatSession> CreateSessionAsync(int userId);
    Task<ChatMessage> AddMessageAsync(Guid sessionId, string role, string content);
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid sessionId, int limit = 20);
}
