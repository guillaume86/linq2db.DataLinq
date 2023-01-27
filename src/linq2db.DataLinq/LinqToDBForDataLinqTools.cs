using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.DataLinq;

using Async;
using Data;
using DataProvider;
using Linq;
using Mapping;
using Metadata;
using Expressions;

using Internal;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// EF Core <see cref="DataLinqContext"/> extensions to call LINQ To DB functionality.
/// </summary>
public static partial class LinqToDBForDataLinqTools
{
    static readonly Lazy<bool> _intialized = new(InitializeInternal);

    /// <summary>
    /// Initializes integration of LINQ To DB with EF Core.
    /// </summary>
    public static void Initialize()
    {
        var _ = _intialized.Value;
    }

    static bool InitializeInternal()
    {
        var prev = LinqExtensions.ProcessSourceQueryable;

        var instantiator = MemberHelper.MethodOf(() => Internals.CreateExpressionQueryInstance<int>(null!, null!))
            .GetGenericMethodDefinition();

        LinqExtensions.ProcessSourceQueryable = queryable =>
        {
            // our Provider - nothing to do
            if (queryable.Provider is IQueryProviderAsync)
                return queryable;

            var context = Implementation.GetCurrentContext(queryable);
            if (context == null)
                throw new LinqToDBForDataLinqToolsException("Can not evaluate current context from query");

            var dc = CreateLinqToDbContext(context);
            var newExpression = queryable.Expression;

            var result = (IQueryable)instantiator.MakeGenericMethod(queryable.ElementType)
                .Invoke(null, new object[] { dc, newExpression })!;

            if (prev != null)
                result = prev(result);

            return result;
        };

        //LinqExtensions.ExtensionsAdapter = new LinqToDBExtensionsAdapter();

        return true;
    }

    static ILinqToDBForDataLinqTools _implementation;

    /// <summary>
    /// Gets or sets EF Core to LINQ To DB integration bridge implementation.
    /// </summary>
    public static ILinqToDBForDataLinqTools Implementation
    {
        get => _implementation;
        [MemberNotNull(nameof(_implementation), nameof(_defaultMetadataReader))]
        set
        {
            _implementation = value ?? throw new ArgumentNullException(nameof(value));
            _metadataReaders.Clear();
            _defaultMetadataReader = new Lazy<IMetadataReader?>(() => Implementation.CreateMetadataReader(null));
        }
    }

    static readonly ConcurrentDictionary<System.Data.Linq.Mapping.MetaModel, IMetadataReader?> _metadataReaders = new();

    static Lazy<IMetadataReader?> _defaultMetadataReader;

    /// <summary>
    /// Clears internal caches
    /// </summary>
    public static void ClearCaches()
    {
        _metadataReaders.Clear();
        Implementation.ClearCaches();
        Query.ClearCaches();
    }

    static LinqToDBForDataLinqTools()
    {
        Implementation = new LinqToDBForDataLinqToolsImplDefault();
        Initialize();
    }

    /// <summary>
    /// Creates or return existing metadata provider for provided EF Core data model. If model is null, empty metadata
    /// provider will be returned.
    /// </summary>
    /// <param name="model">EF Core data model instance. Could be <c>null</c>.</param>
    /// <param name="accessor">EF Core service provider.</param>
    /// <returns>LINQ To DB metadata provider.</returns>
    public static IMetadataReader? GetMetadataReader(
        System.Data.Linq.Mapping.MetaModel? model)
    {
        if (model == null)
            return _defaultMetadataReader.Value;

        return _metadataReaders.GetOrAdd(model, m => Implementation.CreateMetadataReader(model));
    }

    ///// <summary>
    ///// Returns EF Core <see cref="DataLinqContextOptions"/> for specific <see cref="DataLinqContext"/> instance.
    ///// </summary>
    ///// <param name="context">EF Core <see cref="DataLinqContext"/> instance.</param>
    ///// <returns><see cref="DataLinqContextOptions"/> instance.</returns>
    //public static IDataLinqContextOptions? GetContextOptions(DataLinqContext context)
    //{
    //	return Implementation.GetContextOptions(context);
    //}

    /// <summary>
    /// Returns EF Core database provider information for specific <see cref="DataLinqContext"/> instance.
    /// </summary>
    /// <param name="context">EF Core <see cref="DataLinqContext"/> instance.</param>
    /// <returns>EF Core provider information.</returns>
    public static DataLinqProviderInfo GetEFProviderInfo(DataLinqContext context)
    {
        var info = new DataLinqProviderInfo
        {
            Connection = context.Connection,
            Context = context,
            //Options = GetContextOptions(context)
        };

        return info;
    }

    /// <summary>
    /// Returns EF Core database provider information for specific <see cref="DbConnection"/> instance.
    /// </summary>
    /// <param name="connection">EF Core <see cref="DbConnection"/> instance.</param>
    /// <returns>EF Core provider information.</returns>
    public static DataLinqProviderInfo GetEFProviderInfo(DbConnection connection)
    {
        var info = new DataLinqProviderInfo
        {
            Connection = connection,
            Context = null,
            //Options = null
        };

        return info;
    }

    ///// <summary>
    ///// Returns EF Core database provider information for specific <see cref="DataLinqContextOptions"/> instance.
    ///// </summary>
    ///// <param name="options">EF Core <see cref="DataLinqContextOptions"/> instance.</param>
    ///// <returns>EF Core provider information.</returns>
    //public static DataLinqProviderInfo GetEFProviderInfo(DataLinqContextOptions options)
    //{
    //	var info = new DataLinqProviderInfo
    //	{
    //		Connection = null,
    //		Context = null,
    //		Options = options
    //	};

    //	return info;
    //}

    /// <summary>
    /// Returns LINQ To DB provider, based on provider data from EF Core.
    /// </summary>
    /// <param name="info">EF Core provider information.</param>
    /// <param name="connectionInfo">Database connection information.</param>
    /// <returns>LINQ TO DB provider instance.</returns>
    public static IDataProvider GetDataProvider(DataLinqProviderInfo info, DataLinqConnectionInfo connectionInfo)
    {
        var provider = Implementation.GetDataProvider(info, connectionInfo);

        if (provider == null)
            throw new LinqToDBForDataLinqToolsException("Can not detect provider from Entity Framework or provider not supported");

        return provider;
    }

    /// <summary>
    /// Creates mapping schema using provided EF Core data model.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="accessor">EF Core service provider.</param>
    /// <returns>Mapping schema for provided EF Core model.</returns>
    public static MappingSchema GetMappingSchema(
        System.Data.Linq.Mapping.MetaModel model)
    {
        return Implementation.GetMappingSchema(model, GetMetadataReader(model));
    }

    /// <summary>
    /// Transforms EF Core expression tree to LINQ To DB expression.
    /// </summary>
    /// <param name="expression">EF Core expression tree.</param>
    /// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
    /// <param name="ctx">Optional DataLinqContext instance.</param>
    /// <param name="model">EF Core data model instance.</param>
    /// <returns>Transformed expression.</returns>
    public static Expression TransformExpression(Expression expression, IDataContext? dc, DataLinqContext? ctx, System.Data.Linq.Mapping.MetaModel? model)
    {
        return Implementation.TransformExpression(expression, dc, ctx, model);
    }

    /// <summary>
    /// Creates LINQ To DB <see cref="DataConnection"/> instance, attached to provided
    /// EF Core <see cref="DataLinqContext"/> instance connection and transaction.
    /// </summary>
    /// <param name="context">EF Core <see cref="DataLinqContext"/> instance.</param>
    /// <param name="transaction">Optional transaction instance, to which created connection should be attached.
    /// If not specified, will use current <see cref="DataLinqContext"/> transaction if it available.</param>
    /// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
    public static DataConnection CreateLinqToDbConnection(this DataLinqContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var info = GetEFProviderInfo(context);

        DataConnection? dc = null;

        var connectionInfo = GetConnectionInfo(info);
        var provider = GetDataProvider(info, connectionInfo);

        if (context.Transaction != null)
        {
            // TODO: we need API for testing current connection
            //if (provider.IsCompatibleConnection(dbTrasaction.Connection))
            dc = new LinqToDBForDataLinqToolsDataConnection(context, provider, context.Transaction, context.Mapping, TransformExpression);
        }

        if (dc == null)
        {
            var dbConnection = context.Connection;
            // TODO: we need API for testing current connection
            if (true /*provider.IsCompatibleConnection(dbConnection)*/)
                dc = new LinqToDBForDataLinqToolsDataConnection(context, provider, dbConnection, context.Mapping, TransformExpression);
            else
            {
                //dc = new LinqToDBForDataLinqToolsDataConnection(context, provider, connectionInfo.ConnectionString, context.Model, TransformExpression);
            }
        }

        var mappingSchema = GetMappingSchema(context.Mapping);
        if (mappingSchema != null)
            dc.AddMappingSchema(mappingSchema);

        return dc;
    }

    /// <summary>
    /// Creates linq2db data context for EF Core database context.
    /// </summary>
    /// <param name="context">EF Core database context.</param>
    /// <param name="transaction">Transaction instance.</param>
    /// <returns>linq2db data context.</returns>
    public static IDataContext CreateLinqToDbContext(this DataLinqContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var info = GetEFProviderInfo(context);

        DataConnection? dc = null;

        var transaction = context.Transaction;

        var connectionInfo = GetConnectionInfo(info);
        var provider = GetDataProvider(info, connectionInfo);
        var mappingSchema = GetMappingSchema(context.Mapping);

        if (transaction != null)
        {
            // TODO: we need API for testing current connection
            // if (provider.IsCompatibleConnection(dbTransaction.Connection))
            dc = new LinqToDBForDataLinqToolsDataConnection(context, provider, transaction, context.Mapping, TransformExpression);
        }

        if (dc == null)
        {
            // TODO: we need API for testing current connection
            if (true /*provider.IsCompatibleConnection(dbConnection)*/)
                dc = new LinqToDBForDataLinqToolsDataConnection(context, provider, context.Connection, context.Mapping, TransformExpression);
            else
            {
                /*
                // special case when we have to create data connection by itself
                var dataContext = new LinqToDBForDataLinqToolsDataContext(context, provider, connectionInfo.ConnectionString, context.Model, TransformExpression);

                if (mappingSchema != null)
                    dataContext.MappingSchema = mappingSchema;

                if (logger != null)
                    dataContext.OnTraceConnection = t => Implementation.LogConnectionTrace(t, logger);

                return dataContext;
                */
            }
        }

        if (mappingSchema != null)
            dc.AddMappingSchema(mappingSchema);

        return dc;
    }

    /// <summary>
    /// Creates LINQ To DB <see cref="DataConnection"/> instance that creates new database connection using connection
    /// information from EF Core <see cref="DataLinqContext"/> instance.
    /// </summary>
    /// <param name="context">EF Core <see cref="DataLinqContext"/> instance.</param>
    /// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
    public static DataConnection CreateLinq2DbConnectionDetached(this DataLinqContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var info = GetEFProviderInfo(context);
        var connectionInfo = GetConnectionInfo(info);
        var dataProvider = GetDataProvider(info, connectionInfo);

        var dc = new LinqToDBForDataLinqToolsDataConnection(context, dataProvider, connectionInfo.ConnectionString!, context.Mapping, TransformExpression);

        var mappingSchema = GetMappingSchema(context.Mapping);
        if (mappingSchema != null)
            dc.AddMappingSchema(mappingSchema);

        return dc;
    }


    static readonly ConcurrentDictionary<Type, Func<DbConnection, string>> _connectionStringExtractors = new();

    /// <summary>
    /// Extracts database connection information from EF Core provider data.
    /// </summary>
    /// <param name="info">EF Core database provider data.</param>
    /// <returns>Database connection information.</returns>
    public static DataLinqConnectionInfo GetConnectionInfo(DataLinqProviderInfo info)
    {
        var connection = info.Connection;
        string? connectionString = null;
        if (connection != null)
        {
            var connectionStringFunc = _connectionStringExtractors.GetOrAdd(connection.GetType(), t =>
            {
                // NpgSQL workaround
                var originalProp = t.GetProperty("OriginalConnectionString", BindingFlags.Instance | BindingFlags.NonPublic);

                if (originalProp == null)
                    return c => c.ConnectionString;

                var parameter = Expression.Parameter(typeof(DbConnection), "c");
                var lambda = Expression.Lambda<Func<DbConnection, string>>(
                    Expression.MakeMemberAccess(Expression.Convert(parameter, t), originalProp), parameter);

                return lambda.Compile();
            });

            connectionString = connectionStringFunc(connection);
        }

        if (connection != null && connectionString != null)
            return new DataLinqConnectionInfo { Connection = connection, ConnectionString = connectionString };

        return new DataLinqConnectionInfo
        {
            Connection = info.Connection,
            ConnectionString = info.Connection?.ConnectionString
        };
    }

    /// <summary>
    /// Converts EF Core's query to LINQ To DB query and attach it to provided LINQ To DB <see cref="IDataContext"/>.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">EF Core query.</param>
    /// <param name="dc">LINQ To DB <see cref="IDataContext"/> to use with provided query.</param>
    /// <returns>LINQ To DB query, attached to provided <see cref="IDataContext"/>.</returns>
    public static IQueryable<T> ToLinqToDB<T>(this IQueryable<T> query, IDataContext dc)
    {
        var context = Implementation.GetCurrentContext(query);
        if (context == null)
            throw new LinqToDBForDataLinqToolsException("Can not evaluate current context from query");

        return new LinqToDBForDataLinqQueryProvider<T>(dc, query.Expression);
    }

    /// <summary>
    /// Converts EF Core's query to LINQ To DB query and attach it to current EF Core connection.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">EF Core query.</param>
    /// <returns>LINQ To DB query, attached to current EF Core connection.</returns>
    public static IQueryable<T> ToLinqToDB<T>(this IQueryable<T> query)
    {
        if (query.Provider is IQueryProviderAsync)
        {
            return query;
        }

        var context = Implementation.GetCurrentContext(query);
        if (context == null)
            throw new LinqToDBForDataLinqToolsException("Can not evaluate current context from query");

        var dc = CreateLinqToDbContext(context);

        return new LinqToDBForDataLinqQueryProvider<T>(dc, Implementation.TransformExpression(query.Expression, dc, context, context.Mapping));
    }

    /// <summary>
    /// Extracts <see cref="DataLinqContext"/> instance from <see cref="IQueryable"/> object.
    /// </summary>
    /// <param name="query">EF Core query.</param>
    /// <returns>Current <see cref="DataLinqContext"/> instance.</returns>
    public static DataLinqContext? GetCurrentContext(IQueryable query)
    {
        return Implementation.GetCurrentContext(query);
    }

    /// <summary>
    /// Enables attaching entities to change tracker.
    /// Entities will be attached only if AsNoTracking() is not used in query and DataLinqContext is configured to track entities. 
    /// </summary>
    public static bool EnableChangeTracker
    {
        get => Implementation.EnableChangeTracker;
        set => Implementation.EnableChangeTracker = value;
    }
}
