-- =============================================================================
-- sp_OrderSummary — monthly order totals per customer for the reporting screen.
-- Demonstrates a legacy reporting stored procedure that the Java migration must
-- reproduce (typically as a Spring Data @Query or a repository projection).
-- =============================================================================
CREATE PROCEDURE dbo.sp_OrderSummary
    @FromDate DATETIME2,
    @ToDate   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.CustomerCode,
        c.Name,
        c.PricingTier,
        COUNT(o.OrderId)        AS OrderCount,
        SUM(o.SubTotal)         AS GrossTotal,
        SUM(o.DiscountTotal)    AS DiscountTotal,
        SUM(o.GrandTotal)       AS NetTotal
    FROM dbo.Orders o
        INNER JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
    WHERE o.OrderDate >= @FromDate
      AND o.OrderDate <  @ToDate
      AND o.Status <> 5            -- exclude Cancelled
    GROUP BY c.CustomerCode, c.Name, c.PricingTier
    ORDER BY NetTotal DESC;
END
GO
