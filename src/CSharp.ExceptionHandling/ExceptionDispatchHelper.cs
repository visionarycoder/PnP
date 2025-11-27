using System.Runtime.ExceptionServices;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Preserves the original stack trace when re-throwing exceptions.
/// </summary>
public static class ExceptionDispatchHelper
{
    /// <summary>
    /// Re-throws an exception while preserving the original stack trace.
    /// </summary>
    public static void RethrowWithStackTrace(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    /// Transforms an exception while preserving stack trace information.
    /// </summary>
    public static T TransformException<T>(Exception originalException, Func<Exception, T> transform) where T : Exception
    {
        var transformed = transform(originalException);
        
        // Preserve original exception as inner exception if not already set
        if (transformed.InnerException == null && transformed != originalException)
        {
            // Create new instance with original exception as inner exception
            var constructors = typeof(T).GetConstructors();
            var messageInnerConstructor = constructors.FirstOrDefault(c => 
            {
                var parameters = c.GetParameters();
                return parameters.Length == 2 && 
                       parameters[0].ParameterType == typeof(string) && 
                       parameters[1].ParameterType == typeof(Exception);
            });

            if (messageInnerConstructor != null)
            {
                return (T)messageInnerConstructor.Invoke(new object[] { transformed.Message, originalException });
            }
        }

        return transformed;
    }
}