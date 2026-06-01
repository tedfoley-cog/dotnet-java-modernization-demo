-- =============================================================================
-- OrderManagement — SQL Server schema (T-SQL)
-- Matches the EF6 Code-First model under dotnet_source/OrderManagement.Web/Models.
-- Target migration: PostgreSQL/H2 via Spring Data JPA + Hibernate DDL.
-- =============================================================================

CREATE TABLE dbo.Customers
(
    CustomerId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerCode      NVARCHAR(20)  NOT NULL,
    Name              NVARCHAR(120) NOT NULL,
    PricingTier       NVARCHAR(10)  NOT NULL CONSTRAINT DF_Customers_Tier DEFAULT 'STANDARD',
    CreditLimit       MONEY         NOT NULL CONSTRAINT DF_Customers_Credit DEFAULT 0,
    PaymentTermsDays  INT           NOT NULL CONSTRAINT DF_Customers_Terms DEFAULT 30,
    TaxRegionCode     NCHAR(2)      NULL,
    OnCreditHold      BIT           NOT NULL CONSTRAINT DF_Customers_Hold DEFAULT 0,
    CreatedUtc        DATETIME2     NOT NULL CONSTRAINT DF_Customers_Created DEFAULT SYSUTCDATETIME()
);
GO
CREATE UNIQUE INDEX UX_Customers_Code ON dbo.Customers (CustomerCode);
GO

CREATE TABLE dbo.Products
(
    ProductId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Sku            NVARCHAR(30)  NOT NULL,
    Description    NVARCHAR(150) NOT NULL,
    ListPrice      MONEY         NOT NULL,
    TaxClass       NCHAR(3)      NOT NULL CONSTRAINT DF_Products_Tax DEFAULT 'STD',
    QuantityOnHand INT           NOT NULL CONSTRAINT DF_Products_Qoh DEFAULT 0,
    Discontinued   BIT           NOT NULL CONSTRAINT DF_Products_Disc DEFAULT 0
);
GO

CREATE TABLE dbo.Orders
(
    OrderId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderNumber   NVARCHAR(20) NOT NULL,
    CustomerId    INT          NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers (CustomerId),
    Status        INT          NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 0,
    OrderDate     DATETIME2    NOT NULL CONSTRAINT DF_Orders_Date DEFAULT SYSUTCDATETIME(),
    SubTotal      MONEY        NOT NULL CONSTRAINT DF_Orders_Sub DEFAULT 0,
    DiscountTotal MONEY        NOT NULL CONSTRAINT DF_Orders_Disc DEFAULT 0,
    TaxTotal      MONEY        NOT NULL CONSTRAINT DF_Orders_Tax DEFAULT 0,
    GrandTotal    MONEY        NOT NULL CONSTRAINT DF_Orders_Grand DEFAULT 0,
    CreatedBy     NVARCHAR(50) NULL
);
GO
CREATE UNIQUE INDEX UX_Orders_Number ON dbo.Orders (OrderNumber);
GO

CREATE TABLE dbo.OrderLines
(
    OrderLineId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderLines PRIMARY KEY,
    OrderId      INT           NOT NULL CONSTRAINT FK_OrderLines_Orders REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,
    ProductId    INT           NOT NULL CONSTRAINT FK_OrderLines_Products REFERENCES dbo.Products (ProductId),
    Quantity     INT           NOT NULL,
    UnitPrice    MONEY         NOT NULL,
    DiscountRate DECIMAL(5,4)  NOT NULL CONSTRAINT DF_OrderLines_Disc DEFAULT 0,
    LineTotal    MONEY         NOT NULL
);
GO

CREATE TABLE dbo.Invoices
(
    InvoiceId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    InvoiceNumber NVARCHAR(20) NOT NULL,
    OrderId       INT          NOT NULL CONSTRAINT FK_Invoices_Orders REFERENCES dbo.Orders (OrderId),
    IssuedDate    DATE         NOT NULL,
    DueDate       DATE         NOT NULL,
    AmountDue     MONEY        NOT NULL,
    Paid          BIT          NOT NULL CONSTRAINT DF_Invoices_Paid DEFAULT 0
);
GO
