namespace JavaScript.TypeScriptPatterns;

/// <summary>
/// Demonstrates TypeScript patterns, types, and advanced features
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("JavaScript TypeScript Patterns Examples");
        Console.WriteLine("======================================");
        
        BasicTypePatterns();
        InterfacePatterns();
        GenericPatterns();
        AdvancedTypes();
        DecoratorPatterns();
        UtilityTypes();
        ModulePatterns();
    }

    /// <summary>
    /// Basic TypeScript type patterns
    /// </summary>
    private static void BasicTypePatterns()
    {
        Console.WriteLine("\n1. Basic Type Patterns:");
        
        Console.WriteLine("// Primitive types");
        Console.WriteLine("let name: string = 'John';");
        Console.WriteLine("let age: number = 30;");
        Console.WriteLine("let isActive: boolean = true;");
        Console.WriteLine("let data: any = { dynamic: 'content' };");
        Console.WriteLine("let nothing: void = undefined;");
        Console.WriteLine("let nullable: null = null;");
        Console.WriteLine("let undefinedValue: undefined = undefined;");
        
        Console.WriteLine("\n// Array types");
        Console.WriteLine("let numbers: number[] = [1, 2, 3];");
        Console.WriteLine("let strings: Array<string> = ['a', 'b', 'c'];");
        Console.WriteLine("let mixed: (string | number)[] = [1, 'two', 3];");
        
        Console.WriteLine("\n// Tuple types");
        Console.WriteLine("let coordinate: [number, number] = [10, 20];");
        Console.WriteLine("let nameAndAge: [string, number] = ['Alice', 25];");
        Console.WriteLine("let variableTuple: [string, ...number[]] = ['label', 1, 2, 3];");
        
        Console.WriteLine("\n// Enum types");
        Console.WriteLine("enum Direction {");
        Console.WriteLine("  Up = 'UP',");
        Console.WriteLine("  Down = 'DOWN',");
        Console.WriteLine("  Left = 'LEFT',");
        Console.WriteLine("  Right = 'RIGHT'");
        Console.WriteLine("}");
        Console.WriteLine("let currentDirection: Direction = Direction.Up;");
        
        Console.WriteLine("\n// Function types");
        Console.WriteLine("type Calculator = (a: number, b: number) => number;");
        Console.WriteLine("const add: Calculator = (a, b) => a + b;");
        Console.WriteLine("const multiply: Calculator = (a, b) => a * b;");
    }

    /// <summary>
    /// Interface patterns and object typing
    /// </summary>
    private static void InterfacePatterns()
    {
        Console.WriteLine("\n2. Interface Patterns:");
        
        Console.WriteLine("// Basic interface");
        Console.WriteLine("interface User {");
        Console.WriteLine("  id: number;");
        Console.WriteLine("  name: string;");
        Console.WriteLine("  email: string;");
        Console.WriteLine("  age?: number; // Optional property");
        Console.WriteLine("  readonly createdAt: Date; // Read-only property");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Interface inheritance");
        Console.WriteLine("interface AdminUser extends User {");
        Console.WriteLine("  permissions: string[];");
        Console.WriteLine("  lastLogin?: Date;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Function interfaces");
        Console.WriteLine("interface EventHandler {");
        Console.WriteLine("  (event: Event): void;");
        Console.WriteLine("}");
        Console.WriteLine("interface EventHandlerObject {");
        Console.WriteLine("  handleEvent(event: Event): void;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Index signatures");
        Console.WriteLine("interface StringDictionary {");
        Console.WriteLine("  [key: string]: string;");
        Console.WriteLine("}");
        Console.WriteLine("interface NumberArray {");
        Console.WriteLine("  [index: number]: number;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Hybrid types (callable and indexable)");
        Console.WriteLine("interface Counter {");
        Console.WriteLine("  (start: number): string;");
        Console.WriteLine("  interval: number;");
        Console.WriteLine("  reset(): void;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Extending multiple interfaces");
        Console.WriteLine("interface Timestamped {");
        Console.WriteLine("  timestamp: Date;");
        Console.WriteLine("}");
        Console.WriteLine("interface Tagged {");
        Console.WriteLine("  tags: string[];");
        Console.WriteLine("}");
        Console.WriteLine("interface Document extends Timestamped, Tagged {");
        Console.WriteLine("  content: string;");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Generic type patterns
    /// </summary>
    private static void GenericPatterns()
    {
        Console.WriteLine("\n3. Generic Patterns:");
        
        Console.WriteLine("// Basic generic function");
        Console.WriteLine("function identity<T>(arg: T): T {");
        Console.WriteLine("  return arg;");
        Console.WriteLine("}");
        Console.WriteLine("let stringIdentity = identity<string>('hello');");
        Console.WriteLine("let numberIdentity = identity(42); // Type inference");
        
        Console.WriteLine("\n// Generic interfaces");
        Console.WriteLine("interface Repository<T> {");
        Console.WriteLine("  findById(id: string): Promise<T | null>;");
        Console.WriteLine("  save(entity: T): Promise<T>;");
        Console.WriteLine("  delete(id: string): Promise<void>;");
        Console.WriteLine("  findAll(): Promise<T[]>;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Generic classes");
        Console.WriteLine("class GenericCollection<T> {");
        Console.WriteLine("  private items: T[] = [];");
        Console.WriteLine("  ");
        Console.WriteLine("  add(item: T): void {");
        Console.WriteLine("    this.items.push(item);");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  get(index: number): T | undefined {");
        Console.WriteLine("    return this.items[index];");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  filter(predicate: (item: T) => boolean): T[] {");
        Console.WriteLine("    return this.items.filter(predicate);");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Generic constraints");
        Console.WriteLine("interface Lengthwise {");
        Console.WriteLine("  length: number;");
        Console.WriteLine("}");
        Console.WriteLine("function logLength<T extends Lengthwise>(arg: T): T {");
        Console.WriteLine("  console.log(arg.length);");
        Console.WriteLine("  return arg;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Keyof and indexed access types");
        Console.WriteLine("function getProperty<T, K extends keyof T>(obj: T, key: K): T[K] {");
        Console.WriteLine("  return obj[key];");
        Console.WriteLine("}");
        Console.WriteLine("const person = { name: 'Alice', age: 30 };");
        Console.WriteLine("const name = getProperty(person, 'name'); // Type: string");
        Console.WriteLine("const age = getProperty(person, 'age'); // Type: number");
        
        Console.WriteLine("\n// Conditional types");
        Console.WriteLine("type ApiResponse<T> = T extends string ? string : T extends number ? number : never;");
        Console.WriteLine("type NonNullable<T> = T extends null | undefined ? never : T;");
    }

    /// <summary>
    /// Advanced TypeScript types
    /// </summary>
    private static void AdvancedTypes()
    {
        Console.WriteLine("\n4. Advanced Types:");
        
        Console.WriteLine("// Union types");
        Console.WriteLine("type Status = 'loading' | 'success' | 'error';");
        Console.WriteLine("type StringOrNumber = string | number;");
        Console.WriteLine("type Theme = 'light' | 'dark' | 'auto';");
        
        Console.WriteLine("\n// Intersection types");
        Console.WriteLine("type Serializable = {");
        Console.WriteLine("  serialize(): string;");
        Console.WriteLine("};");
        Console.WriteLine("type Timestamped = {");
        Console.WriteLine("  timestamp: Date;");
        Console.WriteLine("};");
        Console.WriteLine("type LogEntry = Serializable & Timestamped & {");
        Console.WriteLine("  message: string;");
        Console.WriteLine("  level: 'info' | 'warn' | 'error';");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Discriminated unions");
        Console.WriteLine("interface LoadingState {");
        Console.WriteLine("  status: 'loading';");
        Console.WriteLine("}");
        Console.WriteLine("interface SuccessState {");
        Console.WriteLine("  status: 'success';");
        Console.WriteLine("  data: any;");
        Console.WriteLine("}");
        Console.WriteLine("interface ErrorState {");
        Console.WriteLine("  status: 'error';");
        Console.WriteLine("  error: string;");
        Console.WriteLine("}");
        Console.WriteLine("type AsyncState = LoadingState | SuccessState | ErrorState;");
        
        Console.WriteLine("\n// Mapped types");
        Console.WriteLine("type Readonly<T> = {");
        Console.WriteLine("  readonly [P in keyof T]: T[P];");
        Console.WriteLine("};");
        Console.WriteLine("type Partial<T> = {");
        Console.WriteLine("  [P in keyof T]?: T[P];");
        Console.WriteLine("};");
        Console.WriteLine("type Nullable<T> = {");
        Console.WriteLine("  [P in keyof T]: T[P] | null;");
        Console.WriteLine("};");
        
        Console.WriteLine("\n// Template literal types");
        Console.WriteLine("type EventName<T extends string> = `on${Capitalize<T>}`;");
        Console.WriteLine("type ButtonEvent = EventName<'click'>; // 'onClick'");
        Console.WriteLine("type InputEvent = EventName<'change'>; // 'onChange'");
        
        Console.WriteLine("\n// Recursive types");
        Console.WriteLine("type Json = string | number | boolean | null | Json[] | { [key: string]: Json };");
        Console.WriteLine("type DeepReadonly<T> = {");
        Console.WriteLine("  readonly [P in keyof T]: T[P] extends object ? DeepReadonly<T[P]> : T[P];");
        Console.WriteLine("};");
    }

    /// <summary>
    /// Decorator patterns (when enabled)
    /// </summary>
    private static void DecoratorPatterns()
    {
        Console.WriteLine("\n5. Decorator Patterns:");
        
        Console.WriteLine("// Class decorator");
        Console.WriteLine("function sealed(constructor: Function) {");
        Console.WriteLine("  Object.seal(constructor);");
        Console.WriteLine("  Object.seal(constructor.prototype);");
        Console.WriteLine("}");
        Console.WriteLine("@sealed");
        Console.WriteLine("class BugReport {");
        Console.WriteLine("  type = 'report';");
        Console.WriteLine("  title: string;");
        Console.WriteLine("  constructor(t: string) {");
        Console.WriteLine("    this.title = t;");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Method decorator");
        Console.WriteLine("function log(target: any, propertyName: string, descriptor: PropertyDescriptor) {");
        Console.WriteLine("  const method = descriptor.value;");
        Console.WriteLine("  descriptor.value = function (...args: any[]) {");
        Console.WriteLine("    console.log(`Calling ${propertyName} with args:`, args);");
        Console.WriteLine("    const result = method.apply(this, args);");
        Console.WriteLine("    console.log(`Result:`, result);");
        Console.WriteLine("    return result;");
        Console.WriteLine("  };");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Property decorator");
        Console.WriteLine("function configurable(value: boolean) {");
        Console.WriteLine("  return function (target: any, propertyKey: string) {");
        Console.WriteLine("    const descriptor = Object.getOwnPropertyDescriptor(target, propertyKey) || {};");
        Console.WriteLine("    descriptor.configurable = value;");
        Console.WriteLine("    Object.defineProperty(target, propertyKey, descriptor);");
        Console.WriteLine("  };");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Parameter decorator");
        Console.WriteLine("function required(target: any, propertyKey: string, parameterIndex: number) {");
        Console.WriteLine("  const existingRequiredParameters: number[] = ");
        Console.WriteLine("    Reflect.getOwnMetadata('required', target, propertyKey) || [];");
        Console.WriteLine("  existingRequiredParameters.push(parameterIndex);");
        Console.WriteLine("  Reflect.defineMetadata('required', existingRequiredParameters, target, propertyKey);");
        Console.WriteLine("}");
    }

    /// <summary>
    /// TypeScript utility types
    /// </summary>
    private static void UtilityTypes()
    {
        Console.WriteLine("\n6. Utility Types:");
        
        Console.WriteLine("// Built-in utility types");
        Console.WriteLine("interface User {");
        Console.WriteLine("  id: number;");
        Console.WriteLine("  name: string;");
        Console.WriteLine("  email: string;");
        Console.WriteLine("  age: number;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Partial<T> - Makes all properties optional");
        Console.WriteLine("type PartialUser = Partial<User>;");
        Console.WriteLine("// { id?: number; name?: string; email?: string; age?: number; }");
        
        Console.WriteLine("\n// Required<T> - Makes all properties required");
        Console.WriteLine("type RequiredUser = Required<PartialUser>;");
        
        Console.WriteLine("\n// Pick<T, K> - Creates type with subset of properties");
        Console.WriteLine("type UserSummary = Pick<User, 'id' | 'name'>;");
        Console.WriteLine("// { id: number; name: string; }");
        
        Console.WriteLine("\n// Omit<T, K> - Creates type without specified properties");
        Console.WriteLine("type UserWithoutId = Omit<User, 'id'>;");
        Console.WriteLine("// { name: string; email: string; age: number; }");
        
        Console.WriteLine("\n// Record<K, T> - Creates object type with specific keys and values");
        Console.WriteLine("type UserRoles = Record<'admin' | 'user' | 'guest', string[]>;");
        Console.WriteLine("// { admin: string[]; user: string[]; guest: string[]; }");
        
        Console.WriteLine("\n// Exclude<T, U> - Excludes types from union");
        Console.WriteLine("type StringOrNumber = string | number | boolean;");
        Console.WriteLine("type StringOnly = Exclude<StringOrNumber, number | boolean>; // string");
        
        Console.WriteLine("\n// Extract<T, U> - Extracts types from union");
        Console.WriteLine("type NumberOnly = Extract<StringOrNumber, number>; // number");
        
        Console.WriteLine("\n// ReturnType<T> - Gets return type of function");
        Console.WriteLine("function createUser(): User {");
        Console.WriteLine("  return { id: 1, name: 'John', email: 'john@example.com', age: 30 };");
        Console.WriteLine("}");
        Console.WriteLine("type CreateUserReturn = ReturnType<typeof createUser>; // User");
        
        Console.WriteLine("\n// Custom utility types");
        Console.WriteLine("type DeepPartial<T> = {");
        Console.WriteLine("  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];");
        Console.WriteLine("};");
        Console.WriteLine("type Optional<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;");
        Console.WriteLine("type NonEmptyArray<T> = [T, ...T[]];");
    }

    /// <summary>
    /// TypeScript module patterns
    /// </summary>
    private static void ModulePatterns()
    {
        Console.WriteLine("\n7. Module Patterns:");
        
        Console.WriteLine("// Namespace pattern");
        Console.WriteLine("namespace Utilities {");
        Console.WriteLine("  export function formatCurrency(amount: number): string {");
        Console.WriteLine("    return `$${amount.toFixed(2)}`;");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  export namespace Validation {");
        Console.WriteLine("    export function isEmail(email: string): boolean {");
        Console.WriteLine("      return /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/.test(email);");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Module augmentation");
        Console.WriteLine("// Extending existing modules");
        Console.WriteLine("declare module 'lodash' {");
        Console.WriteLine("  interface LoDashStatic {");
        Console.WriteLine("    customMethod(input: string): string;");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Global augmentation");
        Console.WriteLine("declare global {");
        Console.WriteLine("  interface Window {");
        Console.WriteLine("    customGlobal: {");
        Console.WriteLine("      version: string;");
        Console.WriteLine("      config: Record<string, any>;");
        Console.WriteLine("    };");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Ambient module declarations");
        Console.WriteLine("declare module '*.json' {");
        Console.WriteLine("  const content: Record<string, any>;");
        Console.WriteLine("  export default content;");
        Console.WriteLine("}");
        Console.WriteLine("declare module '*.css' {");
        Console.WriteLine("  const styles: Record<string, string>;");
        Console.WriteLine("  export default styles;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Type-only imports/exports");
        Console.WriteLine("// export type { User, UserRepository } from './types';");
        Console.WriteLine("// import type { ApiResponse } from './api';");
        
        Console.WriteLine("\n// Triple-slash directives");
        Console.WriteLine("/// <reference path=\"./types.d.ts\" />");
        Console.WriteLine("/// <reference types=\"node\" />");
        
        Console.WriteLine("\n// Export patterns");
        Console.WriteLine("// Named exports");
        Console.WriteLine("export { User, UserRepository };");
        Console.WriteLine("export { User as UserModel };");
        Console.WriteLine("// Default export");
        Console.WriteLine("export default class UserService {");
        Console.WriteLine("  // Implementation");
        Console.WriteLine("}");
        Console.WriteLine("// Re-exports");
        Console.WriteLine("export * from './user';");
        Console.WriteLine("export { default as UserService } from './user-service';");
    }
}