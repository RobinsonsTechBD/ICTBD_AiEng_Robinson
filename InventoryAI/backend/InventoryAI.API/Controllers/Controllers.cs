using System.Security.Claims;
using InventoryAI.API.Models.Entities;
using InventoryAI.API.Repositories.Interfaces;
using InventoryAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAI.API.Controllers;

// ============================================================
// AUTH CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IAuthService    _authService;

    public AuthController(IUserRepository userRepo, IAuthService authService)
    {
        _userRepo    = userRepo;
        _authService = authService;
    }

    public record LoginDto(string Username, string Password);
    public record RegisterDto(string FullName, string Email, string Username, string Password, int RoleId);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userRepo.GetByUsernameAsync(dto.Username);
        if (user == null || !_authService.ValidatePassword(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = _authService.GenerateToken(user);
        var menu  = await _userRepo.GetMenuByRoleAsync(user.RoleId);

        return Ok(new
        {
            token,
            user = new
            {
                user.UserId, user.FullName, user.Email, user.Username,
                Role = user.Role?.RoleName
            },
            menu
        });
    }

    [HttpPost("register")]
    //, Authorize(Roles = "Admin")
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new User
        {
            FullName     = dto.FullName,
            Email        = dto.Email,
            Username     = dto.Username,
            PasswordHash = dto.Password, // hashed in repo
            RoleId       = dto.RoleId
        };
        var created = await _userRepo.CreateAsync(user);
        return CreatedAtAction(nameof(Login), new { id = created.UserId });
    }
}

// ============================================================
// PRODUCTS CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]"), Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repo;
    private readonly IAIService         _ai;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductRepository repo, IAIService ai, IWebHostEnvironment env)
    {
        _repo = repo; _ai = ai; _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repo.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _repo.GetLowStockAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var product = new Product
        {
            SKU = dto.SKU, ProductName = dto.ProductName, Description = dto.Description,
            CategoryId = dto.CategoryId, UnitPrice = dto.UnitPrice, CostPrice = dto.CostPrice,
            QuantityInStock = dto.QuantityInStock, LowStockThreshold = dto.LowStockThreshold
        };
        var created = await _repo.CreateAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductCreateDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.ProductName = dto.ProductName; existing.Description = dto.Description;
        existing.CategoryId  = dto.CategoryId;  existing.UnitPrice   = dto.UnitPrice;
        existing.CostPrice   = dto.CostPrice;   existing.LowStockThreshold = dto.LowStockThreshold;

        return Ok(await _repo.UpdateAsync(existing));
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpPost("{id}/upload-image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file");
        var folder   = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        var product = await _repo.GetByIdAsync(id);
        if (product == null) return NotFound();
        product.ImagePath = $"/uploads/products/{fileName}";
        await _repo.UpdateAsync(product);
        return Ok(new { imagePath = product.ImagePath });
    }

    [HttpPost("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var ok = await _repo.UpdateStockAsync(id, dto.Quantity, dto.Type, userId, notes: dto.Notes);
        return ok ? Ok(new { message = "Stock updated" }) : NotFound();
    }

    public record ProductCreateDto(string SKU, string ProductName, string? Description,
        int CategoryId, decimal UnitPrice, decimal CostPrice,
        int QuantityInStock, int LowStockThreshold);
    public record StockUpdateDto(int Quantity, string Type, string? Notes);
}

// ============================================================
// CATEGORIES CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]"), Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    public CategoriesController(ICategoryRepository repo) => _repo = repo;

    [HttpGet]        public async Task<IActionResult> GetAll()        => Ok(await _repo.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) =>
        await _repo.GetByIdAsync(id) is { } c ? Ok(c) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryDto dto)
    {
        var cat = new Category { CategoryName = dto.CategoryName, Description = dto.Description };
        return Ok(await _repo.CreateAsync(cat));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        existing.CategoryName = dto.CategoryName; existing.Description = dto.Description;
        return Ok(await _repo.UpdateAsync(existing));
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    public record CategoryDto(string CategoryName, string? Description);
}

// ============================================================
// ORDERS CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]"), Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repo;
    public OrdersController(IOrderRepository repo) => _repo = repo;

    [HttpGet]        public async Task<IActionResult> GetAll()        => Ok(await _repo.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) =>
        await _repo.GetByIdAsync(id) is { } o ? Ok(o) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var order = new Order
        {
            CustomerName  = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            Discount      = dto.Discount,
            Notes         = dto.Notes,
            CreatedBy     = userId
        };
        var items = dto.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity  = i.Quantity,
            UnitPrice = i.UnitPrice
        });
        return Ok(await _repo.CreateAsync(order, items));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusDto dto) =>
        Ok(await _repo.UpdateStatusAsync(id, dto.Status));

    [HttpDelete("{id}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    public record OrderItemDto(int ProductId, int Quantity, decimal UnitPrice);
    public record OrderCreateDto(string CustomerName, string? CustomerPhone,
        decimal Discount, string? Notes, List<OrderItemDto> Items);
    public record StatusDto(string Status);
}

// ============================================================
// REPORTS CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]"), Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _repo;
    private readonly IAIService        _ai;
    public ReportsController(IReportRepository repo, IAIService ai) { _repo = repo; _ai = ai; }

    [HttpGet("daily")]
    public async Task<IActionResult> Daily([FromQuery] DateTime? date)
    {
        var data = await _repo.GetDailySummaryAsync(date ?? DateTime.Today);
        return Ok(data);
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> Weekly([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var f = from ?? DateTime.Today.AddDays(-7);
        var t = to   ?? DateTime.Today;
        return Ok(await _repo.GetWeeklySummaryAsync(f, t));
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var y = year  ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        return Ok(await _repo.GetMonthlySummaryAsync(y, m));
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] int count = 10) =>
        Ok(await _repo.GetTopProductsAsync(count));

    [HttpGet("stock-levels")]
    public async Task<IActionResult> StockLevels() =>
        Ok(await _repo.GetStockLevelsAsync());

    [HttpGet("ai-insight")]
    public async Task<IActionResult> AiInsight([FromQuery] string type = "monthly")
    {
        object data = type switch
        {
            "daily"  => await _repo.GetDailySummaryAsync(DateTime.Today),
            "weekly" => await _repo.GetWeeklySummaryAsync(DateTime.Today.AddDays(-7), DateTime.Today),
            _        => await _repo.GetMonthlySummaryAsync(DateTime.Today.Year, DateTime.Today.Month)
        };
        var insight = await _ai.GenerateReportInsightAsync(data);
        return Ok(new { insight, data });
    }
}

// ============================================================
// AI CHAT CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]"), Authorize]
public class AIChatController : ControllerBase
{
    private readonly IAIService      _ai;
    private readonly IChatRepository _chatRepo;

    public AIChatController(IAIService ai, IChatRepository chatRepo)
    {
        _ai = ai; _chatRepo = chatRepo;
    }

    [HttpPost("session")]
    public async Task<IActionResult> StartSession()
    {
        var userId  = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var session = await _chatRepo.CreateSessionAsync(userId);
        return Ok(new { sessionId = session.SessionId });
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatDto dto)
    {
        var response = await _ai.ChatAsync(dto.Message, dto.SessionId, 0);
        return Ok(new { response, sessionId = dto.SessionId });
    }

    [HttpGet("{sessionId}/history")]
    public async Task<IActionResult> GetHistory(Guid sessionId) =>
        Ok(await _chatRepo.GetHistoryAsync(sessionId));

    public record ChatDto(string Message, Guid SessionId);
}

// ============================================================
// ALERTS CONTROLLER
// ============================================================
[ApiController, Route("api/[controller]"), Authorize]
public class AlertsController : ControllerBase
{
    private readonly IProductRepository _productRepo;
    private readonly IAIService         _ai;

    public AlertsController(IProductRepository productRepo, IAIService ai)
    {
        _productRepo = productRepo; _ai = ai;
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockAlerts()
    {
        var products = await _productRepo.GetLowStockAsync();
        var alerts = products.Select(p => new
        {
            p.ProductId, p.ProductName, p.SKU,
            p.QuantityInStock, p.LowStockThreshold,
            Severity = p.QuantityInStock == 0 ? "Critical"
                     : p.QuantityInStock <= p.LowStockThreshold / 2 ? "High" : "Medium"
        });
        return Ok(alerts);
    }

    [HttpGet("{productId}/suggestion")]
    public async Task<IActionResult> GetAiSuggestion(int productId)
    {
        var product = await _productRepo.GetByIdAsync(productId);
        if (product == null) return NotFound();
        var suggestion = await _ai.GenerateAlertSuggestionAsync(
            product.ProductName, product.QuantityInStock, product.LowStockThreshold);
        return Ok(new { suggestion, product = new { product.ProductId, product.ProductName } });
    }
}
