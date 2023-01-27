using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.DataLinq;

using Data;
using Linq;
using Internal;
using Interceptors;

public static partial class LinqToDBForDataLinqTools
{
    #region BulkCopy

    /// <summary>
    /// Performs bulk insert operation.
    /// </summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="options">Operation options.</param>
    /// <param name="source">Records to insert.</param>
    /// <returns>Bulk insert operation status.</returns>
    public static BulkCopyRowsCopied BulkCopy<T>(this DataLinqContext context, BulkCopyOptions options, IEnumerable<T> source) where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return dc.BulkCopy(options, source);
        }
    }

    /// <summary>
    /// Performs bulk insert operation.
    /// </summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
    /// <param name="source">Records to insert.</param>
    /// <returns>Bulk insert operation status.</returns>
    public static BulkCopyRowsCopied BulkCopy<T>(this DataLinqContext context, int maxBatchSize, IEnumerable<T> source) where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return dc.BulkCopy(
                new BulkCopyOptions { MaxBatchSize = maxBatchSize },
                source);
        }
    }

    /// <summary>
    /// Performs bulk insert operation.
    /// </summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="source">Records to insert.</param>
    /// <returns>Bulk insert operation status.</returns>
    public static BulkCopyRowsCopied BulkCopy<T>(this DataLinqContext context, IEnumerable<T> source) where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return dc.BulkCopy(
                new BulkCopyOptions(),
                source);
        }
    }

    #endregion

    #region BulkCopyAsync

    /// <summary>Asynchronously performs bulk insert operation.</summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="options">Operation options.</param>
    /// <param name="source">Records to insert.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
    /// <returns>Task with bulk insert operation status.</returns>
    public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
        this DataLinqContext context,
        BulkCopyOptions options,
        IEnumerable<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return await dc.BulkCopyAsync(options, source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
        }
    }

    /// <summary>Asynchronously performs bulk insert operation.</summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="maxBatchSize">
    ///     Number of rows in each batch. At the end of each batch, the rows in the batch are sent to
    ///     the server.
    /// </param>
    /// <param name="source">Records to insert.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
    /// <returns>Task with bulk insert operation status.</returns>
    public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
        this DataLinqContext context,
        int maxBatchSize,
        IEnumerable<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return await dc.BulkCopyAsync(maxBatchSize, source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
        }
    }

    /// <summary>Asynchronously performs bulk insert operation.</summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="source">Records to insert.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
    /// <returns>Task with bulk insert operation status.</returns>
    public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
        this DataLinqContext context,
        IEnumerable<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return await dc.BulkCopyAsync(source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
        }
    }

    /// <summary>Asynchronously performs bulk insert operation.</summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="options">Operation options.</param>
    /// <param name="source">Records to insert.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
    /// <returns>Task with bulk insert operation status.</returns>
    public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
        this DataLinqContext context,
        BulkCopyOptions options,
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return await dc.BulkCopyAsync(options, source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
        }
    }

    /// <summary>Asynchronously performs bulk insert operation.</summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="maxBatchSize">
    ///     Number of rows in each batch. At the end of each batch, the rows in the batch are sent to
    ///     the server.
    /// </param>
    /// <param name="source">Records to insert.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
    /// <returns>Task with bulk insert operation status.</returns>
    public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
        this DataLinqContext context,
        int maxBatchSize,
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return await dc.BulkCopyAsync(maxBatchSize, source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
        }
    }

    /// <summary>Asynchronously performs bulk insert operation.</summary>
    /// <typeparam name="T">Mapping type of inserted record.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="source">Records to insert.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
    /// <returns>Task with bulk insert operation status.</returns>
    public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
        this DataLinqContext context,
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (source == null) throw new ArgumentNullException(nameof(source));

        using (var dc = context.CreateLinqToDbConnection())
        {
            return await dc.BulkCopyAsync(source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
        }
    }

    #endregion

    #region Value Insertable

    /// <summary>
    /// Starts LINQ query definition for insert operation.
    /// </summary>
    /// <typeparam name="T">Target table mapping class.</typeparam>
    /// <param name="context">Database context.</param>
    /// <param name="target">Target table.</param>
    /// <returns>Insertable source query.</returns>
    public static IValueInsertable<T> Into<T>(this DataLinqContext context, ITable<T> target)
        where T : notnull
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (target == null) throw new ArgumentNullException(nameof(target));

        return context.CreateLinqToDbConnection().Into(target);
    }

    #endregion

    #region GetTable

    /// <summary>
    /// Returns queryable source for specified mapping class for current DBContext, mapped to database table or view.
    /// </summary>
    /// <typeparam name="T">Mapping class type.</typeparam>
    /// <returns>Queryable source.</returns>
    public static ITable<T> GetTable<T>(this DataLinqContext context)
        where T : class
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        return context.CreateLinqToDbContext().GetTable<T>();
    }

    #endregion
}
