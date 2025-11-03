# Enterprise SQL Query Patterns

**Description**: Production-ready SQL patterns with security hardening, performance optimization, and cross-database compatibility for enterprise applications.

**Language/Technology**: Standard SQL with vendor-specific optimizations

**Code**:

```sql
-- ============================================================
-- 1. Performance-Optimized Product Query with Parameterization
-- ============================================================
-- Secure, parameterized query with explicit column selection and proper indexing hints

-- Standard SQL (parameterized - use with prepared statements)
SELECT 
    prod.product_id,
    prod.product_name,
    prod.price,
    prod.stock_quantity,
    cat.category_name,
    prod.last_updated
FROM products prod
INNER JOIN categories cat ON prod.category_id = cat.category_id
WHERE prod.stock_quantity > ? 
    AND prod.is_active = 1
    AND prod.price BETWEEN ? AND ?
ORDER BY prod.price DESC, prod.product_name ASC
OFFSET ? ROWS FETCH NEXT ? ROWS ONLY; -- SQL Server/PostgreSQL

-- Alternative pagination syntax:
-- MySQL/MariaDB: LIMIT ? OFFSET ?
-- Oracle: OFFSET ? ROWS FETCH FIRST ? ROWS ONLY

-- Performance notes:
-- Index recommendation: (is_active, stock_quantity, price) covering index
-- Parameter binding prevents SQL injection

-- ============================================================
-- 2. Secure Multi-Table Join with Audit Trail
-- ============================================================
-- Enterprise pattern with security, performance, and compliance considerations

WITH order_enriched AS (
    SELECT 
        ord.order_id,
        ord.customer_id,
        ord.order_date,
        ord.order_status,
        ord.total_amount,
        -- Calculate order age for business logic
        DATEDIFF(DAY, ord.order_date, GETDATE()) AS days_since_order,
        -- Window function for customer analytics
        ROW_NUMBER() OVER (
            PARTITION BY ord.customer_id 
            ORDER BY ord.order_date DESC
        ) AS customer_order_sequence
    FROM orders ord
    WHERE ord.order_date >= DATEADD(MONTH, -3, GETDATE())
        AND ord.is_deleted = 0
)
SELECT 
    oe.order_id,
    -- PII handling with conditional masking
    CASE 
        WHEN @user_role = 'ADMIN' THEN cust.customer_name
        ELSE LEFT(cust.customer_name, 1) + '***'
    END AS customer_display_name,
    CASE 
        WHEN @user_role IN ('ADMIN', 'SUPPORT') THEN cust.email
        ELSE CONCAT(LEFT(cust.email, 3), '***@', 
                   RIGHT(cust.email, CHARINDEX('@', REVERSE(cust.email)) + 2))
    END AS email_display,
    prod.product_name,
    oi.quantity,
    oi.unit_price,
    (oi.quantity * oi.unit_price) AS line_total,
    oe.days_since_order,
    oe.customer_order_sequence
FROM order_enriched oe
INNER JOIN customers cust ON oe.customer_id = cust.customer_id
INNER JOIN order_items oi ON oe.order_id = oi.order_id
INNER JOIN products prod ON oi.product_id = prod.product_id
WHERE cust.is_active = 1
    AND (@customer_segment IS NULL OR cust.customer_segment = @customer_segment)
ORDER BY oe.order_date DESC, oe.order_id ASC;

-- ============================================================
-- 3. Advanced Analytics with Window Functions and CTEs
-- ============================================================
-- Enterprise-grade analytics with performance optimization and statistical analysis

WITH sales_metrics AS (
    SELECT 
        cat.category_id,
        cat.category_name,
        prod.product_id,
        prod.product_name,
        oi.quantity,
        oi.unit_price,
        (oi.quantity * oi.unit_price) AS line_revenue,
        ord.order_date,
        -- Window functions for comparative analysis
        SUM(oi.quantity * oi.unit_price) OVER (
            PARTITION BY cat.category_id
        ) AS category_total_revenue,
        AVG(oi.unit_price) OVER (
            PARTITION BY cat.category_id
        ) AS category_avg_price,
        PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY oi.unit_price) OVER (
            PARTITION BY cat.category_id
        ) AS category_median_price
    FROM order_items oi
    INNER JOIN products prod ON oi.product_id = prod.product_id
    INNER JOIN categories cat ON prod.category_id = cat.category_id
    INNER JOIN orders ord ON oi.order_id = ord.order_id
    WHERE ord.order_date >= DATEADD(QUARTER, -4, GETDATE())
        AND ord.order_status = 'COMPLETED'
        AND prod.is_active = 1
),
category_summary AS (
    SELECT 
        category_id,
        category_name,
        COUNT(DISTINCT product_id) AS unique_products,
        COUNT(*) AS total_line_items,
        SUM(quantity) AS total_units_sold,
        MAX(category_total_revenue) AS total_revenue, -- MAX removes duplicates from window function
        MAX(category_avg_price) AS avg_unit_price,
        MAX(category_median_price) AS median_unit_price,
        STDDEV(line_revenue) AS revenue_std_deviation,
        MIN(line_revenue) AS min_line_revenue,
        MAX(line_revenue) AS max_line_revenue
    FROM sales_metrics
    GROUP BY category_id, category_name
)
SELECT 
    cs.category_name,
    cs.unique_products,
    cs.total_line_items,
    cs.total_units_sold,
    CAST(cs.total_revenue AS DECIMAL(15,2)) AS total_revenue,
    CAST(cs.avg_unit_price AS DECIMAL(10,2)) AS avg_unit_price,
    CAST(cs.median_unit_price AS DECIMAL(10,2)) AS median_unit_price,
    CAST(cs.revenue_std_deviation AS DECIMAL(10,2)) AS revenue_volatility,
    -- Performance indicators
    CASE 
        WHEN cs.total_revenue > 50000 THEN 'High Performance'
        WHEN cs.total_revenue > 20000 THEN 'Medium Performance'
        ELSE 'Needs Attention'
    END AS performance_tier,
    -- Revenue share calculation
    CAST(
        (cs.total_revenue * 100.0) / SUM(cs.total_revenue) OVER() 
        AS DECIMAL(5,2)
    ) AS revenue_share_percent
FROM category_summary cs
WHERE cs.total_revenue > @minimum_revenue_threshold -- Parameterized filter
ORDER BY cs.total_revenue DESC, cs.category_name ASC;

-- 4. Subquery - Find customers with above-average orders
SELECT 
    c.customer_id,
    c.customer_name,
    COUNT(o.order_id) AS order_count,
    SUM(o.total_amount) AS total_spent
FROM customers c
INNER JOIN orders o ON c.customer_id = o.customer_id
GROUP BY c.customer_id, c.customer_name
HAVING SUM(o.total_amount) > (
    SELECT AVG(total_amount)
    FROM orders
)
ORDER BY total_spent DESC;

-- 5. UPDATE with JOIN
-- Update product prices based on category discount
UPDATE products p
INNER JOIN category_discounts cd ON p.category = cd.category
SET p.price = p.price * (1 - cd.discount_percentage / 100)
WHERE cd.active = 1
  AND p.price > 10;

-- 6. INSERT with SELECT
-- Copy active customers to a marketing list
INSERT INTO marketing_list (customer_id, customer_name, email, segment)
SELECT 
    c.customer_id,
    c.customer_name,
    c.email,
    CASE 
        WHEN total_spent > 1000 THEN 'VIP'
        WHEN total_spent > 500 THEN 'Premium'
        ELSE 'Standard'
    END AS segment
FROM customers c
INNER JOIN (
    SELECT 
        customer_id,
        SUM(total_amount) AS total_spent
    FROM orders
    WHERE order_date >= DATE_SUB(CURRENT_DATE, INTERVAL 1 YEAR)
    GROUP BY customer_id
) o ON c.customer_id = o.customer_id
WHERE c.active = 1;

-- 7. DELETE with conditions
-- Remove old completed orders (keep for 2 years)
DELETE FROM orders
WHERE status = 'COMPLETED'
  AND order_date < DATE_SUB(CURRENT_DATE, INTERVAL 2 YEAR);

-- 8. CASE statement for conditional logic
SELECT 
    product_id,
    product_name,
    stock_quantity,
    CASE 
        WHEN stock_quantity = 0 THEN 'Out of Stock'
        WHEN stock_quantity < 10 THEN 'Low Stock'
        WHEN stock_quantity < 50 THEN 'Medium Stock'
        ELSE 'In Stock'
    END AS stock_status
FROM products
ORDER BY stock_quantity ASC;

-- 9. Common Table Expression (CTE)
-- Find customers and their most recent order
WITH RecentOrders AS (
    SELECT 
        customer_id,
        order_id,
        order_date,
        total_amount,
        ROW_NUMBER() OVER (PARTITION BY customer_id ORDER BY order_date DESC) AS rn
    FROM orders
)
SELECT 
    c.customer_name,
    c.email,
    ro.order_id,
    ro.order_date,
    ro.total_amount
FROM customers c
LEFT JOIN RecentOrders ro ON c.customer_id = ro.customer_id AND ro.rn = 1
WHERE c.active = 1;

-- 10. EXISTS clause for checking related records
-- Find customers who have never placed an order
SELECT 
    customer_id,
    customer_name,
    email,
    registration_date
FROM customers c
WHERE NOT EXISTS (
    SELECT 1
    FROM orders o
    WHERE o.customer_id = c.customer_id
)
ORDER BY registration_date DESC;
```

-- ============================================================
-- 5. Cross-Database Compatibility Patterns
-- ============================================================
-- Enterprise patterns that work across major database platforms

-- Universal date range filtering with parameterized inputs
-- SQL Server / Azure SQL
CREATE PROCEDURE GetSalesReport_SQLSERVER
    @start_date DATE,
    @end_date DATE,
    @category_filter NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH monthly_sales AS (
        SELECT 
            YEAR(o.order_date) AS year_num,
            MONTH(o.order_date) AS month_num,
            FORMAT(o.order_date, 'yyyy-MM') AS month_key,
            c.category_name,
            COUNT(DISTINCT o.order_id) AS total_orders,
            COUNT(DISTINCT o.customer_id) AS unique_customers,
            SUM(o.total_amount) AS revenue,
            AVG(o.total_amount) AS avg_order_value
        FROM orders o
        INNER JOIN order_items oi ON o.order_id = oi.order_id
        INNER JOIN products p ON oi.product_id = p.product_id
        INNER JOIN categories c ON p.category_id = c.category_id
        WHERE o.order_date >= @start_date
            AND o.order_date <= @end_date
            AND o.order_status = 'COMPLETED'
            AND (@category_filter IS NULL OR c.category_name = @category_filter)
        GROUP BY 
            YEAR(o.order_date),
            MONTH(o.order_date),
            FORMAT(o.order_date, 'yyyy-MM'),
            c.category_name
    )
    SELECT 
        ms.year_num,
        ms.month_num,
        ms.month_key,
        ms.category_name,
        ms.total_orders,
        ms.unique_customers,
        CAST(ms.revenue AS DECIMAL(15,2)) AS revenue,
        CAST(ms.avg_order_value AS DECIMAL(10,2)) AS avg_order_value,
        -- Comparative metrics
        LAG(ms.revenue) OVER (
            PARTITION BY ms.category_name 
            ORDER BY ms.year_num, ms.month_num
        ) AS previous_month_revenue,
        CASE 
            WHEN LAG(ms.revenue) OVER (
                PARTITION BY ms.category_name 
                ORDER BY ms.year_num, ms.month_num
            ) > 0 THEN
                CAST(
                    ((ms.revenue - LAG(ms.revenue) OVER (
                        PARTITION BY ms.category_name 
                        ORDER BY ms.year_num, ms.month_num
                    )) * 100.0) / LAG(ms.revenue) OVER (
                        PARTITION BY ms.category_name 
                        ORDER BY ms.year_num, ms.month_num
                    ) AS DECIMAL(5,2)
                )
            ELSE NULL
        END AS growth_rate_percent
    FROM monthly_sales ms
    ORDER BY ms.category_name, ms.year_num DESC, ms.month_num DESC;
END;

-- PostgreSQL equivalent with JSON aggregation
CREATE OR REPLACE FUNCTION get_sales_report_postgresql(
    p_start_date DATE,
    p_end_date DATE,
    p_category_filter VARCHAR(50) DEFAULT NULL
) RETURNS TABLE (
    report_data JSONB
) AS $$
BEGIN
    RETURN QUERY
    WITH monthly_sales AS (
        SELECT 
            EXTRACT(YEAR FROM o.order_date)::INTEGER AS year_num,
            EXTRACT(MONTH FROM o.order_date)::INTEGER AS month_num,
            TO_CHAR(o.order_date, 'YYYY-MM') AS month_key,
            c.category_name,
            COUNT(DISTINCT o.order_id) AS total_orders,
            COUNT(DISTINCT o.customer_id) AS unique_customers,
            SUM(o.total_amount) AS revenue,
            AVG(o.total_amount) AS avg_order_value
        FROM orders o
        INNER JOIN order_items oi ON o.order_id = oi.order_id
        INNER JOIN products p ON oi.product_id = p.product_id
        INNER JOIN categories c ON p.category_id = c.category_id
        WHERE o.order_date >= p_start_date
            AND o.order_date <= p_end_date
            AND o.order_status = 'COMPLETED'
            AND (p_category_filter IS NULL OR c.category_name = p_category_filter)
        GROUP BY 
            EXTRACT(YEAR FROM o.order_date),
            EXTRACT(MONTH FROM o.order_date),
            TO_CHAR(o.order_date, 'YYYY-MM'),
            c.category_name
    )
    SELECT 
        jsonb_build_object(
            'year', ms.year_num,
            'month', ms.month_num,
            'month_key', ms.month_key,
            'category', ms.category_name,
            'metrics', jsonb_build_object(
                'total_orders', ms.total_orders,
                'unique_customers', ms.unique_customers,
                'revenue', ROUND(ms.revenue::NUMERIC, 2),
                'avg_order_value', ROUND(ms.avg_order_value::NUMERIC, 2)
            ),
            'growth_analysis', jsonb_build_object(
                'previous_month_revenue', LAG(ms.revenue) OVER (
                    PARTITION BY ms.category_name 
                    ORDER BY ms.year_num, ms.month_num
                ),
                'growth_rate_percent', 
                CASE 
                    WHEN LAG(ms.revenue) OVER (
                        PARTITION BY ms.category_name 
                        ORDER BY ms.year_num, ms.month_num
                    ) > 0 THEN
                        ROUND(
                            ((ms.revenue - LAG(ms.revenue) OVER (
                                PARTITION BY ms.category_name 
                                ORDER BY ms.year_num, ms.month_num
                            )) * 100.0) / LAG(ms.revenue) OVER (
                                PARTITION BY ms.category_name 
                                ORDER BY ms.year_num, ms.month_num
                            ), 2
                        )
                    ELSE NULL
                END
            )
        ) AS report_data
    FROM monthly_sales ms
    ORDER BY ms.category_name, ms.year_num DESC, ms.month_num DESC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================
-- 6. Advanced Query Optimization & Performance Patterns
-- ============================================================
-- Production-grade query patterns with execution plan considerations

-- Efficient pagination with offset alternatives
WITH ranked_products AS (
    SELECT 
        p.product_id,
        p.product_name,
        p.price,
        c.category_name,
        p.created_date,
        ROW_NUMBER() OVER (ORDER BY p.created_date DESC, p.product_id) AS row_num,
        COUNT(*) OVER() AS total_count
    FROM products p
    INNER JOIN categories c ON p.category_id = c.category_id
    WHERE p.is_active = 1
        AND (@category_filter IS NULL OR c.category_name = @category_filter)
        AND (@price_min IS NULL OR p.price >= @price_min)
        AND (@price_max IS NULL OR p.price <= @price_max)
)
SELECT 
    rp.product_id,
    rp.product_name,
    CAST(rp.price AS DECIMAL(10,2)) AS price,
    rp.category_name,
    rp.created_date,
    rp.total_count,
    -- Pagination metadata
    CAST(CEILING(rp.total_count / CAST(@page_size AS FLOAT)) AS INT) AS total_pages,
    @current_page AS current_page
FROM ranked_products rp
WHERE rp.row_num BETWEEN (@current_page - 1) * @page_size + 1 
                     AND @current_page * @page_size
ORDER BY rp.row_num;

-- Dynamic search with full-text capabilities
CREATE PROCEDURE SearchProductsCrossPlatform
    @search_term NVARCHAR(255),
    @category_ids NVARCHAR(500) = NULL, -- Comma-separated IDs
    @sort_order NVARCHAR(20) = 'relevance'
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Parse category filter
    DECLARE @category_table TABLE (category_id INT);
    IF @category_ids IS NOT NULL
    BEGIN
        INSERT INTO @category_table (category_id)
        SELECT CAST(value AS INT)
        FROM STRING_SPLIT(@category_ids, ',')
        WHERE ISNUMERIC(value) = 1;
    END
    
    WITH search_results AS (
        SELECT 
            p.product_id,
            p.product_name,
            p.description,
            p.price,
            c.category_name,
            p.created_date,
            -- Relevance scoring
            (CASE 
                WHEN p.product_name LIKE '%' + @search_term + '%' THEN 10
                ELSE 0
            END +
            CASE 
                WHEN p.description LIKE '%' + @search_term + '%' THEN 5
                ELSE 0
            END +
            CASE 
                WHEN c.category_name LIKE '%' + @search_term + '%' THEN 3
                ELSE 0
            END) AS relevance_score,
            -- Performance metrics
            ISNULL(ps.total_sold, 0) AS units_sold_last_90_days,
            ISNULL(ps.avg_rating, 0) AS avg_customer_rating
        FROM products p
        INNER JOIN categories c ON p.category_id = c.category_id
        LEFT JOIN (
            SELECT 
                oi.product_id,
                SUM(oi.quantity) AS total_sold,
                AVG(CAST(pr.rating AS DECIMAL(3,2))) AS avg_rating
            FROM order_items oi
            INNER JOIN orders o ON oi.order_id = o.order_id
            LEFT JOIN product_reviews pr ON oi.product_id = pr.product_id
            WHERE o.order_date >= DATEADD(DAY, -90, GETUTCDATE())
                AND o.order_status = 'COMPLETED'
            GROUP BY oi.product_id
        ) ps ON p.product_id = ps.product_id
        WHERE p.is_active = 1
            AND (
                @search_term IS NULL 
                OR p.product_name LIKE '%' + @search_term + '%'
                OR p.description LIKE '%' + @search_term + '%'
                OR c.category_name LIKE '%' + @search_term + '%'
            )
            AND (
                NOT EXISTS (SELECT 1 FROM @category_table)
                OR c.category_id IN (SELECT category_id FROM @category_table)
            )
    )
    SELECT 
        sr.product_id,
        sr.product_name,
        sr.description,
        CAST(sr.price AS DECIMAL(10,2)) AS price,
        sr.category_name,
        sr.created_date,
        sr.relevance_score,
        sr.units_sold_last_90_days,
        CAST(sr.avg_customer_rating AS DECIMAL(3,2)) AS avg_customer_rating
    FROM search_results sr
    WHERE sr.relevance_score > 0 OR @search_term IS NULL
    ORDER BY 
        CASE WHEN @sort_order = 'relevance' THEN sr.relevance_score END DESC,
        CASE WHEN @sort_order = 'price_asc' THEN sr.price END ASC,
        CASE WHEN @sort_order = 'price_desc' THEN sr.price END DESC,
        CASE WHEN @sort_order = 'popularity' THEN sr.units_sold_last_90_days END DESC,
        CASE WHEN @sort_order = 'rating' THEN sr.avg_customer_rating END DESC,
        sr.product_name ASC;
END;

-- ============================================================
-- 7. Error Handling & Transaction Management
-- ============================================================
-- Enterprise error handling with comprehensive logging

CREATE PROCEDURE ProcessOrderWithErrorHandling
    @customer_id INT,
    @order_items NVARCHAR(MAX), -- JSON array of items
    @payment_method NVARCHAR(50),
    @order_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Automatic rollback on errors
    
    DECLARE @error_number INT = 0;
    DECLARE @error_message NVARCHAR(4000) = '';
    DECLARE @transaction_id UNIQUEIDENTIFIER = NEWID();
    
    BEGIN TRY
        BEGIN TRANSACTION ProcessOrder;
        
        -- Validate customer
        IF NOT EXISTS (
            SELECT 1 FROM customers 
            WHERE customer_id = @customer_id AND is_active = 1
        )
        BEGIN
            THROW 50001, 'Invalid or inactive customer ID', 1;
        END
        
        -- Create order header
        INSERT INTO orders (
            customer_id, order_date, order_status, 
            payment_method, transaction_id, created_date
        )
        VALUES (
            @customer_id, GETUTCDATE(), 'PENDING',
            @payment_method, @transaction_id, GETUTCDATE()
        );
        
        SET @order_id = SCOPE_IDENTITY();
        
        -- Process order items from JSON
        WITH parsed_items AS (
            SELECT 
                JSON_VALUE(value, '$.product_id') AS product_id,
                CAST(JSON_VALUE(value, '$.quantity') AS INT) AS quantity,
                CAST(JSON_VALUE(value, '$.unit_price') AS DECIMAL(10,2)) AS unit_price
            FROM OPENJSON(@order_items)
        ),
        validated_items AS (
            SELECT 
                pi.product_id,
                pi.quantity,
                pi.unit_price,
                p.price AS current_price,
                p.stock_quantity AS available_stock
            FROM parsed_items pi
            INNER JOIN products p ON pi.product_id = p.product_id
            WHERE p.is_active = 1
                AND pi.quantity > 0
                AND pi.quantity <= p.stock_quantity
                AND ABS(pi.unit_price - p.price) < 0.01 -- Price validation
        )
        INSERT INTO order_items (
            order_id, product_id, quantity, unit_price, line_total
        )
        SELECT 
            @order_id,
            vi.product_id,
            vi.quantity,
            vi.unit_price,
            vi.quantity * vi.unit_price
        FROM validated_items vi;
        
        -- Update inventory
        UPDATE p
        SET 
            stock_quantity = p.stock_quantity - oi.quantity,
            last_sale_date = GETUTCDATE()
        FROM products p
        INNER JOIN order_items oi ON p.product_id = oi.product_id
        WHERE oi.order_id = @order_id;
        
        -- Calculate order total
        UPDATE orders
        SET total_amount = (
            SELECT SUM(line_total) 
            FROM order_items 
            WHERE order_id = @order_id
        )
        WHERE order_id = @order_id;
        
        -- Log successful processing
        INSERT INTO order_processing_log (
            order_id, transaction_id, processing_stage, 
            status, message, created_date
        )
        VALUES (
            @order_id, @transaction_id, 'ORDER_CREATED',
            'SUCCESS', 'Order processed successfully', GETUTCDATE()
        );
        
        COMMIT TRANSACTION ProcessOrder;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION ProcessOrder;
        
        SELECT 
            @error_number = ERROR_NUMBER(),
            @error_message = ERROR_MESSAGE();
        
        -- Log error details
        INSERT INTO order_processing_log (
            order_id, transaction_id, processing_stage,
            status, message, error_number, created_date
        )
        VALUES (
            ISNULL(@order_id, 0), @transaction_id, 'ORDER_PROCESSING',
            'ERROR', @error_message, @error_number, GETUTCDATE()
        );
        
        -- Re-throw error for caller handling
        THROW;
    END CATCH
END;

**Performance Notes**:

- **Indexing Strategy**: Create composite indexes on frequently queried column combinations
- **Query Execution Plans**: Always analyze execution plans for complex queries
- **Parameter Sniffing**: Use `OPTION (OPTIMIZE FOR UNKNOWN)` for dynamic queries
- **Statistics Maintenance**: Keep statistics up-to-date for optimal query performance
- **Connection Pooling**: Use connection pooling in application layer
- **Batch Operations**: Process large datasets in smaller batches to avoid blocking

**Cross-Platform Compatibility**:

- **SQL Server**: Full T-SQL features, JSON support, advanced window functions
- **PostgreSQL**: Excellent JSON/JSONB support, advanced analytics, custom functions
- **MySQL 8.0+**: Common table expressions, window functions, JSON functions
- **Oracle**: Advanced analytics, hierarchical queries, comprehensive SQL standard support
- **SQLite**: Limited advanced features, suitable for development and small applications

**Security Best Practices**:

- **Always use parameterized queries** to prevent SQL injection
- **Implement role-based access control** at database level
- **Encrypt sensitive data** both at rest and in transit
- **Regular security audits** of database permissions and access patterns
- **Monitor and log** all database operations for compliance
- **Use least privilege principle** for application database connections

**Related Documentation**:

- [Query Performance Optimization](performance-optimization.md)
- [Database Security Patterns](security-patterns.md)
- [Window Functions Reference](window-functions.md)
- [Cross-Database Migration Guide](migration-guide.md)
- [Transaction Management Best Practices](transaction-management.md)
