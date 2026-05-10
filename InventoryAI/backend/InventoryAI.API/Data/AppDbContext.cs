using InventoryAI.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryAI.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RoleMenuItem> RoleMenuItems => Set<RoleMenuItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockAlert> StockAlerts => Set<StockAlert>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Explicit PKs for every entity ─────────────────────
        mb.Entity<Role>().HasKey(e => e.RoleId);
        mb.Entity<User>().HasKey(e => e.UserId);
        mb.Entity<MenuItem>().HasKey(e => e.MenuItemId);
        mb.Entity<Category>().HasKey(e => e.CategoryId);
        mb.Entity<Product>().HasKey(e => e.ProductId);
        mb.Entity<Order>().HasKey(e => e.OrderId);
        mb.Entity<OrderItem>().HasKey(e => e.OrderItemId);
        mb.Entity<StockMovement>().HasKey(e => e.MovementId);
        mb.Entity<StockAlert>().HasKey(e => e.AlertId);
        mb.Entity<ChatSession>().HasKey(e => e.SessionId);   // Guid PK
        mb.Entity<ChatMessage>().HasKey(e => e.MessageId);

        // ── Composite PK ──────────────────────────────────────
        mb.Entity<RoleMenuItem>().HasKey(e => new { e.RoleId, e.MenuItemId });

        // ── Decimal precision ─────────────────────────────────
        mb.Entity<Product>(e => {
            e.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
        });

        mb.Entity<Order>(e => {
            e.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.Discount).HasColumnType("decimal(18,2)");
        });

        mb.Entity<OrderItem>(e => {
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.Subtotal)
             .HasColumnType("decimal(18,2)")
             .HasComputedColumnSql("[Quantity] * [UnitPrice]", stored: true);
        });

        // ── Delete behaviours (avoid cascade cycles) ──────────
        mb.Entity<StockMovement>(e => {
            e.HasOne(s => s.Product)
             .WithMany(p => p.StockMovements)
             .HasForeignKey(s => s.ProductId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Creator)
             .WithMany()
             .HasForeignKey(s => s.CreatedBy)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<Order>(e => {
            e.HasOne(o => o.Creator)
             .WithMany()
             .HasForeignKey(o => o.CreatedBy)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<OrderItem>(e => {
            e.HasOne(i => i.Order)
             .WithMany(o => o.OrderItems)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Product)
             .WithMany(p => p.OrderItems)
             .HasForeignKey(i => i.ProductId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<StockAlert>(e => {
            e.HasOne(a => a.Product)
             .WithMany()
             .HasForeignKey(a => a.ProductId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<ChatSession>(e => {
            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<ChatMessage>(e => {
            e.HasOne(m => m.Session)
             .WithMany(s => s.Messages)
             .HasForeignKey(m => m.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // MenuItem self-referencing parent
        mb.Entity<MenuItem>(e => {
            e.HasOne(m => m.Parent)
             .WithMany(m => m.Children)
             .HasForeignKey(m => m.ParentId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<RoleMenuItem>(e => {
            e.HasOne(r => r.Role)
             .WithMany(r => r.RoleMenuItems)
             .HasForeignKey(r => r.RoleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.MenuItem)
             .WithMany(m => m.RoleMenuItems)
             .HasForeignKey(r => r.MenuItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<User>(e => {
            e.HasOne(u => u.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<Product>(e => {
            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
