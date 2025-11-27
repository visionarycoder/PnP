using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp.EventSourcing;

/// <summary>
/// Exception thrown when optimistic concurrency check fails
/// </summary>
public class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException(string message) : base(message) { }
    public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}