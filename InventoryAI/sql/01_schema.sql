-- ============================================================
-- AI Inventory Management System - Database Schema
-- MS SQL Server
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'InventoryAIDB')
    CREATE DATABASE InventoryAIDB;
GO

USE InventoryAIDB;
GO

-- ============================================================
-- ROLES & USERS (RBAC)
-- ============================================================
CREATE TABLE Roles (
    RoleId    INT IDENTITY(1,1) PRIMARY KEY,
    RoleName  NVARCHAR(50)  NOT NULL UNIQUE,
    IsActive  BIT           NOT NULL DEFAULT 1,
    CreatedAt DATETIME2     NOT NULL DEFAULT GETDATE()
);

CREATE TABLE MenuItems (
    MenuItemId   INT IDENTITY(1,1) PRIMARY KEY,
    ParentId     INT           NULL REFERENCES MenuItems(MenuItemId),
    Title        NVARCHAR(100) NOT NULL,
    Icon         NVARCHAR(100) NULL,
    RouteUrl     NVARCHAR(200) NULL,
    SortOrder    INT           NOT NULL DEFAULT 0,
    IsActive     BIT           NOT NULL DEFAULT 1
);

CREATE TABLE RoleMenuItems (
    RoleId     INT NOT NULL REFERENCES Roles(RoleId),
    MenuItemId INT NOT NULL REFERENCES MenuItems(MenuItemId),
    CanView    BIT NOT NULL DEFAULT 1,
    CanCreate  BIT NOT NULL DEFAULT 0,
    CanEdit    BIT NOT NULL DEFAULT 0,
    CanDelete  BIT NOT NULL DEFAULT 0,
    PRIMARY KEY (RoleId, MenuItemId)
);

CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150) NOT NULL,
    Email        NVARCHAR(200) NOT NULL UNIQUE,
    Username     NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    RoleId       INT           NOT NULL REFERENCES Roles(RoleId),
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE(),
    LastLogin    DATETIME2     NULL
);

-- ============================================================
-- PRODUCT CATALOG
-- ============================================================
CREATE TABLE Categories (
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description  NVARCHAR(500) NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Products (
    ProductId        INT IDENTITY(1,1) PRIMARY KEY,
    SKU              NVARCHAR(50)   NOT NULL UNIQUE,
    ProductName      NVARCHAR(200)  NOT NULL,
    Description      NVARCHAR(1000) NULL,
    CategoryId       INT            NOT NULL REFERENCES Categories(CategoryId),
    UnitPrice        DECIMAL(18,2)  NOT NULL DEFAULT 0,
    CostPrice        DECIMAL(18,2)  NOT NULL DEFAULT 0,
    QuantityInStock  INT            NOT NULL DEFAULT 0,
    LowStockThreshold INT           NOT NULL DEFAULT 10,
    ImagePath        NVARCHAR(500)  NULL,
    IsActive         BIT            NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt        DATETIME2      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE StockMovements (
    MovementId    INT IDENTITY(1,1) PRIMARY KEY,
    ProductId     INT            NOT NULL REFERENCES Products(ProductId),
    MovementType  NVARCHAR(20)   NOT NULL CHECK (MovementType IN ('IN','OUT','ADJUST')),
    Quantity      INT            NOT NULL,
    ReferenceId   INT            NULL,
    ReferenceType NVARCHAR(50)   NULL,
    Notes         NVARCHAR(500)  NULL,
    CreatedBy     INT            NOT NULL REFERENCES Users(UserId),
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- ORDERS / SALES (Master-Detail)
-- ============================================================
CREATE TABLE Orders (
    OrderId      INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber  NVARCHAR(50)   NOT NULL UNIQUE,
    OrderDate    DATETIME2      NOT NULL DEFAULT GETDATE(),
    CustomerName NVARCHAR(200)  NOT NULL,
    CustomerPhone NVARCHAR(50)  NULL,
    Status       NVARCHAR(30)   NOT NULL DEFAULT 'Pending'
                                CHECK (Status IN ('Pending','Processing','Completed','Cancelled')),
    TotalAmount  DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Discount     DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Notes        NVARCHAR(500)  NULL,
    CreatedBy    INT            NOT NULL REFERENCES Users(UserId),
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME2      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE OrderItems (
    OrderItemId  INT IDENTITY(1,1) PRIMARY KEY,
    OrderId      INT           NOT NULL REFERENCES Orders(OrderId),
    ProductId    INT           NOT NULL REFERENCES Products(ProductId),
    Quantity     INT           NOT NULL,
    UnitPrice    DECIMAL(18,2) NOT NULL,
    Subtotal     AS (Quantity * UnitPrice) PERSISTED
);

-- ============================================================
-- LOW STOCK ALERTS
-- ============================================================
CREATE TABLE StockAlerts (
    AlertId      INT IDENTITY(1,1) PRIMARY KEY,
    ProductId    INT           NOT NULL REFERENCES Products(ProductId),
    AlertType    NVARCHAR(50)  NOT NULL DEFAULT 'LowStock',
    Message      NVARCHAR(500) NULL,
    AiSuggestion NVARCHAR(2000) NULL,
    IsRead       BIT           NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- AI CHAT HISTORY (RAG context storage)
-- ============================================================
CREATE TABLE ChatSessions (
    SessionId  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId     INT           NOT NULL REFERENCES Users(UserId),
    CreatedAt  DATETIME2     NOT NULL DEFAULT GETDATE()
);

CREATE TABLE ChatMessages (
    MessageId  INT IDENTITY(1,1) PRIMARY KEY,
    SessionId  UNIQUEIDENTIFIER NOT NULL REFERENCES ChatSessions(SessionId),
    Role       NVARCHAR(20)  NOT NULL CHECK (Role IN ('user','assistant','system')),
    Content    NVARCHAR(MAX) NOT NULL,
    CreatedAt  DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- SEED DATA
-- ============================================================
INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Manager'), ('Staff');

INSERT INTO MenuItems (ParentId, Title, Icon, RouteUrl, SortOrder) VALUES
(NULL, 'Dashboard',        'bi-speedometer2',    '/dashboard',          1),
(NULL, 'Products',         'bi-box-seam',        '/products',           2),
(NULL, 'Categories',       'bi-tags',            '/categories',         3),
(NULL, 'Orders',           'bi-cart3',           '/orders',             4),
(NULL, 'Stock Movements',  'bi-arrow-left-right','/stock-movements',    5),
(NULL, 'Reports',          'bi-bar-chart-line',  '/reports',            6),
(NULL, 'Alerts',           'bi-bell',            '/alerts',             7),
(NULL, 'AI Assistant',     'bi-robot',           '/ai-chat',            8),
(NULL, 'User Management',  'bi-people',          '/users',              9);

-- Admin gets all permissions
INSERT INTO RoleMenuItems (RoleId, MenuItemId, CanView, CanCreate, CanEdit, CanDelete)
SELECT 1, MenuItemId, 1, 1, 1, 1 FROM MenuItems;

-- Manager: no user management, no delete
INSERT INTO RoleMenuItems (RoleId, MenuItemId, CanView, CanCreate, CanEdit, CanDelete)
SELECT 2, MenuItemId, 1, 1, 1, 0 FROM MenuItems WHERE MenuItemId <> 9;

-- Staff: view only + can create orders
INSERT INTO RoleMenuItems (RoleId, MenuItemId, CanView, CanCreate, CanEdit, CanDelete)
SELECT 3, MenuItemId, 1, 0, 0, 0 FROM MenuItems WHERE MenuItemId IN (1,2,3,4,7,8);

-- Admin user (Password: Admin@123)
INSERT INTO Users (FullName, Email, Username, PasswordHash, RoleId)
VALUES ('System Admin', 'admin@inventory.com', 'admin',
        'AQAAAAIAAYagAAAAELFhMPlq1JJQQmN6/...',  -- BCrypt hash placeholder
        1);

INSERT INTO Categories (CategoryName, Description) VALUES
('Electronics',    'Electronic devices and components'),
('Office Supplies','Stationery and office equipment'),
('Furniture',      'Office and warehouse furniture'),
('Tools',          'Hand tools and power tools'),
('Consumables',    'Regularly consumed items');

INSERT INTO Products (SKU, ProductName, Description, CategoryId, UnitPrice, CostPrice, QuantityInStock, LowStockThreshold) VALUES
('ELEC-001', 'Laptop Dell XPS 15',    '15" i7 laptop 16GB RAM',       1,  145000, 120000, 12, 5),
('ELEC-002', 'Wireless Mouse',        'Logitech MX Master 3',          1,   4500,   3000, 45, 10),
('ELEC-003', 'USB-C Hub 7-in-1',      'Multi-port USB hub',            1,   2800,   1800, 8,  10),
('OFF-001',  'A4 Copy Paper (Ream)',  '500 sheets 80gsm',              2,    450,    280, 3,  20),
('OFF-002',  'Ballpoint Pens (Box)',  'Blue ink, box of 50',           2,    350,    200, 60, 15),
('FURN-001', 'Ergonomic Office Chair','Adjustable lumbar support',     3,  18000,  12000, 7,  3),
('TOOL-001', 'Cordless Drill',        '18V with 2 batteries',          4,   6500,   4500, 15, 5),
('CONS-001', 'AA Batteries (Pack 8)', 'Alkaline long life',            5,    180,    100, 5,  25);
GO

-- ============================================================
-- VIEWS FOR RAG CONTEXT
-- ============================================================
CREATE VIEW vw_InventorySummary AS
SELECT
    p.ProductId, p.SKU, p.ProductName, c.CategoryName,
    p.QuantityInStock, p.LowStockThreshold, p.UnitPrice,
    CASE WHEN p.QuantityInStock <= p.LowStockThreshold THEN 'LOW' ELSE 'OK' END AS StockStatus,
    p.IsActive
FROM Products p
JOIN Categories c ON p.CategoryId = c.CategoryId;
GO

CREATE VIEW vw_SalesSummary AS
SELECT
    CAST(o.OrderDate AS DATE) AS OrderDate,
    COUNT(DISTINCT o.OrderId) AS TotalOrders,
    SUM(o.TotalAmount)        AS TotalRevenue,
    SUM(oi.Quantity)          AS TotalUnitsSold
FROM Orders o
JOIN OrderItems oi ON o.OrderId = oi.OrderId
WHERE o.Status = 'Completed'
GROUP BY CAST(o.OrderDate AS DATE);
GO

-- Stored procedure to get RAG context
CREATE PROCEDURE sp_GetInventoryContext
AS
BEGIN
    SELECT TOP 50
        ProductName, SKU, CategoryName, QuantityInStock,
        LowStockThreshold, UnitPrice, StockStatus
    FROM vw_InventorySummary
    WHERE IsActive = 1
    ORDER BY StockStatus DESC, QuantityInStock ASC;
END;
GO
