using System.Text;
using System.Text.Json;
using InventoryAI.API.Repositories.Interfaces;

namespace InventoryAI.API.Services;

// ============================================================
// AI Service — RAG Pipeline + Ollama Integration
// ============================================================
public interface IAIService
{
    Task<string> ChatAsync(string userMessage, Guid sessionId, int userId);
    Task<string> GenerateAlertSuggestionAsync(string productName, int currentStock, int threshold);
    Task<string> GenerateReportInsightAsync(object reportData);
}

public class AIService : IAIService
{
    private readonly IProductRepository   _productRepo;
    private readonly IChatRepository      _chatRepo;
    private readonly IConfiguration       _config;
    private readonly HttpClient           _http;
    private readonly ILogger<AIService>   _logger;

    private const string SystemPromptBase = """
        You are an intelligent inventory management assistant for a warehouse/retail business.
        You help staff manage stock levels, analyze sales trends, and prevent stockouts.
        Always be concise, practical, and data-driven. Respond in clear English.
        If asked about stock levels, prices, or orders — use the inventory data provided.
        Never make up inventory numbers; only use what is given in the context.
        """;

    public AIService(
        IProductRepository productRepo,
        IChatRepository chatRepo,
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILogger<AIService> logger)
    {
        _productRepo = productRepo;
        _chatRepo    = chatRepo;
        _config      = config;
        _http        = httpFactory.CreateClient("ollama");
        _logger      = logger;
    }

    // ============================================================
    // MAIN CHAT — RAG PIPELINE
    // ============================================================
    public async Task<string> ChatAsync(string userMessage, Guid sessionId, int userId)
    {
        // 1. Save user message
        await _chatRepo.AddMessageAsync(sessionId, "user", userMessage);

        // 2. RAG — Retrieve inventory context from DB
        var inventoryContext = await BuildInventoryContextAsync();

        // 3. Get chat history for conversational memory
        var history = await _chatRepo.GetHistoryAsync(sessionId, limit: 10);

        // 4. Build messages array with system prompt + RAG context + history
        var messages = new List<object>
        {
            new {
                role    = "system",
                content = $"{SystemPromptBase}\n\n=== CURRENT INVENTORY CONTEXT ===\n{inventoryContext}"
            }
        };

        foreach (var msg in history.SkipLast(1)) // exclude the user message we just saved
            messages.Add(new { role = msg.Role, content = msg.Content });

        messages.Add(new { role = "user", content = userMessage });

        // 5. Call Ollama
        var response = await CallOllamaAsync(messages);

        // 6. Save assistant response
        await _chatRepo.AddMessageAsync(sessionId, "assistant", response);

        return response;
    }

    // ============================================================
    // ALERT SUGGESTION — Prompt Engineering for restock advice
    // ============================================================
    public async Task<string> GenerateAlertSuggestionAsync(
        string productName, int currentStock, int threshold)
    {
        var prompt = $"""
            Product: {productName}
            Current stock: {currentStock} units
            Low stock threshold: {threshold} units

            As an inventory manager, provide a brief (2-3 sentence) reorder recommendation
            including urgency level (Critical/High/Medium) and suggested reorder quantity.
            Be concise and actionable.
            """;

        var messages = new List<object>
        {
            new { role = "system", content = "You are an expert inventory analyst. Give brief, actionable advice." },
            new { role = "user",   content = prompt }
        };

        return await CallOllamaAsync(messages);
    }

    // ============================================================
    // REPORT INSIGHT — AI analysis on report data
    // ============================================================
    public async Task<string> GenerateReportInsightAsync(object reportData)
    {
        var dataJson = JsonSerializer.Serialize(reportData,
            new JsonSerializerOptions { WriteIndented = false });

        var messages = new List<object>
        {
            new {
                role    = "system",
                content = "You are an inventory business analyst. Analyze data and provide 3-4 bullet point insights. Be concise."
            },
            new {
                role    = "user",
                content = $"Analyze this inventory/sales data and give key insights:\n{dataJson}"
            }
        };

        return await CallOllamaAsync(messages);
    }

    // ============================================================
    // RAG CONTEXT BUILDER — Converts DB data to LLM-readable text
    // ============================================================
    private async Task<string> BuildInventoryContextAsync()
    {
        var products = await _productRepo.GetInventoryContextAsync();

        if (!products.Any()) return "No inventory data available.";

        var sb = new StringBuilder();
        sb.AppendLine("Product Inventory (format: Name | SKU | Category | Stock | Threshold | Price | Status):");

        foreach (var p in products)
        {
            var json = JsonSerializer.Serialize(p);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            sb.AppendLine(
                $"- {root.GetProperty("ProductName").GetString()} | " +
                $"{root.GetProperty("SKU").GetString()} | " +
                $"{root.GetProperty("Category").GetString()} | " +
                $"Stock:{root.GetProperty("QuantityInStock").GetInt32()} | " +
                $"Min:{root.GetProperty("LowStockThreshold").GetInt32()} | " +
                $"৳{root.GetProperty("UnitPrice").GetDecimal():N0} | " +
                $"{root.GetProperty("Status").GetString()}"
            );
        }

        return sb.ToString();
    }

    // ============================================================
    // OLLAMA HTTP CALL
    // ============================================================
    private async Task<string> CallOllamaAsync(List<object> messages)
    {
        var model      = _config["Ollama:Model"] ?? "qwen3:0.6b";
        var ollamaUrl  = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";

        var payload = new
        {
            model,
            messages,
            stream  = false,
            options = new { temperature = 0.7, num_predict = 512 }
        };

        try
        {
            var json     = JsonSerializer.Serialize(payload);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{ollamaUrl}/api/chat", content);

            response.EnsureSuccessStatusCode();

            var body      = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            return doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "I couldn't generate a response. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama call failed");
            return $"AI service unavailable: {ex.Message}. Please ensure Ollama is running with `ollama serve`.";
        }
    }
}
