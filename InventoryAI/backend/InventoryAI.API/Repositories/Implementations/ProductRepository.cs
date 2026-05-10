using InventoryAI.API.Data;
using InventoryAI.API.Models.Entities;
using InventoryAI.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAI.API.Repositories.Implementations;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Product>> GetAllAsync(bool includeInactive = false) =>
        await _db.Products
            .Include(p => p.Category)
            .Where(p => includeInactive || p.IsActive)
            .OrderBy(p => p.ProductName)
            .ToListAsync();

    public async Task<Product?> GetByIdAsync(int id) =>
        await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);

    public async Task<Product?> GetBySKUAsync(string sku) =>
        await _db.Products.FirstOrDefaultAsync(p => p.SKU == sku);

    public async Task<IEnumerable<Product>> GetLowStockAsync() =>
        await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.QuantityInStock <= p.LowStockThreshold)
            .OrderBy(p => p.QuantityInStock)
            .ToListAsync();

    public async Task<Product> CreateAsync(Product product)
    {
        product.CreatedAt = product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        p.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStockAsync(int productId, int qty, string type,
        int userId, int? refId = null, string? notes = null)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return false;

        product.QuantityInStock = type switch
        {
            "IN"     => product.QuantityInStock + qty,
            "OUT"    => product.QuantityInStock - qty,
            "ADJUST" => qty,
            _        => product.QuantityInStock
        };
        product.UpdatedAt = DateTime.UtcNow;

        _db.StockMovements.Add(new StockMovement
        {
            ProductId     = productId,
            MovementType  = type,
            Quantity      = qty,
            ReferenceId   = refId,
            ReferenceType = refId.HasValue ? "Order" : null,
            Notes         = notes,
            CreatedBy     = userId,
            CreatedAt     = DateTime.UtcNow
        });

        // Auto-create alert if stock goes low
        if (product.QuantityInStock <= product.LowStockThreshold)
        {
            _db.StockAlerts.Add(new StockAlert
            {
                ProductId = productId,
                AlertType = "LowStock",
                Message   = $"{product.ProductName} is running low ({product.QuantityInStock} remaining)",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<object>> GetInventoryContextAsync()
    {
        return await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .Select(p => (object)new
            {
                p.SKU, p.ProductName,
                Category  = p.Category.CategoryName,
                p.QuantityInStock, p.LowStockThreshold,
                p.UnitPrice,
                Status = p.QuantityInStock <= p.LowStockThreshold ? "LOW STOCK" : "OK"
            })
            .ToListAsync();
    }
}
