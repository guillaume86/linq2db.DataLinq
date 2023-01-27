using System;

namespace LinqToDB.DataLinq;

/// <summary>
/// Exception class for EF.Core to LINQ To DB integration issues.
/// </summary>
public sealed class LinqToDBForDataLinqToolsException : Exception
{
    /// <summary>
    /// Creates new instance of exception.
    /// </summary>
    public LinqToDBForDataLinqToolsException()
    {
    }

    /// <summary>
    /// Creates new instance of exception.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public LinqToDBForDataLinqToolsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates new instance of exception when it generated for other exception.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Original exception.</param>
    public LinqToDBForDataLinqToolsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
