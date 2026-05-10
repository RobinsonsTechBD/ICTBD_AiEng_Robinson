using InventoryAI.API.Data;
using InventoryAI.API.Models.Entities;
using InventoryAI.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAI.API.Repositories.Implementations;

// ============================================================
// Order Repository
// ============================================================
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Order>> GetAllAsync() =>
        await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.Creator)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<Order?> GetByIdAsync(int id) =>
        await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.Creator)
            .FirstOrDefaultAsync(o => o.OrderId == id);

    public async Task<Order> CreateAsync(Order order, IEnumerable<OrderItem> items)
    {
        order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
        order.OrderDate   = order.CreatedAt = order.UpdatedAt = DateTime.UtcNow;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        decimal total = 0;
        foreach (var item in items)
        {
            item.OrderId = order.OrderId;
            _db.OrderItems.Add(item);
            total += item.Quantity * item.UnitPrice;

            // Deduct stock
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.QuantityInStock -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    ProductId     = item.ProductId,
                    MovementType  = "OUT",
                    Quantity      = item.Quantity,
                    ReferenceId   = order.OrderId,
                    ReferenceType = "Order",
                    Notes         = $"Sale: {order.OrderNumber}",
                    CreatedBy     = order.CreatedBy,
                    CreatedAt     = DateTime.UtcNow
                });

                if (product.QuantityInStock <= product.LowStockThreshold)
                    _db.StockAlerts.Add(new StockAlert
                    {
                        ProductId = item.ProductId,
                        AlertType = "LowStock",
                        Message   = $"{product.ProductName} now at {product.QuantityInStock} units",
                        CreatedAt = DateTime.UtcNow
                    });
            }
        }

        order.TotalAmount = total - order.Discount;
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateStatusAsync(int id, string status)
    {
        var order = await _db.Orders.FindAsync(id)
            ?? throw new KeyNotFoundException($"Order {id} not found");
        order.Status    = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return false;
        order.Status    = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}

// ============================================================
// User Repository
// ============================================================
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

    public async Task<User?> GetByIdAsync(int id) =>
        await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _db.Users.Include(u => u.Role).Where(u => u.IsActive).ToListAsync();

    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt    = DateTime.UtcNow;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<MenuItem>> GetMenuByRoleAsync(int roleId)
    {
        var menuItemIds = await _db.RoleMenuItems
            .Where(r => r.RoleId == roleId && r.CanView)
            .Select(r => r.MenuItemId)
            .ToListAsync();

        return await _db.MenuItems
            .Where(m => menuItemIds.Contains(m.MenuItemId) && m.IsActive && m.ParentId == null)
            .Include(m => m.Children)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();
    }
}

// ============================================================
// Category Repository
// ============================================================
public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;
    public CategoryRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Category>> GetAllAsync() =>
        await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();

    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories.FindAsync(id);

    public async Task<Category> CreateAsync(Category cat)
    {
        cat.CreatedAt = DateTime.UtcNow;
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    public async Task<Category> UpdateAsync(Category cat)
    {
        _db.Categories.Update(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return false;
        cat.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

// ============================================================
// Report Repository
// ============================================================
public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;
    public ReportRepository(AppDbContext db) => _db = db;

    public async Task<object> GetDailySummaryAsync(DateTime date)
    {
        var orders = await _db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderDate.Date == date.Date && o.Status == "Completed")
            .ToListAsync();

        return new
        {
            Date        = date.Date,
            TotalOrders = orders.Count,
            TotalRevenue= orders.Sum(o => o.TotalAmount),
            TotalUnits  = orders.SelectMany(o => o.OrderItems).Sum(i => i.Quantity)
        };
    }

    public async Task<object> GetWeeklySummaryAsync(DateTime from, DateTime to)
    {
        var orders = await _db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to && o.Status == "Completed")
            .ToListAsync();

        return new
        {
            From = from, To = to,
            TotalOrders  = orders.Count,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            AverageOrder = orders.Any() ? orders.Average(o => o.TotalAmount) : 0
        };
    }

    public async Task<object> GetMonthlySummaryAsync(int year, int month)
    {
        var orders = await _db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderDate.Year == year && o.OrderDate.Month == month && o.Status == "Completed")
            .ToListAsync();

        return new
        {
            Year = year, Month = month,
            TotalOrders  = orders.Count,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
        };
    }

    public async Task<object> GetTopProductsAsync(int count = 10)
    {
        return await _db.OrderItems
            .Include(i => i.Product)
            .GroupBy(i => new { i.ProductId, i.Product.ProductName })
            .Select(g => new
            {
                g.Key.ProductId, g.Key.ProductName,
                TotalSold    = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(count)
            .ToListAsync<object>();
    }

    public async Task<object> GetStockLevelsAsync()
    {
        return await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.ProductId, p.SKU, p.ProductName,
                Category  = p.Category.CategoryName,
                p.QuantityInStock, p.LowStockThreshold,
                Status = p.QuantityInStock <= 0 ? "OUT_OF_STOCK"
                       : p.QuantityInStock <= p.LowStockThreshold ? "LOW"
                       : "OK"
            })
            .OrderBy(p => p.QuantityInStock)
            .ToListAsync<object>();
    }
}

// ============================================================
// Chat Repository
// ============================================================
public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _db;
    public ChatRepository(AppDbContext db) => _db = db;

    public async Task<ChatSession> CreateSessionAsync(int userId)
    {
        var session = new ChatSession { SessionId = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };
        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<ChatMessage> AddMessageAsync(Guid sessionId, string role, string content)
    {
        var msg = new ChatMessage { SessionId = sessionId, Role = role, Content = content, CreatedAt = DateTime.UtcNow };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();
        return msg;
    }

    public async Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid sessionId, int limit = 20) =>
        await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
}
