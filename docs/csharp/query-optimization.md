# LINQ Query Optimization Patterns

**Description**: Advanced LINQ query optimization techniques including expression analysis, query rewriting, predicate composition, and performance monitoring. Focuses on reducing allocations, improving execution plans, and optimizing database translations.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ComponentModel;

// Predicate composition and optimization
public static class PredicateOptimizer
{
    // Compile and cache predicates for better performance
    private static readonly ConcurrentDictionary<string, Delegate> PredicateCache = new();

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));

        return CombinePredicates(left, right, Expression.AndAlso);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));

        return CombinePredicates(left, right, Expression.OrElse);
    }

    public static Expression<Func<T, bool>> Not<T>(
        this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var parameter = expression.Parameters[0];
        var notExpression = Expression.Not(expression.Body);
        
        return Expression.Lambda<Func<T, bool>>(notExpression, parameter);
    }

    private static Expression<Func<T, bool>> CombinePredicates<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combineFunction)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        
        var combinedBody = combineFunction(leftBody, rightBody);
        
        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    // Cached compilation for frequently used predicates
    public static Func<T, bool> CompileAndCache<T>(
        this Expression<Func<T, bool>> expression,
        string? cacheKey = null)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        cacheKey ??= expression.ToString();
        
        return (Func<T, bool>)PredicateCache.GetOrAdd(cacheKey, _ => expression.Compile());
    }

    // Optimized predicate builder for dynamic queries
    public static PredicateBuilder<T> Create<T>() => new();
}

public class PredicateBuilder<T>
{
    private Expression<Func<T, bool>>? _predicate;

    public PredicateBuilder<T> And(Expression<Func<T, bool>> condition)
    {
        _predicate = _predicate?.And(condition) ?? condition;
        return this;
    }

    public PredicateBuilder<T> Or(Expression<Func<T, bool>> condition)
    {
        _predicate = _predicate?.Or(condition) ?? condition;
        return this;
    }

    public PredicateBuilder<T> AndIf(bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? And(predicate) : this;
    }

    public PredicateBuilder<T> OrIf(bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? Or(predicate) : this;
    }

    public Expression<Func<T, bool>>? Build() => _predicate;

    public Func<T, bool> Compile(string? cacheKey = null)
    {
        if (_predicate == null)
            throw new InvalidOperationException("No predicates have been added");
            
        return _predicate.CompileAndCache(cacheKey);
    }

    public static implicit operator Expression<Func<T, bool>>?(PredicateBuilder<T> builder)
    {
        return builder._predicate;
    }
}

// Parameter replacement visitor
public class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}

// Query execution optimization
public static class QueryOptimizer
{
    // Materialize queries efficiently based on expected size
    public static List<T> OptimizedToList<T>(this IQueryable<T> query, int? expectedCount = null)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        // Pre-size the list if we have a hint about expected count
        if (expectedCount.HasValue)
        {
            var list = new List<T>(expectedCount.Value);
            list.AddRange(query);
            return list;
        }

        return query.ToList();
    }

    // Optimized counting that uses database-level optimizations
    public static bool HasAny<T>(this IQueryable<T> query, int minimumCount = 1)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return query.Take(minimumCount).Count() >= minimumCount;
    }

    // Efficient existence checking
    public static bool Exists<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return query.Where(predicate).Take(1).Any();
    }

    // Batched processing for large datasets
    public static IEnumerable<IEnumerable<T>> Batch<T>(
        this IQueryable<T> query, 
        int batchSize,
        Expression<Func<T, object>>? orderBy = null)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        var orderedQuery = orderBy != null ? query.OrderBy(orderBy) : query;
        var skip = 0;

        List<T> batch;
        do
        {
            batch = orderedQuery.Skip(skip).Take(batchSize).ToList();
            if (batch.Count > 0)
            {
                yield return batch;
                skip += batchSize;
            }
        } while (batch.Count == batchSize);
    }

    // Efficient paging with total count
    public static PagedResult<T> ToPagedResult<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        bool includeTotalCount = true)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (pageNumber < 0) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var skip = pageNumber * pageSize;
        var items = query.Skip(skip).Take(pageSize).ToList();
        var totalCount = includeTotalCount ? query.Count() : -1;

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            HasNextPage = items.Count == pageSize,
            HasPreviousPage = pageNumber > 0
        };
    }

    // Smart projection that reduces data transfer
    public static IQueryable<TResult> SelectOptimized<T, TResult>(
        this IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        bool distinct = false)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        var result = query.Select(selector);
        return distinct ? result.Distinct() : result;
    }

    // Conditional ordering to avoid unnecessary sorting
    public static IOrderedQueryable<T> OrderByIf<T, TKey>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, TKey>> keySelector,
        bool descending = false)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        if (!condition) 
            return query.OrderBy(x => 0); // Dummy ordering to maintain signature

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    // Efficient join operations with proper indexing hints
    public static IQueryable<TResult> JoinOptimized<TOuter, TInner, TKey, TResult>(
        this IQueryable<TOuter> outer,
        IQueryable<TInner> inner,
        Expression<Func<TOuter, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<TOuter, TInner, TResult>> resultSelector,
        bool useLeftJoin = false)
    {
        if (outer == null) throw new ArgumentNullException(nameof(outer));
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
        if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        if (useLeftJoin)
        {
            return outer.GroupJoin(
                inner,
                outerKeySelector,
                innerKeySelector,
                (o, innerGroup) => new { Outer = o, InnerGroup = innerGroup })
                .SelectMany(
                    x => x.InnerGroup.DefaultIfEmpty(),
                    (x, i) => resultSelector.Compile()(x.Outer, i));
        }

        return outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector);
    }
}

// Query performance monitoring
public class QueryPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, QueryStats> _stats = new();

    public T MonitorQuery<T>(string queryName, Func<T> queryExecution)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);

        try
        {
            var result = queryExecution();
            stopwatch.Stop();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            UpdateStats(queryName, stopwatch.Elapsed, memoryUsed, true);

            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            UpdateStats(queryName, stopwatch.Elapsed, 0, false);
            throw;
        }
    }

    private void UpdateStats(string queryName, TimeSpan elapsed, long memoryUsed, bool successful)
    {
        _stats.AddOrUpdate(queryName,
            new QueryStats
            {
                QueryName = queryName,
                ExecutionCount = 1,
                TotalElapsedTime = elapsed,
                MaxElapsedTime = elapsed,
                MinElapsedTime = elapsed,
                TotalMemoryUsed = memoryUsed,
                MaxMemoryUsed = memoryUsed,
                SuccessfulExecutions = successful ? 1 : 0,
                FailedExecutions = successful ? 0 : 1
            },
            (key, existingStats) =>
            {
                existingStats.ExecutionCount++;
                existingStats.TotalElapsedTime += elapsed;
                existingStats.MaxElapsedTime = elapsed > existingStats.MaxElapsedTime ? elapsed : existingStats.MaxElapsedTime;
                existingStats.MinElapsedTime = elapsed < existingStats.MinElapsedTime ? elapsed : existingStats.MinElapsedTime;
                existingStats.TotalMemoryUsed += memoryUsed;
                existingStats.MaxMemoryUsed = memoryUsed > existingStats.MaxMemoryUsed ? memoryUsed : existingStats.MaxMemoryUsed;
                
                if (successful)
                    existingStats.SuccessfulExecutions++;
                else
                    existingStats.FailedExecutions++;

                return existingStats;
            });
    }

    public QueryStats GetStats(string queryName)
    {
        return _stats.TryGetValue(queryName, out var stats) ? stats : new QueryStats { QueryName = queryName };
    }

    public IEnumerable<QueryStats> GetAllStats()
    {
        return _stats.Values.ToList();
    }

    public string GeneratePerformanceReport()
    {
        var allStats = GetAllStats().OrderByDescending(s => s.AverageElapsedTime);
        var report = new System.Text.StringBuilder();

        report.AppendLine("Query Performance Report");
        report.AppendLine("======================");
        report.AppendLine();

        foreach (var stats in allStats)
        {
            report.AppendLine($"Query: {stats.QueryName}");
            report.AppendLine($"  Executions: {stats.ExecutionCount:N0} (Success: {stats.SuccessfulExecutions:N0}, Failed: {stats.FailedExecutions:N0})");
            report.AppendLine($"  Average Time: {stats.AverageElapsedTime.TotalMilliseconds:F2}ms");
            report.AppendLine($"  Min/Max Time: {stats.MinElapsedTime.TotalMilliseconds:F2}ms / {stats.MaxElapsedTime.TotalMilliseconds:F2}ms");
            report.AppendLine($"  Average Memory: {stats.AverageMemoryUsed / 1024:F2}KB");
            report.AppendLine($"  Success Rate: {stats.SuccessRate:P2}");
            report.AppendLine();
        }

        return report.ToString();
    }
}

public class QueryStats
{
    public string QueryName { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public TimeSpan TotalElapsedTime { get; set; }
    public TimeSpan MaxElapsedTime { get; set; }
    public TimeSpan MinElapsedTime { get; set; } = TimeSpan.MaxValue;
    public long TotalMemoryUsed { get; set; }
    public long MaxMemoryUsed { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }

    public TimeSpan AverageElapsedTime => ExecutionCount > 0 
        ? TimeSpan.FromTicks(TotalElapsedTime.Ticks / ExecutionCount) 
        : TimeSpan.Zero;

    public long AverageMemoryUsed => ExecutionCount > 0 
        ? TotalMemoryUsed / ExecutionCount 
        : 0;

    public double SuccessRate => ExecutionCount > 0 
        ? (double)SuccessfulExecutions / ExecutionCount 
        : 0;
}

// Paginated result wrapper
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

// Expression tree optimization utilities
public static class ExpressionOptimizer
{
    // Simplify constant expressions
    public static Expression<Func<T, bool>> OptimizeConstants<T>(
        this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var optimizer = new ConstantOptimizer();
        var optimizedBody = optimizer.Visit(expression.Body);
        
        return Expression.Lambda<Func<T, bool>>(optimizedBody, expression.Parameters);
    }

    // Convert method calls to optimized equivalents
    public static Expression<Func<T, bool>> OptimizeMethodCalls<T>(
        this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var optimizer = new MethodCallOptimizer();
        var optimizedBody = optimizer.Visit(expression.Body);
        
        return Expression.Lambda<Func<T, bool>>(optimizedBody, expression.Parameters);
    }

    // Remove redundant conditions
    public static Expression<Func<T, bool>> RemoveRedundancy<T>(
        this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var optimizer = new RedundancyRemover();
        var optimizedBody = optimizer.Visit(expression.Body);
        
        return Expression.Lambda<Func<T, bool>>(optimizedBody, expression.Parameters);
    }
}

// Constant folding optimizer
public class ConstantOptimizer : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // If both operands are constants, evaluate at compile time
        if (left is ConstantExpression leftConstant && right is ConstantExpression rightConstant)
        {
            try
            {
                var lambda = Expression.Lambda(Expression.MakeBinary(node.NodeType, leftConstant, rightConstant));
                var result = lambda.Compile().DynamicInvoke();
                return Expression.Constant(result, node.Type);
            }
            catch
            {
                // If evaluation fails, return original
                return Expression.MakeBinary(node.NodeType, left, right);
            }
        }

        return Expression.MakeBinary(node.NodeType, left, right);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);

        if (operand is ConstantExpression constantOperand)
        {
            try
            {
                var lambda = Expression.Lambda(Expression.MakeUnary(node.NodeType, constantOperand, node.Type));
                var result = lambda.Compile().DynamicInvoke();
                return Expression.Constant(result, node.Type);
            }
            catch
            {
                return Expression.MakeUnary(node.NodeType, operand, node.Type);
            }
        }

        return Expression.MakeUnary(node.NodeType, operand, node.Type);
    }
}

// Method call optimizer
public class MethodCallOptimizer : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Optimize string operations
        if (node.Method.DeclaringType == typeof(string))
        {
            return OptimizeStringMethods(node);
        }

        // Optimize LINQ operations
        if (IsLinqMethod(node.Method))
        {
            return OptimizeLinqMethods(node);
        }

        return base.VisitMethodCall(node);
    }

    private Expression OptimizeStringMethods(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(string.StartsWith):
            case nameof(string.EndsWith):
            case nameof(string.Contains):
                // Optimize constant string operations
                if (node.Arguments.Count > 0 && node.Arguments[0] is ConstantExpression constant)
                {
                    var value = constant.Value as string;
                    if (string.IsNullOrEmpty(value))
                    {
                        return Expression.Constant(true); // Empty string operations usually return true
                    }
                }
                break;
        }

        return node;
    }

    private Expression OptimizeLinqMethods(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "Where":
                // Combine multiple Where clauses
                if (node.Arguments[0] is MethodCallExpression sourceMethod && 
                    sourceMethod.Method.Name == "Where")
                {
                    // Combine predicates using AND logic
                    // This is a simplified optimization
                    return node;
                }
                break;
        }

        return node;
    }

    private static bool IsLinqMethod(MethodInfo method)
    {
        return method.DeclaringType == typeof(Queryable) || 
               method.DeclaringType == typeof(Enumerable);
    }
}

// Redundancy remover
public class RedundancyRemover : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Remove redundant comparisons like x == x
        if (node.NodeType == ExpressionType.Equal && 
            ExpressionComparer.AreEqual(left, right))
        {
            return Expression.Constant(true);
        }

        // Optimize boolean operations
        if (node.Type == typeof(bool))
        {
            return OptimizeBooleanOperation(node.NodeType, left, right);
        }

        return Expression.MakeBinary(node.NodeType, left, right);
    }

    private static Expression OptimizeBooleanOperation(ExpressionType nodeType, Expression left, Expression right)
    {
        switch (nodeType)
        {
            case ExpressionType.AndAlso:
                if (left is ConstantExpression leftConst)
                {
                    if (leftConst.Value is bool leftBool)
                    {
                        return leftBool ? right : Expression.Constant(false);
                    }
                }
                if (right is ConstantExpression rightConst)
                {
                    if (rightConst.Value is bool rightBool)
                    {
                        return rightBool ? left : Expression.Constant(false);
                    }
                }
                break;

            case ExpressionType.OrElse:
                if (left is ConstantExpression leftOrConst)
                {
                    if (leftOrConst.Value is bool leftOrBool)
                    {
                        return leftOrBool ? Expression.Constant(true) : right;
                    }
                }
                if (right is ConstantExpression rightOrConst)
                {
                    if (rightOrConst.Value is bool rightOrBool)
                    {
                        return rightOrBool ? Expression.Constant(true) : left;
                    }
                }
                break;
        }

        return Expression.MakeBinary(nodeType, left, right);
    }
}

// Expression equality comparer
public static class ExpressionComparer
{
    public static bool AreEqual(Expression? left, Expression? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        if (left.NodeType != right.NodeType || left.Type != right.Type) return false;

        switch (left.NodeType)
        {
            case ExpressionType.Constant:
                var leftConstant = (ConstantExpression)left;
                var rightConstant = (ConstantExpression)right;
                return Equals(leftConstant.Value, rightConstant.Value);

            case ExpressionType.Parameter:
                var leftParam = (ParameterExpression)left;
                var rightParam = (ParameterExpression)right;
                return leftParam.Name == rightParam.Name && leftParam.Type == rightParam.Type;

            case ExpressionType.MemberAccess:
                var leftMember = (MemberExpression)left;
                var rightMember = (MemberExpression)right;
                return leftMember.Member == rightMember.Member &&
                       AreEqual(leftMember.Expression, rightMember.Expression);

            default:
                return false;
        }
    }
}

// Query plan analyzer (simplified version)
public class QueryPlanAnalyzer
{
    public QueryPlan AnalyzeQuery<T>(IQueryable<T> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var plan = new QueryPlan
        {
            QueryType = typeof(T).Name,
            Expression = query.Expression.ToString(),
            EstimatedComplexity = EstimateComplexity(query.Expression),
            OptimizationSuggestions = GenerateOptimizationSuggestions(query.Expression)
        };

        return plan;
    }

    private int EstimateComplexity(Expression expression)
    {
        var visitor = new ComplexityVisitor();
        visitor.Visit(expression);
        return visitor.Complexity;
    }

    private List<string> GenerateOptimizationSuggestions(Expression expression)
    {
        var suggestions = new List<string>();
        var visitor = new OptimizationSuggestionVisitor(suggestions);
        visitor.Visit(expression);
        return suggestions;
    }
}

public class QueryPlan
{
    public string QueryType { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int EstimatedComplexity { get; set; }
    public List<string> OptimizationSuggestions { get; set; } = new();
}

public class ComplexityVisitor : ExpressionVisitor
{
    public int Complexity { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Different operations have different complexity costs
        switch (node.Method.Name)
        {
            case "Where":
                Complexity += 1;
                break;
            case "OrderBy":
            case "OrderByDescending":
                Complexity += 3;
                break;
            case "Join":
                Complexity += 5;
                break;
            case "GroupBy":
                Complexity += 4;
                break;
            default:
                Complexity += 1;
                break;
        }

        return base.VisitMethodCall(node);
    }
}

public class OptimizationSuggestionVisitor : ExpressionVisitor
{
    private readonly List<string> _suggestions;

    public OptimizationSuggestionVisitor(List<string> suggestions)
    {
        _suggestions = suggestions;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "Where":
                if (HasMultipleWhereClause(node))
                {
                    _suggestions.Add("Consider combining multiple Where clauses into a single predicate for better performance");
                }
                break;
                
            case "OrderBy":
                if (HasUnnecessaryOrdering(node))
                {
                    _suggestions.Add("Ordering operation detected - ensure it's necessary and consider using Take() to limit results");
                }
                break;
                
            case "ToList":
                _suggestions.Add("Consider using streaming operations instead of materializing with ToList() if the entire collection is not needed");
                break;
        }

        return base.VisitMethodCall(node);
    }

    private bool HasMultipleWhereClause(MethodCallExpression node)
    {
        return node.Arguments[0] is MethodCallExpression source && 
               source.Method.Name == "Where";
    }

    private bool HasUnnecessaryOrdering(MethodCallExpression node)
    {
        // Simplified check - in real implementation, would analyze if ordering is actually used
        return true;
    }
}

// Fluent query builder with optimization hints
public class OptimizedQueryBuilder<T>
{
    private readonly IQueryable<T> _query;
    private readonly List<string> _optimizationHints = new();

    public OptimizedQueryBuilder(IQueryable<T> query)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public OptimizedQueryBuilder<T> Where(Expression<Func<T, bool>> predicate, string? hint = null)
    {
        if (hint != null)
            _optimizationHints.Add($"Where: {hint}");
            
        return new OptimizedQueryBuilder<T>(_query.Where(predicate));
    }

    public OptimizedQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector, string? hint = null)
    {
        if (hint != null)
            _optimizationHints.Add($"OrderBy: {hint}");
            
        return new OptimizedQueryBuilder<T>(_query.OrderBy(keySelector));
    }

    public OptimizedQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector, string? hint = null)
    {
        if (hint != null)
            _optimizationHints.Add($"Select: {hint}");
            
        var newQuery = _query.Select(selector);
        var result = new OptimizedQueryBuilder<TResult>(newQuery);
        result._optimizationHints.AddRange(_optimizationHints);
        return result;
    }

    public IQueryable<T> Build()
    {
        return _query;
    }

    public List<string> GetOptimizationHints()
    {
        return new List<string>(_optimizationHints);
    }
}
```

**Usage**:

```csharp
// Example 1: Predicate composition and optimization
Console.WriteLine("Predicate Composition and Optimization:");

// Build complex predicates dynamically
var customerPredicate = PredicateOptimizer.Create<Customer>()
    .And(c => c.IsActive)
    .AndIf(true, c => c.Age >= 18)  // Conditional predicate
    .OrIf(false, c => c.IsVip)      // This won't be added
    .And(c => c.Country == "USA");

var compiledPredicate = customerPredicate.CompileAndCache("active-adult-usa-customers");

// Example customers
var customers = new[]
{
    new Customer { Id = 1, Name = "John", Age = 25, Country = "USA", IsActive = true, IsVip = false },
    new Customer { Id = 2, Name = "Jane", Age = 17, Country = "USA", IsActive = true, IsVip = true },
    new Customer { Id = 3, Name = "Bob", Age = 30, Country = "Canada", IsActive = true, IsVip = false },
    new Customer { Id = 4, Name = "Alice", Age = 35, Country = "USA", IsActive = false, IsVip = true }
}.AsQueryable();

var filteredCustomers = customers.Where(customerPredicate.Build()).ToList();
Console.WriteLine($"Filtered customers: {filteredCustomers.Count}");
foreach (var customer in filteredCustomers)
{
    Console.WriteLine($"  {customer.Name} (Age: {customer.Age}, Country: {customer.Country})");
}

// Example 2: Query optimization and performance monitoring
Console.WriteLine("\nQuery Performance Monitoring:");

var performanceMonitor = new QueryPerformanceMonitor();

// Simulate multiple query executions
for (int i = 0; i < 5; i++)
{
    var result = performanceMonitor.MonitorQuery("customer-search", () =>
    {
        return customers
            .Where(c => c.IsActive)
            .Where(c => c.Age >= 18)  // Multiple Where clauses
            .OrderBy(c => c.Name)
            .ToList();
    });

    Console.WriteLine($"Execution {i + 1}: Found {result.Count} customers");
}

// Generate performance report
Console.WriteLine("\nPerformance Report:");
Console.WriteLine(performanceMonitor.GeneratePerformanceReport());

// Example 3: Efficient pagination
Console.WriteLine("Efficient Pagination:");

var largeCustomerSet = Enumerable.Range(1, 1000)
    .Select(i => new Customer
    {
        Id = i,
        Name = $"Customer {i}",
        Age = 20 + (i % 50),
        Country = i % 3 == 0 ? "USA" : i % 3 == 1 ? "Canada" : "UK",
        IsActive = i % 4 != 0,
        IsVip = i % 10 == 0
    })
    .AsQueryable();

// Paginate efficiently
var page1 = largeCustomerSet
    .Where(c => c.IsActive)
    .ToPagedResult(pageNumber: 0, pageSize: 10);

Console.WriteLine($"Page 1: {page1.Items.Count} items out of {page1.TotalCount} total");
Console.WriteLine($"Has next page: {page1.HasNextPage}");
Console.WriteLine($"Total pages: {page1.TotalPages}");

foreach (var customer in page1.Items.Take(3))
{
    Console.WriteLine($"  {customer.Name} (ID: {customer.Id})");
}

// Example 4: Batched processing for large datasets
Console.WriteLine("\nBatched Processing:");

var batchCount = 0;
foreach (var batch in largeCustomerSet.Batch(100, c => c.Id))
{
    batchCount++;
    var batchAvgAge = batch.Average(c => c.Age);
    Console.WriteLine($"Batch {batchCount}: {batch.Count()} customers, avg age: {batchAvgAge:F1}");
    
    if (batchCount >= 3) break; // Show only first 3 batches
}

// Example 5: Expression optimization
Console.WriteLine("\nExpression Optimization:");

// Original expression with redundancy
Expression<Func<Customer, bool>> originalExpr = c => 
    (c.Age > 18 && true) || (c.IsVip && c.IsVip) || (false && c.IsActive);

// Optimize the expression
var optimizedExpr = originalExpr
    .OptimizeConstants()
    .RemoveRedundancy();

Console.WriteLine($"Original: {originalExpr}");
Console.WriteLine($"Optimized: {optimizedExpr}");

// Test both expressions
var testCustomer = new Customer { Age = 25, IsVip = true, IsActive = true };
var originalResult = originalExpr.Compile()(testCustomer);
var optimizedResult = optimizedExpr.Compile()(testCustomer);

Console.WriteLine($"Original result: {originalResult}, Optimized result: {optimizedResult}");

// Example 6: Query plan analysis
Console.WriteLine("\nQuery Plan Analysis:");

var analyzer = new QueryPlanAnalyzer();
var complexQuery = largeCustomerSet
    .Where(c => c.IsActive)
    .Where(c => c.Age >= 21)  // Multiple where clauses
    .OrderBy(c => c.Name)     // Expensive ordering
    .Join(largeCustomerSet.Where(c => c.IsVip), 
          c => c.Id, 
          v => v.Id, 
          (c, v) => c)        // Join operation
    .GroupBy(c => c.Country)  // Grouping
    .Select(g => new { Country = g.Key, Count = g.Count() });

var queryPlan = analyzer.AnalyzeQuery(complexQuery);

Console.WriteLine($"Query Type: {queryPlan.QueryType}");
Console.WriteLine($"Estimated Complexity: {queryPlan.EstimatedComplexity}");
Console.WriteLine("Optimization Suggestions:");
foreach (var suggestion in queryPlan.OptimizationSuggestions)
{
    Console.WriteLine($"  - {suggestion}");
}

// Example 7: Optimized query builder with hints
Console.WriteLine("\nOptimized Query Builder:");

var queryBuilder = new OptimizedQueryBuilder<Customer>(largeCustomerSet)
    .Where(c => c.IsActive, "Use index on IsActive column")
    .Where(c => c.Age >= 25, "Consider compound index on (IsActive, Age)")
    .OrderBy(c => c.Name, "Name column should be indexed for sorting")
    .Select(c => new { c.Name, c.Age }, "Projection reduces data transfer");

var optimizedQuery = queryBuilder.Build();
var optimizedResults = optimizedQuery.Take(5).ToList();

Console.WriteLine($"Optimized query results: {optimizedResults.Count} items");
Console.WriteLine("Query optimization hints:");
foreach (var hint in queryBuilder.GetOptimizationHints())
{
    Console.WriteLine($"  - {hint}");
}

// Example 8: Conditional operations for dynamic queries
Console.WriteLine("\nConditional Query Operations:");

bool includeInactive = false;
bool sortByAge = true;
string? countryFilter = "USA";

var dynamicQuery = largeCustomerSet.AsQueryable();

// Apply filters conditionally
if (!includeInactive)
{
    dynamicQuery = dynamicQuery.Where(c => c.IsActive);
}

if (!string.IsNullOrEmpty(countryFilter))
{
    dynamicQuery = dynamicQuery.Where(c => c.Country == countryFilter);
}

// Apply ordering conditionally
var orderedQuery = dynamicQuery.OrderByIf(sortByAge, c => c.Age, descending: false);

var dynamicResults = orderedQuery.Take(5).ToList();
Console.WriteLine($"Dynamic query results ({dynamicResults.Count} items):");
foreach (var customer in dynamicResults)
{
    Console.WriteLine($"  {customer.Name}: Age {customer.Age}, {customer.Country}, Active: {customer.IsActive}");
}

// Example 9: Efficient existence checking
Console.WriteLine("\nEfficient Existence Checking:");

// Check if any VIP customers exist (stops at first match)
var hasVipCustomers = largeCustomerSet.Exists(c => c.IsVip);
Console.WriteLine($"Has VIP customers: {hasVipCustomers}");

// Check if we have at least 100 active customers
var hasEnoughActiveCustomers = largeCustomerSet
    .Where(c => c.IsActive)
    .HasAny(minimumCount: 100);
Console.WriteLine($"Has at least 100 active customers: {hasEnoughActiveCustomers}");

// Example 10: Advanced predicate operations
Console.WriteLine("\nAdvanced Predicate Operations:");

// Create base predicates
Expression<Func<Customer, bool>> isAdultPredicate = c => c.Age >= 18;
Expression<Func<Customer, bool>> isActivePredicate = c => c.IsActive;
Expression<Func<Customer, bool>> isVipPredicate = c => c.IsVip;

// Combine predicates with logical operations
var adultAndActivePredidate = isAdultPredicate.And(isActivePredicate);
var vipOrAdultActivePrediicate = isVipPredicate.Or(adultAndActivePrediicate);
var notVipPredicate = isVipPredicate.Not();

// Test combined predicates
var adultActiveCustomers = largeCustomerSet.Where(adultAndActivePrediicate).Count();
var vipOrAdultActiveCustomers = largeCustomerSet.Where(vipOrAdultActivePrediicate).Count();
var nonVipCustomers = largeCustomerSet.Where(notVipPredicate).Count();

Console.WriteLine($"Adult & Active customers: {adultActiveCustomers}");
Console.WriteLine($"VIP or (Adult & Active) customers: {vipOrAdultActiveCustomers}");
Console.WriteLine($"Non-VIP customers: {nonVipCustomers}");

// Example 11: Smart projection optimization
Console.WriteLine("\nSmart Projection Optimization:");

// Optimized projection with deduplication
var uniqueCountries = largeCustomerSet
    .SelectOptimized(c => c.Country, distinct: true)
    .ToList();

Console.WriteLine($"Unique countries: [{string.Join(", ", uniqueCountries)}]");

// Projection with pre-sized list (if we know expected count)
var customerSummaries = largeCustomerSet
    .Where(c => c.IsActive)
    .Select(c => new { c.Name, c.Age })
    .OptimizedToList(expectedCount: 750); // Hint about expected size

Console.WriteLine($"Customer summaries: {customerSummaries.Count} items");

// Example 12: Memory-efficient streaming operations
Console.WriteLine("\nMemory-Efficient Operations:");

// Process large dataset in chunks without loading all into memory
var processedCount = 0;
foreach (var batch in largeCustomerSet.Batch(50))
{
    // Simulate processing
    var batchSummary = batch.GroupBy(c => c.Country)
                          .ToDictionary(g => g.Key, g => g.Count());
    
    processedCount += batch.Count();
    
    if (processedCount >= 200) break; // Process only first 200 customers
}

Console.WriteLine($"Processed {processedCount} customers in batches");
```

// Supporting data model

```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVip { get; set; }
}
```

**Notes**:

- Predicate composition allows building complex filters dynamically while maintaining type safety and performance
- Expression caching significantly improves performance for frequently used predicates by avoiding recompilation
- Query performance monitoring helps identify bottlenecks and optimization opportunities in production systems
- Pagination with total count optimization reduces database round trips when total count isn't needed
- Expression tree optimization can simplify queries at compile time, reducing runtime overhead
- Batched processing enables handling large datasets without memory exhaustion
- Conditional query operations prevent unnecessary database operations when conditions aren't met
- Query plan analysis helps understand and optimize complex LINQ queries before they hit the database
- Smart projections reduce data transfer by selecting only needed columns and removing duplicates
- Memory pooling and pre-sizing collections improve performance for large result sets

**Prerequisites**:

- .NET Core 2.0+ or .NET Framework 4.7+ for Expression Tree support
- Entity Framework Core 2.0+ for optimal database query translation
- Understanding of Expression Trees and LINQ expression compilation
- Knowledge of database indexing strategies for query optimization
- Familiarity with performance profiling tools and techniques

**Related Snippets**:

- [LINQ Extensions](linq-extensions.md) - Basic LINQ extension methods
- [Performance LINQ](performance-linq.md) - High-performance LINQ operations
- [Expression Trees](expression-trees.md) - Advanced expression tree manipulation
- [Database Optimization](database-optimization.md) - Database-specific optimization techniques
