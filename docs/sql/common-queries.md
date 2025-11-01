# Common SQL Queries

**Description**: Collection of frequently used SQL query patterns for common database operations.

**Language/Technology**: SQL (Standard SQL compatible with most databases)

**Code**:
```sql
-- 1. SELECT with filtering and ordering
-- Get top 10 most expensive products in stock
SELECT 
    product_id,
    product_name,
    price,
    stock_quantity
FROM products
WHERE stock_quantity > 0
ORDER BY price DESC
LIMIT 10;

-- 2. JOIN multiple tables
-- Get order details with customer and product information
SELECT 
    o.order_id,
    c.customer_name,
    c.email,
    p.product_name,
    oi.quantity,
    oi.unit_price,
    (oi.quantity * oi.unit_price) AS line_total
FROM orders o
INNER JOIN customers c ON o.customer_id = c.customer_id
INNER JOIN order_items oi ON o.order_id = oi.order_id
INNER JOIN products p ON oi.product_id = p.product_id
WHERE o.order_date >= DATE_SUB(CURRENT_DATE, INTERVAL 30 DAY)
ORDER BY o.order_date DESC;

-- 3. Aggregate functions with GROUP BY
-- Get sales summary by category
SELECT 
    p.category,
    COUNT(DISTINCT o.order_id) AS total_orders,
    SUM(oi.quantity) AS total_items_sold,
    SUM(oi.quantity * oi.unit_price) AS total_revenue,
    AVG(oi.unit_price) AS avg_price
FROM order_items oi
INNER JOIN products p ON oi.product_id = p.product_id
INNER JOIN orders o ON oi.order_id = o.order_id
GROUP BY p.category
HAVING total_revenue > 1000
ORDER BY total_revenue DESC;

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

**Usage**:
```sql
-- Example: Get monthly sales report
SELECT 
    DATE_FORMAT(order_date, '%Y-%m') AS month,
    COUNT(DISTINCT order_id) AS total_orders,
    COUNT(DISTINCT customer_id) AS unique_customers,
    SUM(total_amount) AS revenue,
    AVG(total_amount) AS avg_order_value
FROM orders
WHERE order_date >= DATE_SUB(CURRENT_DATE, INTERVAL 12 MONTH)
GROUP BY DATE_FORMAT(order_date, '%Y-%m')
ORDER BY month DESC;

-- Example: Find best-selling products
SELECT 
    p.product_id,
    p.product_name,
    p.category,
    SUM(oi.quantity) AS units_sold,
    SUM(oi.quantity * oi.unit_price) AS revenue
FROM products p
INNER JOIN order_items oi ON p.product_id = oi.product_id
INNER JOIN orders o ON oi.order_id = o.order_id
WHERE o.order_date >= DATE_SUB(CURRENT_DATE, INTERVAL 90 DAY)
GROUP BY p.product_id, p.product_name, p.category
ORDER BY units_sold DESC
LIMIT 20;
```

**Notes**: 
- Standard SQL syntax with database-specific variations noted below
- **MySQL/MariaDB**: Uses `DATE_SUB()`, `LIMIT`
  ```sql
  WHERE order_date >= DATE_SUB(CURRENT_DATE, INTERVAL 30 DAY)
  ORDER BY order_date DESC
  LIMIT 10;
  ```
- **PostgreSQL**: Uses `INTERVAL`, `LIMIT`
  ```sql
  WHERE order_date >= CURRENT_DATE - INTERVAL '30 days'
  ORDER BY order_date DESC
  LIMIT 10;
  ```
- **SQL Server**: Uses `DATEADD()`, `TOP`
  ```sql
  WHERE order_date >= DATEADD(day, -30, GETDATE())
  ORDER BY order_date DESC
  OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;
  ```
- **Oracle**: Uses `ADD_MONTHS()`, `ROWNUM` or `FETCH FIRST`
  ```sql
  WHERE order_date >= CURRENT_DATE - 30
  ORDER BY order_date DESC
  FETCH FIRST 10 ROWS ONLY;
  ```
- Always test UPDATE/DELETE queries with SELECT first
- Use transactions for critical data modifications
- Consider adding indexes on frequently queried columns
- Test queries in your specific database environment
- Related: [Window Functions](window-functions.md), [Performance Optimization](performance-optimization.md)
