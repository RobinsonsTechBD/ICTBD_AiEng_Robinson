// ============================================================
// Models/Entities - All database entity classes
// ============================================================
namespace InventoryAI.API.Models.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RoleMenuItem> RoleMenuItems { get; set; } = new List<RoleMenuItem>();
}

public class MenuItem
{
    public int MenuItemId { get; set; }
    public int? ParentId { get; set; }
    public string Title { get; set; } = default!;
    public string? Icon { get; set; }
    public string? RouteUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public ICollection<RoleMenuItem> RoleMenuItems { get; set; } = new List<RoleMenuItem>();
}

public class RoleMenuItem
{
    public int RoleId { get; set; }
    public int MenuItemId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public Role Role { get; set; } = default!;
    public MenuItem MenuItem { get; set; } = default!;
}

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public Role Role { get; set; } = default!;
}

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Category Category { get; set; } = default!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

public class StockMovement
{
    public int MovementId { get; set; }
    public int ProductId { get; set; }
    public string MovementType { get; set; } = default!; // IN | OUT | ADJUST
    public int Quantity { get; set; }
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Product Product { get; set; } = default!;
    public User Creator { get; set; } = default!;
}

public class Order
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User Creator { get; set; } = default!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    // DB persisted computed column - read only
    public decimal Subtotal { get; private set; }
    public Order Order { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

public class StockAlert
{
    public int AlertId { get; set; }
    public int ProductId { get; set; }
    public string AlertType { get; set; } = "LowStock";
    public string? Message { get; set; }
    public string? AiSuggestion { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public Product Product { get; set; } = default!;
}

public class ChatSession
{
    public Guid SessionId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = default!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage
{
    public int MessageId { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = default!; // user | assistant | system
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public ChatSession Session { get; set; } = default!;
}
