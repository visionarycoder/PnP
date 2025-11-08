namespace JavaScript.AsyncPatterns;

/// <summary>
/// Demonstrates JavaScript async/await and Promise patterns
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("JavaScript Async Patterns Examples");
        Console.WriteLine("=================================");
        
        BasicPromisePatterns();
        AsyncAwaitPatterns();
        ErrorHandlingPatterns();
        ConcurrentOperations();
        AdvancedAsyncPatterns();
        PromiseUtilities();
    }

    /// <summary>
    /// Basic Promise patterns and creation
    /// </summary>
    private static void BasicPromisePatterns()
    {
        Console.WriteLine("\n1. Basic Promise Patterns:");
        
        Console.WriteLine("// Creating a Promise");
        Console.WriteLine("const delay = (ms) => new Promise(resolve => {");
        Console.WriteLine("  setTimeout(resolve, ms);");
        Console.WriteLine("});");
        
        Console.WriteLine("\n// Promise.resolve() and Promise.reject()");
        Console.WriteLine("const immediateResolve = Promise.resolve('Success');");
        Console.WriteLine("const immediateReject = Promise.reject(new Error('Failed'));");
        
        Console.WriteLine("\n// Chaining Promises");
        Console.WriteLine("fetchUser(id)");
        Console.WriteLine("  .then(user => fetchUserPosts(user.id))");
        Console.WriteLine("  .then(posts => posts.filter(post => post.published))");
        Console.WriteLine("  .then(publishedPosts => console.log(publishedPosts))");
        Console.WriteLine("  .catch(error => console.error('Error:', error));");
    }

    /// <summary>
    /// Async/await patterns and best practices
    /// </summary>
    private static void AsyncAwaitPatterns()
    {
        Console.WriteLine("\n2. Async/Await Patterns:");
        
        Console.WriteLine("// Basic async function");
        Console.WriteLine("async function fetchUserData(userId) {");
        Console.WriteLine("  const user = await fetchUser(userId);");
        Console.WriteLine("  const posts = await fetchUserPosts(userId);");
        Console.WriteLine("  return { user, posts };");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Async arrow functions");
        Console.WriteLine("const processData = async (data) => {");
        Console.WriteLine("  const processed = await transform(data);");
        Console.WriteLine("  return processed;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Async IIFE (Immediately Invoked Function Expression)");
        Console.WriteLine("(async () => {");
        Console.WriteLine("  try {");
        Console.WriteLine("    const result = await someAsyncOperation();");
        Console.WriteLine("    console.log(result);");
        Console.WriteLine("  } catch (error) {");
        Console.WriteLine("    console.error(error);");
        Console.WriteLine("  }");
        Console.WriteLine("})();");
    }

    /// <summary>
    /// Error handling patterns for async operations
    /// </summary>
    private static void ErrorHandlingPatterns()
    {
        Console.WriteLine("\n3. Error Handling Patterns:");
        
        Console.WriteLine("// Try-catch with async/await");
        Console.WriteLine("async function safeApiCall(url) {");
        Console.WriteLine("  try {");
        Console.WriteLine("    const response = await fetch(url);");
        Console.WriteLine("    ");
        Console.WriteLine("    if (!response.ok) {");
        Console.WriteLine("      throw new Error(`HTTP ${response.status}: ${response.statusText}`);");
        Console.WriteLine("    }");
        Console.WriteLine("    ");
        Console.WriteLine("    return await response.json();");
        Console.WriteLine("  } catch (error) {");
        Console.WriteLine("    console.error('API call failed:', error);");
        Console.WriteLine("    throw error; // Re-throw for caller to handle");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Error handling with Promise.catch()");
        Console.WriteLine("fetchData(url)");
        Console.WriteLine("  .catch(error => {");
        Console.WriteLine("    if (error.name === 'NetworkError') {");
        Console.WriteLine("      return getFromCache(url);");
        Console.WriteLine("    }");
        Console.WriteLine("    throw error;");
        Console.WriteLine("  });");
        
        Console.WriteLine("\n// Graceful error handling");
        Console.WriteLine("async function robustOperation() {");
        Console.WriteLine("  const results = await Promise.allSettled([");
        Console.WriteLine("    operation1(),");
        Console.WriteLine("    operation2(),");
        Console.WriteLine("    operation3()");
        Console.WriteLine("  ]);");
        Console.WriteLine("  ");
        Console.WriteLine("  const successful = results");
        Console.WriteLine("    .filter(result => result.status === 'fulfilled')");
        Console.WriteLine("    .map(result => result.value);");
        Console.WriteLine("  ");
        Console.WriteLine("  return successful;");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Concurrent and parallel async operations
    /// </summary>
    private static void ConcurrentOperations()
    {
        Console.WriteLine("\n4. Concurrent Operations:");
        
        Console.WriteLine("// Promise.all - All must succeed");
        Console.WriteLine("async function fetchAllData() {");
        Console.WriteLine("  const [users, posts, comments] = await Promise.all([");
        Console.WriteLine("    fetchUsers(),");
        Console.WriteLine("    fetchPosts(),");
        Console.WriteLine("    fetchComments()");
        Console.WriteLine("  ]);");
        Console.WriteLine("  ");
        Console.WriteLine("  return { users, posts, comments };");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Promise.allSettled - Handle mixed success/failure");
        Console.WriteLine("async function fetchOptionalData() {");
        Console.WriteLine("  const results = await Promise.allSettled([");
        Console.WriteLine("    fetchCriticalData(),");
        Console.WriteLine("    fetchOptionalData1(),");
        Console.WriteLine("    fetchOptionalData2()");
        Console.WriteLine("  ]);");
        Console.WriteLine("  ");
        Console.WriteLine("  return processResults(results);");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Promise.race - First to complete wins");
        Console.WriteLine("async function fetchWithTimeout(url, timeout = 5000) {");
        Console.WriteLine("  const timeoutPromise = new Promise((_, reject) => {");
        Console.WriteLine("    setTimeout(() => reject(new Error('Timeout')), timeout);");
        Console.WriteLine("  });");
        Console.WriteLine("  ");
        Console.WriteLine("  return Promise.race([");
        Console.WriteLine("    fetch(url),");
        Console.WriteLine("    timeoutPromise");
        Console.WriteLine("  ]);");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Advanced async patterns
    /// </summary>
    private static void AdvancedAsyncPatterns()
    {
        Console.WriteLine("\n5. Advanced Async Patterns:");
        
        Console.WriteLine("// Retry pattern");
        Console.WriteLine("async function retry(operation, maxAttempts = 3, delay = 1000) {");
        Console.WriteLine("  for (let attempt = 1; attempt <= maxAttempts; attempt++) {");
        Console.WriteLine("    try {");
        Console.WriteLine("      return await operation();");
        Console.WriteLine("    } catch (error) {");
        Console.WriteLine("      if (attempt === maxAttempts) throw error;");
        Console.WriteLine("      ");
        Console.WriteLine("      console.log(`Attempt ${attempt} failed, retrying...`);");
        Console.WriteLine("      await new Promise(resolve => setTimeout(resolve, delay));");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Sequential processing");
        Console.WriteLine("async function processSequentially(items, processor) {");
        Console.WriteLine("  const results = [];");
        Console.WriteLine("  ");
        Console.WriteLine("  for (const item of items) {");
        Console.WriteLine("    const result = await processor(item);");
        Console.WriteLine("    results.push(result);");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  return results;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Batch processing with concurrency limit");
        Console.WriteLine("async function processBatches(items, processor, batchSize = 5) {");
        Console.WriteLine("  const results = [];");
        Console.WriteLine("  ");
        Console.WriteLine("  for (let i = 0; i < items.length; i += batchSize) {");
        Console.WriteLine("    const batch = items.slice(i, i + batchSize);");
        Console.WriteLine("    const batchResults = await Promise.all(");
        Console.WriteLine("      batch.map(processor)");
        Console.WriteLine("    );");
        Console.WriteLine("    results.push(...batchResults);");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  return results;");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Utility functions for Promise handling
    /// </summary>
    private static void PromiseUtilities()
    {
        Console.WriteLine("\n6. Promise Utilities:");
        
        Console.WriteLine("// Timeout wrapper");
        Console.WriteLine("function withTimeout(promise, ms) {");
        Console.WriteLine("  const timeout = new Promise((_, reject) => {");
        Console.WriteLine("    setTimeout(() => reject(new Error('Timeout')), ms);");
        Console.WriteLine("  });");
        Console.WriteLine("  ");
        Console.WriteLine("  return Promise.race([promise, timeout]);");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Promisify callback-based functions");
        Console.WriteLine("function promisify(fn) {");
        Console.WriteLine("  return (...args) => {");
        Console.WriteLine("    return new Promise((resolve, reject) => {");
        Console.WriteLine("      fn(...args, (error, result) => {");
        Console.WriteLine("        if (error) reject(error);");
        Console.WriteLine("        else resolve(result);");
        Console.WriteLine("      });");
        Console.WriteLine("    });");
        Console.WriteLine("  };");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Async queue for rate limiting");
        Console.WriteLine("class AsyncQueue {");
        Console.WriteLine("  constructor(concurrency = 1) {");
        Console.WriteLine("    this.concurrency = concurrency;");
        Console.WriteLine("    this.running = 0;");
        Console.WriteLine("    this.queue = [];");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  async add(task) {");
        Console.WriteLine("    return new Promise((resolve, reject) => {");
        Console.WriteLine("      this.queue.push({ task, resolve, reject });");
        Console.WriteLine("      this.process();");
        Console.WriteLine("    });");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  async process() {");
        Console.WriteLine("    if (this.running >= this.concurrency || this.queue.length === 0) {");
        Console.WriteLine("      return;");
        Console.WriteLine("    }");
        Console.WriteLine("    ");
        Console.WriteLine("    this.running++;");
        Console.WriteLine("    const { task, resolve, reject } = this.queue.shift();");
        Console.WriteLine("    ");
        Console.WriteLine("    try {");
        Console.WriteLine("      const result = await task();");
        Console.WriteLine("      resolve(result);");
        Console.WriteLine("    } catch (error) {");
        Console.WriteLine("      reject(error);");
        Console.WriteLine("    } finally {");
        Console.WriteLine("      this.running--;");
        Console.WriteLine("      this.process();");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("}");
    }
}