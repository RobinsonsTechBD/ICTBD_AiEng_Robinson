# 🤖 InventoryAI — AI-Powered Inventory Management System

> **Exam Project** — AI Project Development  
> Stack: Angular 19 (Non-Standalone) · ASP.NET Core 8 · MS SQL Server · Ollama Local LLM · RAG

---

## 📋 Table of Contents
1. [Project Overview](#1-project-overview)
2. [AI Technologies Used](#2-ai-technologies-used)
3. [System Architecture](#3-system-architecture)
4. [Prerequisites](#4-prerequisites)
5. [Step 1 — Database Setup (MS SQL)](#5-step-1--database-setup)
6. [Step 2 — Backend Setup (ASP.NET Core 8)](#6-step-2--backend-setup)
7. [Step 3 — Ollama Setup (Local LLM)](#7-step-3--ollama-setup)
8. [Step 4 — Frontend Setup (Angular 19)](#8-step-4--frontend-setup)
9. [Step 5 — Run Everything](#9-step-5--run-everything)
10. [Features](#10-features)
11. [API Endpoints](#11-api-endpoints)
12. [Default Credentials](#12-default-credentials)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Project Overview

InventoryAI is a **production-grade inventory management system** with integrated AI capabilities.
It solves real-world problems like:
- Preventing **stockouts** with intelligent low-stock alerts + AI restock advice
- Enabling **natural language queries** over your inventory (ask in plain English)
- Generating **AI-powered business insights** from sales and stock data
- **100% private AI** — runs entirely on local hardware via Ollama

---

## 2. AI Technologies Used

| Technology | Implementation | Purpose |
|---|---|---|
| **RAG** (Retrieval-Augmented Generation) | Live DB context injected into LLM prompts | Chat answers based on real inventory data |
| **Local LLM** (Ollama) | `qwen3:0.6b` or `gemma3:1b` | Chat, alerts, report insights |
| **Prompt Engineering** | Structured system + user prompts | Consistent, role-aware AI responses |
| **Chat History** | SQL-persisted conversation | Multi-turn inventory assistant |

**RAG Pipeline:**
```
User Question
     ↓
Fetch inventory from MS SQL  ← RETRIEVAL
     ↓
Build context string          ← AUGMENTATION
     ↓
Inject into Ollama prompt     ← GENERATION
     ↓
AI response with real data
```

---

## 3. System Architecture

```
┌─────────────────────────────────────────────────┐
│              Angular 19 (Port 4200)              │
│   Dashboard · Products · Orders · AI Chat        │
└─────────────────┬───────────────────────────────┘
                  │ HTTP + JWT
┌─────────────────▼───────────────────────────────┐
│          ASP.NET Core 8 API (Port 5000)          │
│   Controllers → Repository → EF Core            │
└──────────┬───────────────────┬──────────────────┘
           │                   │
┌──────────▼──────┐   ┌────────▼──────────────────┐
│   MS SQL Server  │   │   Ollama (Port 11434)      │
│  InventoryAIDB  │   │   qwen3:0.6b / gemma3:1b   │
└─────────────────┘   └───────────────────────────┘
```

---

## 4. Prerequisites

Ensure you have these installed:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org |
| Angular CLI | 19+ | `npm install -g @angular/cli` |
| MS SQL Server | 2019+ | https://www.microsoft.com/sql-server |
| SQL Server Management Studio (SSMS) | Any | https://aka.ms/ssms |
| Ollama | Latest | https://ollama.com |

---

## 5. Step 1 — Database Setup

### 5.1 Open SSMS and connect to your SQL Server instance

### 5.2 Run the schema script
```
File: sql/01_schema.sql
```
1. Open SSMS → New Query
2. Open `sql/01_schema.sql`
3. Press **F5** to execute
4. Verify: `InventoryAIDB` database is created with all tables and seed data

### 5.3 Verify tables exist
```sql
USE InventoryAIDB;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
-- Should show: Roles, Users, MenuItems, Categories, Products, Orders, OrderItems, etc.

SELECT * FROM Products;   -- Should show 8 sample products
SELECT * FROM Categories; -- Should show 5 categories
```

### 5.4 Update connection string (if needed)
Edit `backend/InventoryAI.API/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=InventoryAIDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
- For SQL Auth: `Server=.;Database=InventoryAIDB;User Id=sa;Password=YourPass;TrustServerCertificate=True;`
- For default local instance: `Server=localhost;Database=InventoryAIDB;...`

---

## 6. Step 2 — Backend Setup

### 6.1 Navigate to backend folder
```bash
cd backend/InventoryAI.API
```

### 6.2 Restore NuGet packages
```bash
dotnet restore
```

### 6.3 Verify appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryAIDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "InventoryAI_SuperSecretKey_2024_ChangeInProduction!",
    "Issuer": "InventoryAI.API",
    "Audience": "InventoryAI.Angular"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:0.6b"
  }
}
```

### 6.4 Create wwwroot folder for uploads
```bash
mkdir -p wwwroot/uploads/products
```

### 6.5 Build the project
```bash
dotnet build
```

### 6.6 Update admin password hash
The seed data has a placeholder hash. Update it:
```bash
# Run this one-time C# script or update manually in SQL:
```
```sql
-- In SSMS, run this to set password to "Admin@123":
UPDATE Users 
SET PasswordHash = '$2a$11$rBnxJWHCwi/NHsHfR9kK9eN8.OULVm3Xq0ZsF5YyTqKfL8JzjJm.K'
WHERE Username = 'admin';
-- Note: This is a BCrypt hash of "Admin@123"
-- Or just register via API after first login workaround
```

**Easier alternative** — update the seed in `01_schema.sql` before running it:
The hash `$2a$11$...` is pre-generated. For development, you can temporarily disable the password check in `AuthService.cs` and create the admin, then re-enable.

### 6.7 Run the API
```bash
dotnet run
```
✅ API runs at: `http://localhost:5000`  
✅ Swagger UI at: `http://localhost:5000/swagger`

---

## 7. Step 3 — Ollama Setup

### 7.1 Verify Ollama is installed
```bash
ollama --version
```

### 7.2 Start Ollama server
```bash
ollama serve
```
Keep this terminal open. Ollama runs at `http://localhost:11434`

### 7.3 Verify your models are available
```bash
ollama list
```
You should see:
```
NAME              ID              SIZE    MODIFIED
qwen3:0.6b        7df6b6e09427    522 MB  2 days ago   ← Best for this project (fast)
gemma3:1b         8648f39daa8f    815 MB  2 days ago   ← Good quality
qwen:4b           d53d04290064    2.3 GB  23 hours ago ← Best quality (slower)
gemma:2b          b50d6c999e59    1.7 GB  23 hours ago ← Good balance
```

### 7.4 Choose your model
Edit `backend/InventoryAI.API/appsettings.json`:
```json
"Ollama": {
  "Model": "qwen3:0.6b"
}
```

**Model recommendations:**
- `qwen3:0.6b` — **Fastest** (recommended for demos, 522MB RAM)
- `gemma3:1b`  — **Balanced** (better quality, 815MB)
- `gemma:2b`   — **Best quality** (1.7GB, slower responses)

### 7.5 Test Ollama manually
```bash
ollama run qwen3:0.6b "Say hello in one sentence"
```

---

## 8. Step 4 — Frontend Setup

### 8.1 Navigate to frontend folder
```bash
cd frontend
```

### 8.2 Install dependencies
```bash
npm install
```

### 8.3 Verify environment.ts
```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

### 8.4 Install Angular CLI if not present
```bash
npm install -g @angular/cli@19
```

### 8.5 Verify Angular version
```bash
ng version
# Should show Angular CLI: 19.x
```

---

## 9. Step 5 — Run Everything

Open **3 separate terminals**:

### Terminal 1 — Ollama
```bash
ollama serve
```

### Terminal 2 — Backend API
```bash
cd backend/InventoryAI.API
dotnet run
```
Wait for: `Now listening on: http://localhost:5000`

### Terminal 3 — Angular Frontend
```bash
cd frontend
ng serve
```
Wait for: `** Angular Live Development Server is listening on localhost:4200`

### Open the application
```
http://localhost:4200
```

---

## 10. Features

### Core Modules
| Module | Description |
|---|---|
| **Dashboard** | KPI cards, low-stock alerts, recent orders, AI insight button |
| **Products** | Full CRUD, image upload, profit margin calculator |
| **Categories** | CRUD with product count |
| **Orders** | Master-detail order creation, status management, real-time stock deduction |
| **Stock Movement** | Manual IN/OUT/ADJUST with audit trail |
| **Reports** | Daily/Weekly/Monthly summaries, top products, stock level chart |
| **Alerts** | Low stock list with per-product AI restock suggestions |
| **AI Chat** | RAG-powered inventory assistant with conversation history |
| **Users** | CRUD with RBAC (Admin/Manager/Staff) |

### AI Features
| Feature | Endpoint | Model Used |
|---|---|---|
| **Inventory Chat** | `POST /api/aichat/message` | RAG + Ollama |
| **Restock Suggestion** | `GET /api/alerts/{id}/suggestion` | Prompt Engineering |
| **Report Insight** | `GET /api/reports/ai-insight` | Prompt Engineering |

### RBAC Permissions
| Feature | Admin | Manager | Staff |
|---|---|---|---|
| View all modules | ✅ | ✅ | ✅ (limited) |
| Create/Edit products | ✅ | ✅ | ❌ |
| Delete | ✅ | ❌ | ❌ |
| User Management | ✅ | ❌ | ❌ |
| AI Chat | ✅ | ✅ | ✅ |

---

## 11. API Endpoints

### Auth
```
POST   /api/auth/login          → Login, returns JWT + menu
POST   /api/auth/register       → Register user (Admin only)
```

### Products
```
GET    /api/products            → List all products
GET    /api/products/{id}       → Get single product
GET    /api/products/low-stock  → Low stock products
POST   /api/products            → Create product
PUT    /api/products/{id}       → Update product
DELETE /api/products/{id}       → Soft delete
POST   /api/products/{id}/stock → Update stock (IN/OUT/ADJUST)
POST   /api/products/{id}/upload-image → Upload image
```

### Orders
```
GET    /api/orders              → List all orders
GET    /api/orders/{id}         → Order detail with items
POST   /api/orders              → Create order (deducts stock)
PATCH  /api/orders/{id}/status  → Update status
DELETE /api/orders/{id}         → Cancel order
```

### AI
```
POST   /api/aichat/session      → Start chat session
POST   /api/aichat/message      → Send message (RAG response)
GET    /api/aichat/{id}/history → Chat history
GET    /api/alerts/low-stock    → Low stock alerts
GET    /api/alerts/{id}/suggestion → AI restock suggestion
GET    /api/reports/ai-insight  → AI report analysis
```

---

## 12. Default Credentials

```
URL:      http://localhost:4200/login
Username: admin
Password: Admin@123
Role:     Admin (full access)
```

---

## 13. Troubleshooting

### ❌ API not starting
```bash
# Check .NET version
dotnet --version  # Must be 8.x

# Check SQL connection
# Verify SQL Server is running in Services
# Check connection string in appsettings.json
```

### ❌ "Cannot connect to database"
```bash
# Test connection in SSMS first
# Make sure SQL Server Browser service is running
# For Windows Auth: Trusted_Connection=True
# For SQL Auth: User Id=sa;Password=YourPass;
```

### ❌ AI chat not responding
```bash
# Make sure Ollama is running
ollama serve

# Test model directly
ollama run qwen3:0.6b "hello"

# Check API logs for Ollama errors
# Verify Model name matches in appsettings.json
```

### ❌ Angular build errors
```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install

# Check Node version (must be 18+)
node --version
```

### ❌ CORS errors
Ensure backend `Program.cs` has the correct Angular URL:
```csharp
p.WithOrigins("http://localhost:4200")
```

### ❌ Low stock alerts empty
Run the seed data SQL script again, or add products with stock below threshold.

---

## Project Structure

```
InventoryAI/
├── sql/
│   └── 01_schema.sql               ← Run this first
├── backend/
│   └── InventoryAI.API/
│       ├── Controllers/
│       │   └── Controllers.cs       ← All API controllers
│       ├── Data/
│       │   └── AppDbContext.cs      ← EF Core context
│       ├── Models/Entities/
│       │   └── Entities.cs          ← All entity classes
│       ├── Repositories/
│       │   ├── Interfaces/          ← Repository contracts
│       │   └── Implementations/     ← EF Core implementations
│       ├── Services/
│       │   ├── AIService.cs         ← RAG + Ollama integration ⭐
│       │   └── AuthService.cs       ← JWT auth
│       ├── Program.cs               ← DI + middleware setup
│       └── appsettings.json         ← Config (DB + JWT + Ollama)
└── frontend/
    └── src/app/
        ├── core/
        │   ├── services/services.ts ← All Angular services
        │   └── interceptors/        ← JWT interceptor + guards
        ├── layout/                  ← Sidebar + navbar shell
        └── modules/
            ├── auth/login/          ← Login page
            ├── dashboard/           ← KPI dashboard
            ├── products/            ← CRUD + image upload
            ├── orders/              ← Master-detail orders
            ├── alerts/              ← Low stock + AI suggestions ⭐
            ├── reports/             ← Analytics + AI insight ⭐
            ├── ai-chat/             ← RAG chat interface ⭐
            └── users/               ← RBAC user management
```

---

## 📌 Key AI Code Files

| File | What it does |
|---|---|
| `AIService.cs` | RAG pipeline, Ollama HTTP calls, prompt engineering |
| `ai-chat.component.ts/html` | Chat UI with session management |
| `alerts.component.ts/html` | Per-product AI restock advice |
| `reports.component.ts/html` | AI business insight generation |

---

*Built with ❤️ — Angular 19 · ASP.NET Core 8 · MS SQL · Ollama Local LLM · RAG*
