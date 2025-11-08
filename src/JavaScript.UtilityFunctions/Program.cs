namespace JavaScript.UtilityFunctions;

/// <summary>
/// Demonstrates JavaScript utility functions and functional programming patterns
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("JavaScript Utility Functions Examples");
        Console.WriteLine("====================================");
        
        DataValidationUtilities();
        StringUtilities();
        ArrayUtilities();
        ObjectUtilities();
        DateTimeUtilities();
        FunctionalProgrammingUtilities();
        PerformanceUtilities();
    }

    /// <summary>
    /// Data validation utility functions
    /// </summary>
    private static void DataValidationUtilities()
    {
        Console.WriteLine("\n1. Data Validation Utilities:");
        
        Console.WriteLine("// Email validation");
        Console.WriteLine("const isValidEmail = (email) => {");
        Console.WriteLine("  const emailRegex = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/;");
        Console.WriteLine("  return emailRegex.test(email);");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// URL validation");
        Console.WriteLine("const isValidUrl = (url) => {");
        Console.WriteLine("  try {");
        Console.WriteLine("    new URL(url);");
        Console.WriteLine("    return true;");
        Console.WriteLine("  } catch {");
        Console.WriteLine("    return false;");
        Console.WriteLine("  }");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Type checking utilities");
        Console.WriteLine("const isString = (value) => typeof value === 'string';");
        Console.WriteLine("const isNumber = (value) => typeof value === 'number' && !isNaN(value);");
        Console.WriteLine("const isArray = Array.isArray;");
        Console.WriteLine("const isObject = (value) => value !== null && typeof value === 'object' && !isArray(value);");
        Console.WriteLine("const isEmpty = (value) => {");
        Console.WriteLine("  if (value == null) return true;");
        Console.WriteLine("  if (isArray(value) || isString(value)) return value.length === 0;");
        Console.WriteLine("  if (isObject(value)) return Object.keys(value).length === 0;");
        Console.WriteLine("  return false;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Range validation");
        Console.WriteLine("const inRange = (value, min, max) => value >= min && value <= max;");
        Console.WriteLine("const isPositiveInteger = (value) => Number.isInteger(value) && value > 0;");
    }

    /// <summary>
    /// String manipulation utilities
    /// </summary>
    private static void StringUtilities()
    {
        Console.WriteLine("\n2. String Utilities:");
        
        Console.WriteLine("// Case transformations");
        Console.WriteLine("const toCamelCase = (str) => {");
        Console.WriteLine("  return str.replace(/(?:^\\w|[A-Z]|\\b\\w)/g, (word, index) => {");
        Console.WriteLine("    return index === 0 ? word.toLowerCase() : word.toUpperCase();");
        Console.WriteLine("  }).replace(/\\s+/g, '');");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// String truncation with ellipsis");
        Console.WriteLine("const truncate = (str, length, suffix = '...') => {");
        Console.WriteLine("  return str.length <= length ? str : str.slice(0, length) + suffix;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Slug generation");
        Console.WriteLine("const slugify = (text) => {");
        Console.WriteLine("  return text");
        Console.WriteLine("    .toLowerCase()");
        Console.WriteLine("    .trim()");
        Console.WriteLine("    .replace(/[^\\w\\s-]/g, '')");
        Console.WriteLine("    .replace(/[\\s_-]+/g, '-')");
        Console.WriteLine("    .replace(/^-+|-+$/g, '');");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Template interpolation");
        Console.WriteLine("const interpolate = (template, data) => {");
        Console.WriteLine("  return template.replace(/\\{\\{(\\w+)\\}\\}/g, (match, key) => {");
        Console.WriteLine("    return data.hasOwnProperty(key) ? data[key] : match;");
        Console.WriteLine("  });");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// HTML escaping");
        Console.WriteLine("const escapeHtml = (text) => {");
        Console.WriteLine("  const map = {");
        Console.WriteLine("    '&': '&amp;',");
        Console.WriteLine("    '<': '&lt;',");
        Console.WriteLine("    '>': '&gt;',");
        Console.WriteLine("    '\"': '&quot;',");
        Console.WriteLine("    \"'\": '&#039;'");
        Console.WriteLine("  };");
        Console.WriteLine("  return text.replace(/[&<>\"']/g, (m) => map[m]);");
        Console.WriteLine("};");
    }

    /// <summary>
    /// Array manipulation utilities
    /// </summary>
    private static void ArrayUtilities()
    {
        Console.WriteLine("\n3. Array Utilities:");
        
        Console.WriteLine("// Array deduplication");
        Console.WriteLine("const unique = (arr) => [...new Set(arr)];");
        Console.WriteLine("const uniqueBy = (arr, key) => {");
        Console.WriteLine("  const seen = new Set();");
        Console.WriteLine("  return arr.filter(item => {");
        Console.WriteLine("    const value = typeof key === 'function' ? key(item) : item[key];");
        Console.WriteLine("    if (seen.has(value)) return false;");
        Console.WriteLine("    seen.add(value);");
        Console.WriteLine("    return true;");
        Console.WriteLine("  });");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Array grouping");
        Console.WriteLine("const groupBy = (arr, key) => {");
        Console.WriteLine("  return arr.reduce((groups, item) => {");
        Console.WriteLine("    const group = typeof key === 'function' ? key(item) : item[key];");
        Console.WriteLine("    groups[group] = groups[group] || [];");
        Console.WriteLine("    groups[group].push(item);");
        Console.WriteLine("    return groups;");
        Console.WriteLine("  }, {});");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Array chunking");
        Console.WriteLine("const chunk = (arr, size) => {");
        Console.WriteLine("  const chunks = [];");
        Console.WriteLine("  for (let i = 0; i < arr.length; i += size) {");
        Console.WriteLine("    chunks.push(arr.slice(i, i + size));");
        Console.WriteLine("  }");
        Console.WriteLine("  return chunks;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Array intersection and difference");
        Console.WriteLine("const intersection = (arr1, arr2) => {");
        Console.WriteLine("  const set2 = new Set(arr2);");
        Console.WriteLine("  return arr1.filter(item => set2.has(item));");
        Console.WriteLine("};");
        Console.WriteLine("const difference = (arr1, arr2) => {");
        Console.WriteLine("  const set2 = new Set(arr2);");
        Console.WriteLine("  return arr1.filter(item => !set2.has(item));");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Array shuffling");
        Console.WriteLine("const shuffle = (arr) => {");
        Console.WriteLine("  const shuffled = [...arr];");
        Console.WriteLine("  for (let i = shuffled.length - 1; i > 0; i--) {");
        Console.WriteLine("    const j = Math.floor(Math.random() * (i + 1));");
        Console.WriteLine("    [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];");
        Console.WriteLine("  }");
        Console.WriteLine("  return shuffled;");
        Console.WriteLine("};");
    }

    /// <summary>
    /// Object manipulation utilities
    /// </summary>
    private static void ObjectUtilities()
    {
        Console.WriteLine("\n4. Object Utilities:");
        
        Console.WriteLine("// Deep clone");
        Console.WriteLine("const deepClone = (obj) => {");
        Console.WriteLine("  if (obj === null || typeof obj !== 'object') return obj;");
        Console.WriteLine("  if (obj instanceof Date) return new Date(obj.getTime());");
        Console.WriteLine("  if (obj instanceof Array) return obj.map(item => deepClone(item));");
        Console.WriteLine("  ");
        Console.WriteLine("  const cloned = {};");
        Console.WriteLine("  for (const key in obj) {");
        Console.WriteLine("    if (obj.hasOwnProperty(key)) {");
        Console.WriteLine("      cloned[key] = deepClone(obj[key]);");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("  return cloned;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Deep merge");
        Console.WriteLine("const deepMerge = (target, source) => {");
        Console.WriteLine("  const result = { ...target };");
        Console.WriteLine("  ");
        Console.WriteLine("  for (const key in source) {");
        Console.WriteLine("    if (source.hasOwnProperty(key)) {");
        Console.WriteLine("      if (isObject(source[key]) && isObject(result[key])) {");
        Console.WriteLine("        result[key] = deepMerge(result[key], source[key]);");
        Console.WriteLine("      } else {");
        Console.WriteLine("        result[key] = source[key];");
        Console.WriteLine("      }");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("  return result;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Object path operations");
        Console.WriteLine("const get = (obj, path, defaultValue) => {");
        Console.WriteLine("  const keys = path.split('.');");
        Console.WriteLine("  let result = obj;");
        Console.WriteLine("  ");
        Console.WriteLine("  for (const key of keys) {");
        Console.WriteLine("    if (result == null || !result.hasOwnProperty(key)) {");
        Console.WriteLine("      return defaultValue;");
        Console.WriteLine("    }");
        Console.WriteLine("    result = result[key];");
        Console.WriteLine("  }");
        Console.WriteLine("  return result;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Object filtering");
        Console.WriteLine("const pick = (obj, keys) => {");
        Console.WriteLine("  return keys.reduce((result, key) => {");
        Console.WriteLine("    if (obj.hasOwnProperty(key)) result[key] = obj[key];");
        Console.WriteLine("    return result;");
        Console.WriteLine("  }, {});");
        Console.WriteLine("};");
        Console.WriteLine("const omit = (obj, keys) => {");
        Console.WriteLine("  const omitSet = new Set(keys);");
        Console.WriteLine("  return Object.keys(obj).reduce((result, key) => {");
        Console.WriteLine("    if (!omitSet.has(key)) result[key] = obj[key];");
        Console.WriteLine("    return result;");
        Console.WriteLine("  }, {});");
        Console.WriteLine("};");
    }

    /// <summary>
    /// Date and time utilities
    /// </summary>
    private static void DateTimeUtilities()
    {
        Console.WriteLine("\n5. Date/Time Utilities:");
        
        Console.WriteLine("// Date formatting");
        Console.WriteLine("const formatDate = (date, format = 'YYYY-MM-DD') => {");
        Console.WriteLine("  const d = new Date(date);");
        Console.WriteLine("  const year = d.getFullYear();");
        Console.WriteLine("  const month = String(d.getMonth() + 1).padStart(2, '0');");
        Console.WriteLine("  const day = String(d.getDate()).padStart(2, '0');");
        Console.WriteLine("  ");
        Console.WriteLine("  return format");
        Console.WriteLine("    .replace('YYYY', year)");
        Console.WriteLine("    .replace('MM', month)");
        Console.WriteLine("    .replace('DD', day);");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Time ago utility");
        Console.WriteLine("const timeAgo = (date) => {");
        Console.WriteLine("  const now = new Date();");
        Console.WriteLine("  const diff = now - new Date(date);");
        Console.WriteLine("  const seconds = Math.floor(diff / 1000);");
        Console.WriteLine("  ");
        Console.WriteLine("  if (seconds < 60) return 'just now';");
        Console.WriteLine("  const minutes = Math.floor(seconds / 60);");
        Console.WriteLine("  if (minutes < 60) return `${minutes}m ago`;");
        Console.WriteLine("  const hours = Math.floor(minutes / 60);");
        Console.WriteLine("  if (hours < 24) return `${hours}h ago`;");
        Console.WriteLine("  const days = Math.floor(hours / 24);");
        Console.WriteLine("  return `${days}d ago`;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Date range utilities");
        Console.WriteLine("const isDateInRange = (date, startDate, endDate) => {");
        Console.WriteLine("  const d = new Date(date);");
        Console.WriteLine("  return d >= new Date(startDate) && d <= new Date(endDate);");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Business days calculation");
        Console.WriteLine("const addBusinessDays = (date, days) => {");
        Console.WriteLine("  const result = new Date(date);");
        Console.WriteLine("  let remaining = days;");
        Console.WriteLine("  ");
        Console.WriteLine("  while (remaining > 0) {");
        Console.WriteLine("    result.setDate(result.getDate() + 1);");
        Console.WriteLine("    const dayOfWeek = result.getDay();");
        Console.WriteLine("    if (dayOfWeek !== 0 && dayOfWeek !== 6) { // Not weekend");
        Console.WriteLine("      remaining--;");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("  return result;");
        Console.WriteLine("};");
    }

    /// <summary>
    /// Functional programming utilities
    /// </summary>
    private static void FunctionalProgrammingUtilities()
    {
        Console.WriteLine("\n6. Functional Programming Utilities:");
        
        Console.WriteLine("// Function composition");
        Console.WriteLine("const compose = (...fns) => (value) => fns.reduceRight((acc, fn) => fn(acc), value);");
        Console.WriteLine("const pipe = (...fns) => (value) => fns.reduce((acc, fn) => fn(acc), value);");
        
        Console.WriteLine("\n// Currying");
        Console.WriteLine("const curry = (fn) => {");
        Console.WriteLine("  return function curried(...args) {");
        Console.WriteLine("    if (args.length >= fn.length) {");
        Console.WriteLine("      return fn.apply(this, args);");
        Console.WriteLine("    }");
        Console.WriteLine("    return (...nextArgs) => curried(...args, ...nextArgs);");
        Console.WriteLine("  };");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Partial application");
        Console.WriteLine("const partial = (fn, ...argsToApply) => {");
        Console.WriteLine("  return (...restArgs) => fn(...argsToApply, ...restArgs);");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Memoization");
        Console.WriteLine("const memoize = (fn) => {");
        Console.WriteLine("  const cache = new Map();");
        Console.WriteLine("  return (...args) => {");
        Console.WriteLine("    const key = JSON.stringify(args);");
        Console.WriteLine("    if (cache.has(key)) return cache.get(key);");
        Console.WriteLine("    ");
        Console.WriteLine("    const result = fn(...args);");
        Console.WriteLine("    cache.set(key, result);");
        Console.WriteLine("    return result;");
        Console.WriteLine("  };");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Maybe/Optional pattern");
        Console.WriteLine("class Maybe {");
        Console.WriteLine("  constructor(value) {");
        Console.WriteLine("    this.value = value;");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  static of(value) {");
        Console.WriteLine("    return new Maybe(value);");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  map(fn) {");
        Console.WriteLine("    return this.value == null ? Maybe.of(null) : Maybe.of(fn(this.value));");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  flatMap(fn) {");
        Console.WriteLine("    return this.value == null ? Maybe.of(null) : fn(this.value);");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  getOrElse(defaultValue) {");
        Console.WriteLine("    return this.value != null ? this.value : defaultValue;");
        Console.WriteLine("  }");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Performance and utility functions
    /// </summary>
    private static void PerformanceUtilities()
    {
        Console.WriteLine("\n7. Performance Utilities:");
        
        Console.WriteLine("// Performance measurement");
        Console.WriteLine("const benchmark = (fn, iterations = 1000) => {");
        Console.WriteLine("  const start = performance.now();");
        Console.WriteLine("  for (let i = 0; i < iterations; i++) {");
        Console.WriteLine("    fn();");
        Console.WriteLine("  }");
        Console.WriteLine("  const end = performance.now();");
        Console.WriteLine("  return end - start;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Lazy evaluation");
        Console.WriteLine("const lazy = (fn) => {");
        Console.WriteLine("  let cached = false;");
        Console.WriteLine("  let result;");
        Console.WriteLine("  ");
        Console.WriteLine("  return () => {");
        Console.WriteLine("    if (!cached) {");
        Console.WriteLine("      result = fn();");
        Console.WriteLine("      cached = true;");
        Console.WriteLine("    }");
        Console.WriteLine("    return result;");
        Console.WriteLine("  };");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Random utilities");
        Console.WriteLine("const randomInt = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min;");
        Console.WriteLine("const randomChoice = (arr) => arr[Math.floor(Math.random() * arr.length)];");
        Console.WriteLine("const uuid = () => {");
        Console.WriteLine("  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {");
        Console.WriteLine("    const r = Math.random() * 16 | 0;");
        Console.WriteLine("    const v = c === 'x' ? r : (r & 0x3 | 0x8);");
        Console.WriteLine("    return v.toString(16);");
        Console.WriteLine("  });");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// URL and query string utilities");
        Console.WriteLine("const parseQueryString = (url) => {");
        Console.WriteLine("  const params = new URLSearchParams(url.split('?')[1]);");
        Console.WriteLine("  return Object.fromEntries(params.entries());");
        Console.WriteLine("};");
        Console.WriteLine("const buildQueryString = (params) => {");
        Console.WriteLine("  return Object.entries(params)");
        Console.WriteLine("    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)");
        Console.WriteLine("    .join('&');");
        Console.WriteLine("};");
    }
}