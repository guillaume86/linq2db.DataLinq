using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;

using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace LinqToDB.DataLinq;

using Data;
using Expressions;
using Mapping;
using Metadata;
using Extensions;
using SqlQuery;
using Reflection;
using Common.Internal.Cache;

using DataProvider;
using DataProvider.DB2;
using DataProvider.Firebird;
using DataProvider.MySql;
using DataProvider.Oracle;
using DataProvider.PostgreSQL;
using DataProvider.SQLite;
using DataProvider.SqlServer;
using DataProvider.SqlCe;
using System.Diagnostics.CodeAnalysis;
using System.Data.Linq;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
/// <summary>
/// Default EF Core - LINQ To DB integration bridge implementation.
/// </summary>
public class LinqToDBForDataLinqToolsImplDefault : ILinqToDBForDataLinqTools
{
    sealed class ProviderKey
    {
        public ProviderKey(string? providerName, string? connectionString)
        {
            ProviderName = providerName;
            ConnectionString = connectionString;
        }

        string? ProviderName { get; }
        string? ConnectionString { get; }

        #region Equality members

        private bool Equals(ProviderKey other)
        {
            return string.Equals(ProviderName, other.ProviderName) && string.Equals(ConnectionString, other.ConnectionString);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProviderKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ProviderName != null ? ProviderName.GetHashCode() : 0) * 397) ^ (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
            }
        }

        #endregion
    }

    readonly ConcurrentDictionary<ProviderKey, IDataProvider> _knownProviders = new();

    private readonly MemoryCache _schemaCache = new(
        new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromHours(1.0)
        });


    /// <summary>
    /// Force clear of internal caches.
    /// </summary>
    public virtual void ClearCaches()
    {
        _knownProviders.Clear();
        _schemaCache.Compact(1.0);
    }

    /// <summary>
    /// Returns LINQ To DB provider, based on provider data from EF Core.
    /// Could be overriden if you have issues with default detection mechanisms.
    /// </summary>
    /// <param name="providerInfo">Provider information, extracted from EF Core.</param>
    /// <param name="connectionInfo"></param>
    /// <returns>LINQ TO DB provider instance.</returns>
    public virtual IDataProvider GetDataProvider(DataLinqProviderInfo providerInfo, DataLinqConnectionInfo connectionInfo)
    {
        var info = GetLinqToDbProviderInfo(providerInfo);

        return _knownProviders.GetOrAdd(new ProviderKey(info.ProviderName, connectionInfo.ConnectionString), k =>
        {
            return CreateLinqToDbDataProvider(providerInfo, info, connectionInfo);
        });
    }

    /// <summary>
    /// Converts EF Core provider settings to linq2db provider settings.
    /// </summary>
    /// <param name="providerInfo">EF Core provider settings.</param>
    /// <returns>linq2db provider settings.</returns>
    protected virtual LinqToDBProviderInfo GetLinqToDbProviderInfo(DataLinqProviderInfo providerInfo)
    {
        var provInfo = new LinqToDBProviderInfo();

        if (providerInfo.Connection != null)
        {
            provInfo.Merge(GetLinqToDbProviderInfo(providerInfo.Connection));
        }

        if (providerInfo.Context != null)
        {
            provInfo.Merge(GetLinqToDbProviderInfo(providerInfo.Context));
        }

        return provInfo;
    }

    /// <summary>
    /// Creates instance of linq2db database provider.
    /// </summary>
    /// <param name="providerInfo">EF Core provider settings.</param>
    /// <param name="provInfo">linq2db provider settings.</param>
    /// <param name="connectionInfo">EF Core connection settings.</param>
    /// <returns>linq2db database provider.</returns>
    protected virtual IDataProvider CreateLinqToDbDataProvider(DataLinqProviderInfo providerInfo, LinqToDBProviderInfo provInfo,
        DataLinqConnectionInfo connectionInfo)
    {
        if (provInfo.ProviderName == null)
        {
            throw new LinqToDBForDataLinqToolsException("Can not detect data provider.");
        }

        switch (provInfo.ProviderName)
        {
            case ProviderName.SqlServer:
                return CreateSqlServerProvider(SqlServerDefaultVersion, connectionInfo.ConnectionString);

            default:
                throw new LinqToDBForDataLinqToolsException($"Can not instantiate data provider '{provInfo.ProviderName}'.");
        }
    }

    /// <summary>
    /// Creates linq2db provider settings object from <see cref="DatabaseFacade"/> instance.
    /// </summary>
    /// <param name="database">EF Core database information object.</param>
    /// <returns>linq2db provider settings.</returns>
    protected virtual LinqToDBProviderInfo? GetLinqToDbProviderInfo(DataLinqContext context)
    {
        return new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer };
    }

    /// <summary>
    /// Creates linq2db provider settings object from <see cref="DbConnection"/> instance.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <returns>linq2db provider settings.</returns>
    protected virtual LinqToDBProviderInfo? GetLinqToDbProviderInfo(DbConnection connection)
    {
        return new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer };

        //switch (connection.GetType().Name)
        //{
        //    case "SqlConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer };
        //    case "MySqlConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.MySql };
        //    case "NpgsqlConnection":
        //    case "PgSqlConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.PostgreSQL };
        //    case "FbConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.Firebird };
        //    case "DB2Connection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.DB2LUW };
        //    case "OracleConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.Oracle };
        //    case "SqliteConnection":
        //    case "SQLiteConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.SQLite };
        //    case "JetConnection":
        //        return new LinqToDBProviderInfo { ProviderName = ProviderName.Access };
        //}

        //return null;
    }

    /// <summary>
    /// Creates linq2db SQL Server database provider instance.
    /// </summary>
    /// <param name="version">SQL Server dialect.</param>
    /// <param name="connectionString">Connection string.</param>
    /// <returns>linq2db SQL Server provider instance.</returns>
    protected virtual IDataProvider CreateSqlServerProvider(SqlServerVersion version, string? connectionString)
    {
        return DataProvider.SqlServer.SqlServerTools.GetDataProvider(version, SqlServerProvider.SystemDataSqlClient);
    }

    /// <summary>
    /// Creates linq2db PostgreSQL database provider instance.
    /// </summary>
    /// <param name="version">PostgreSQL dialect.</param>
    /// <param name="connectionString">Connection string.</param>
    /// <returns>linq2db PostgreSQL provider instance.</returns>
    protected virtual IDataProvider CreatePostgreSqlProvider(PostgreSQLVersion version, string? connectionString)
    {
        return PostgreSQLTools.GetDataProvider(version);
    }

    /// <summary>
    /// Creates metadata provider for specified EF Core data model. Default implementation uses
    /// <see cref="DataLinqMetadataReader"/> metadata provider.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="accessor">EF Core service provider.</param>
    /// <returns>LINQ To DB metadata provider for specified EF Core model.</returns>
    public virtual IMetadataReader CreateMetadataReader(System.Data.Linq.Mapping.MetaModel? model)
    {
        return new SystemDataLinqAttributeReader();
        //return new DataLinqMetadataReader(model);
    }

    /// <summary>
    /// Creates mapping schema using provided EF Core data model and metadata provider.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
    /// <param name="convertorSelector"></param>
    /// <returns>Mapping schema for provided EF.Core model.</returns>
    public virtual MappingSchema CreateMappingSchema(
        System.Data.Linq.Mapping.MetaModel model,
        IMetadataReader? metadataReader)
    {
        var schema = new MappingSchema();
        if (metadataReader != null)
            schema.AddMetadataReader(metadataReader);

        return schema;
    }

    private static LambdaExpression WithToDataParameter(Expression valueExpression, SqlDataType dataType, ParameterExpression fromParam)
        => Expression.Lambda
        (
            Expression.New
            (
                DataParameterConstructor,
                Expression.Constant("Conv", typeof(string)),
                valueExpression,
                Expression.Constant(dataType.Type.DataType, typeof(DataType)),
                Expression.Constant(dataType.Type.DbType, typeof(string))
            ),
            fromParam
        );

    private static Expression WithConvertToObject(Expression valueExpression)
        => valueExpression.Type != typeof(object)
            ? Expression.Convert(valueExpression, typeof(object))
            : valueExpression;

    /// <summary>
    /// Returns mapping schema using provided EF Core data model and metadata provider.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
    /// <param name="convertorSelector"></param>
    /// <returns>Mapping schema for provided EF.Core model.</returns>
    public virtual MappingSchema GetMappingSchema(
        System.Data.Linq.Mapping.MetaModel model,
        IMetadataReader? metadataReader)
    {
        var result = _schemaCache.GetOrCreate(
            Tuple.Create(
                model,
                metadataReader,
                EnableChangeTracker
            ),
            e =>
            {
                e.SlidingExpiration = TimeSpan.FromHours(1);
                return CreateMappingSchema(model, metadataReader);
            })!;

        return result;
    }

    static readonly MethodInfo L2DBFromSqlMethodInfo =
        MemberHelper.MethodOfGeneric<IDataContext>(dc => dc.FromSql<object>(new Common.RawSqlString()));

    static readonly ConstructorInfo RawSqlStringConstructor = MemberHelper.ConstructorOf(() => new Common.RawSqlString(""));

    static readonly ConstructorInfo DataParameterConstructor = MemberHelper.ConstructorOf(() => new DataParameter("", "", DataType.Undefined, ""));

    static readonly MethodInfo ToSql = MemberHelper.MethodOfGeneric(() => Sql.ToSql(1));

    /// <summary>
    /// Removes conversions from expression.
    /// </summary>
    /// <param name="ex">Expression.</param>
    /// <returns>Unwrapped expression.</returns>
    [return: NotNullIfNotNull(nameof(ex))]
    public static Expression? Unwrap(Expression? ex)
    {
        if (ex == null)
            return null;

        switch (ex.NodeType)
        {
            case ExpressionType.Quote: return Unwrap(((UnaryExpression)ex).Operand);
            case ExpressionType.ConvertChecked:
            case ExpressionType.Convert:
                {
                    var ue = (UnaryExpression)ex;

                    if (!ue.Operand.Type.IsEnum)
                        return Unwrap(ue.Operand);

                    break;
                }
        }

        return ex;
    }

    /// <summary>
    /// Tests that method is <see cref="IQueryable{T}"/> extension.
    /// </summary>
    /// <param name="method">Method to test.</param>
    /// <param name="enumerable">Allow <see cref="IEnumerable{T}"/> extensions.</param>
    /// <returns><c>true</c> if method is <see cref="IQueryable{T}"/> extension.</returns>
    public static bool IsQueryable(MethodCallExpression method, bool enumerable = true)
    {
        var type = method.Method.DeclaringType;

        return type == typeof(Queryable) || (enumerable && type == typeof(Enumerable)) || type == typeof(LinqExtensions) ||
               type == typeof(DataExtensions) || type == typeof(TableExtensions);
    }

    /// <summary>
    /// Evaluates value of expression.
    /// </summary>
    /// <param name="expr">Expression to evaluate.</param>
    /// <returns>Expression value.</returns>
    public static object? EvaluateExpression(Expression? expr)
    {
        if (expr == null)
            return null;

        switch (expr.NodeType)
        {
            case ExpressionType.Constant:
                return ((ConstantExpression)expr).Value;

            case ExpressionType.MemberAccess:
                {
                    var member = (MemberExpression)expr;

                    if (member.Member.IsFieldEx())
                        return ((FieldInfo)member.Member).GetValue(EvaluateExpression(member.Expression));

                    if (member.Member.IsPropertyEx())
                        return ((PropertyInfo)member.Member).GetValue(EvaluateExpression(member.Expression), null);

                    break;
                }
        }

        var value = Expression.Lambda(expr).Compile().DynamicInvoke();
        return value;
    }

    /// <summary>
    /// Compacts expression to handle big filters.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns>Compacted expression.</returns>
    public static Expression CompactExpression(Expression expression)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Or:
            case ExpressionType.And:
            case ExpressionType.OrElse:
            case ExpressionType.AndAlso:
                {
                    var stack = new Stack<Expression>();
                    var items = new List<Expression>();
                    var binary = (BinaryExpression)expression;

                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    while (stack.Count > 0)
                    {
                        var item = stack.Pop();
                        if (item.NodeType == expression.NodeType)
                        {
                            binary = (BinaryExpression)item;
                            stack.Push(binary.Right);
                            stack.Push(binary.Left);
                        }
                        else
                            items.Add(item);
                    }

                    if (items.Count > 3)
                    {
                        // having N items will lead to NxM recursive calls in expression visitors and
                        // will result in stack overflow on relatively small numbers (~1000 items).
                        // To fix it we will rebalance condition tree here which will result in
                        // LOG2(N)*M recursive calls, or 10*M calls for 1000 items.
                        //
                        // E.g. we have condition A OR B OR C OR D OR E
                        // as an expression tree it represented as tree with depth 5
                        //   OR
                        // A    OR
                        //    B    OR
                        //       C    OR
                        //          D    E
                        // for rebalanced tree it will have depth 4
                        //                  OR
                        //        OR
                        //   OR        OR        OR
                        // A    B    C    D    E    F
                        // Not much on small numbers, but huge improvement on bigger numbers
                        while (items.Count != 1)
                        {
                            items = CompactTree(items, expression.NodeType);
                        }

                        return items[0];
                    }

                    break;
                }
        }

        return expression;
    }

    static List<Expression> CompactTree(List<Expression> items, ExpressionType nodeType)
    {
        var result = new List<Expression>();

        // traverse list from left to right to preserve calculation order
        for (var i = 0; i < items.Count; i += 2)
        {
            if (i + 1 == items.Count)
            {
                // last non-paired item
                result.Add(items[i]);
            }
            else
            {
                result.Add(Expression.MakeBinary(nodeType, items[i], items[i + 1]));
            }
        }

        return result;
    }

    /// <summary>
    /// Transforms EF Core expression tree to LINQ To DB expression.
    /// Method replaces EF Core <see cref="EntityQueryable{TResult}"/> instances with LINQ To DB
    /// <see cref="DataExtensions.GetTable{T}(IDataContext)"/> calls.
    /// </summary>
    /// <param name="expression">EF Core expression tree.</param>
    /// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
    /// <param name="ctx">Optional DataLinqContext instance.</param>
    /// <param name="model">EF Core data model instance.</param>
    /// <returns>Transformed expression.</returns>
    public virtual Expression TransformExpression(Expression expression, IDataContext? dc, DataLinqContext? ctx, System.Data.Linq.Mapping.MetaModel? model)
    {
        var tracking = true;
        var ignoreTracking = false;

        var nonEvaluatableParameters = new HashSet<ParameterExpression>();

        TransformInfo LocalTransform(Expression e)
        {
            e = CompactExpression(e);

            switch (e.NodeType)
            {
                case ExpressionType.Lambda:
                    {
                        foreach (var parameter in ((LambdaExpression)e).Parameters)
                        {
                            nonEvaluatableParameters.Add(parameter);
                        }

                        break;
                    }

                case ExpressionType.Constant:
                    {
                        if (dc != null && typeof(Table<>).IsSameOrParentOf(e.Type))
                        {
                            var entityType = e.Type.GenericTypeArguments[0];
                            var newExpr = Expression.Call(null, Methods.LinqToDB.GetTable.MakeGenericMethod(entityType), Expression.Constant(dc));
                            return new TransformInfo(newExpr);
                        }

                        break;
                    }

                case ExpressionType.MemberAccess:
                    {
                        if (typeof(IQueryable<>).IsSameOrParentOf(e.Type))
                        {
                            var ma = (MemberExpression)e;
                            var query = (IQueryable)EvaluateExpression(ma)!;

                            return new TransformInfo(query.Expression, false, true);
                        }

                        break;
                    }

                case ExpressionType.Call:
                    {
                        var methodCall = (MethodCallExpression)e;

                        var generic = methodCall.Method.IsGenericMethod ? methodCall.Method.GetGenericMethodDefinition() : methodCall.Method;

                        if (IsQueryable(methodCall))
                        {
                            if (methodCall.Method.IsGenericMethod)
                            {
                                var isTunnel = false;
                                if (generic == Methods.LinqToDB.RemoveOrderBy)
                                {
                                    // This is workaround. EagerLoading runs query again with RemoveOrderBy method.
                                    // it is only one possible way now how to detect nested query. 
                                    ignoreTracking = true;
                                }

                                if (isTunnel)
                                    return new TransformInfo(methodCall.Arguments[0], false, true);
                            }

                            break;
                        }

                        if (typeof(ITable<>).IsSameOrParentOf(methodCall.Type))
                        {
                            if (generic.Name == "ToLinqToDBTable")
                            {
                                return new TransformInfo(methodCall.Arguments[0], false, true);
                            }

                            break;
                        }

                        //if (typeof(IQueryable<>).IsSameOrParentOf(methodCall.Type))
                        //{
                        //    if (null == methodCall.Find(nonEvaluatableParameters,
                        //            (c, t) => t.NodeType == ExpressionType.Parameter && c.Contains(t)))
                        //    {
                        //        // Invoking function to evaluate EF's Subquery located in function

                        //        var obj = EvaluateExpression(methodCall.Object);
                        //        var arguments = methodCall.Arguments.Select(EvaluateExpression).ToArray();
                        //        if (methodCall.Method.Invoke(obj, arguments) is IQueryable result)
                        //        {
                        //            if (!ExpressionEqualityComparer.Instance.Equals(methodCall, result.Expression))
                        //                return new TransformInfo(result.Expression, false, true);
                        //        }
                        //    }
                        //}

                        //List<Expression>? newArguments = null;
                        //var parameters = generic.GetParameters();
                        //for (var i = 0; i < parameters.Length; i++)
                        //{
                        //    var arg = methodCall.Arguments[i];
                        //    var canWrap = true;

                        //    if (arg.NodeType == ExpressionType.Call)
                        //    {
                        //        var mc = (MethodCallExpression)arg;
                        //        if (mc.Method.DeclaringType == typeof(Sql))
                        //            canWrap = false;
                        //    }

                        //    if (canWrap)
                        //    {
                        //        var parameterInfo = parameters[i];
                        //        var notParametrized = parameterInfo.GetCustomAttributes<NotParameterizedAttribute>()
                        //            .FirstOrDefault();
                        //        if (notParametrized != null)
                        //        {
                        //            newArguments ??= new List<Expression>(methodCall.Arguments.Take(i));

                        //            newArguments.Add(Expression.Call(ToSql.MakeGenericMethod(arg.Type), arg));
                        //            continue;
                        //        }
                        //    }

                        //    newArguments?.Add(methodCall.Arguments[i]);
                        //}

                        //if (newArguments != null)
                        //    return new TransformInfo(methodCall.Update(methodCall.Object, newArguments), false, true);

                        break;
                    }

            }

            return new TransformInfo(e);
        }

        var newExpression = expression.Transform(LocalTransform);

        if (!ignoreTracking && dc is LinqToDBForDataLinqToolsDataConnection dataConnection)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            dataConnection.Tracking = tracking;
        }

        return newExpression;
    }

    static Expression EnsureEnumerable(Expression expression, MappingSchema mappingSchema)
    {
        var enumerable = typeof(IEnumerable<>).MakeGenericType(GetEnumerableElementType(expression.Type, mappingSchema));
        if (expression.Type != enumerable)
            expression = Expression.Convert(expression, enumerable);
        return expression;
    }

    static Expression EnsureEnumerable(LambdaExpression lambda, MappingSchema mappingSchema)
    {
        var newBody = EnsureEnumerable(lambda.Body, mappingSchema);
        if (newBody != lambda.Body)
            lambda = Expression.Lambda(newBody, lambda.Parameters);
        return lambda;
    }


    static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
    {
        if (!IsEnumerableType(type, mappingSchema))
            return type;
        if (type.IsArray)
            return type.GetElementType()!;
        if (typeof(IGrouping<,>).IsSameOrParentOf(type))
            return type.GetGenericArguments()[1];
        return type.GetGenericArguments()[0];
    }

    static bool IsEnumerableType(Type type, MappingSchema mappingSchema)
    {
        if (mappingSchema.IsScalarType(type))
            return false;
        if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
            return false;
        return true;
    }

    /// <summary>
    /// Extracts <see cref="DataLinqContext"/> instance from <see cref="IQueryable"/> object.
    /// Due to unavailability of integration API in EF Core this method use reflection and could became broken after EF Core update.
    /// </summary>
    /// <param name="query">EF Core query.</param>
    /// <returns>Current <see cref="DataLinqContext"/> instance.</returns>
    public virtual DataLinqContext? GetCurrentContext(IQueryable query)
    {
        if (query is DataQuery dataQuery)
            return dataQuery.Context;

        throw new LinqToDBForDataLinqToolsException("Cannot find DataContext in query.");


        //var compilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance)!;
        //var compiler = (QueryCompiler)compilerField.GetValue(query.Provider)!;

        //var queryContextFactoryField = compiler.GetType().GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);

        //if (queryContextFactoryField == null)
        //    throw new LinqToDBForDataLinqToolsException($"Can not find private field '{compiler.GetType()}._queryContextFactory' in current EFCore Version.");

        //if (queryContextFactoryField.GetValue(compiler) is not RelationalQueryContextFactory queryContextFactory)
        //    throw new LinqToDBForDataLinqToolsException("LinqToDB Tools for EFCore support only Relational Databases.");

        //var dependenciesProperty = typeof(RelationalQueryContextFactory).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);

        //if (dependenciesProperty == null)
        //    throw new LinqToDBForDataLinqToolsException($"Can not find protected property '{nameof(RelationalQueryContextFactory)}.Dependencies' in current EFCore Version.");

        //var dependencies = (QueryContextDependencies)dependenciesProperty.GetValue(queryContextFactory)!;

        //return dependencies.CurrentContext?.Context;
    }

    /// <summary>
    /// Extracts EF Core connection information object from <see cref="IDataLinqContextOptions"/>.
    /// </summary>
    /// <param name="options"><see cref="IDataLinqContextOptions"/> instance.</param>
    /// <returns>EF Core connection data.</returns>
    public virtual DataLinqConnectionInfo ExtractConnectionInfo(DataLinqContext? context)
    {
        return new DataLinqConnectionInfo
        {
            ConnectionString = context?.Connection?.ConnectionString,
            Connection = context?.Connection
        };
    }

    /// <summary>
    /// Extracts EF Core data model instance from <see cref="IDataLinqContextOptions"/>.
    /// </summary>
    /// <param name="options"><see cref="IDataLinqContextOptions"/> instance.</param>
    /// <returns>EF Core data model instance.</returns>
    public virtual System.Data.Linq.Mapping.MetaModel? ExtractModel(DataLinqContext? context)
    {
        return context?.Mapping;
    }

    /// <summary>
    /// Logs lin2db trace event to logger.
    /// </summary>
    /// <param name="info">lin2db trace event.</param>
    /// <param name="logger">Logger instance.</param>
    public virtual void LogConnectionTrace(TraceInfo info, ILogger logger)
    {
        var logLevel = info.TraceLevel switch
        {
            TraceLevel.Off => LogLevel.None,
            TraceLevel.Error => LogLevel.Error,
            TraceLevel.Warning => LogLevel.Warning,
            TraceLevel.Info => LogLevel.Information,
            TraceLevel.Verbose => LogLevel.Debug,
            _ => LogLevel.Trace,
        };

#pragma warning disable CA1848 // Use the LoggerMessage delegates
        using var _ = logger.BeginScope("TraceInfoStep: {TraceInfoStep}, IsAsync: {IsAsync}", info.TraceInfoStep, info.IsAsync);

        switch (info.TraceInfoStep)
        {
            case TraceInfoStep.BeforeExecute:
                logger.Log(logLevel, "{SqlText}", info.SqlText);
                break;

            case TraceInfoStep.AfterExecute:
                if (info.RecordsAffected is null)
                {
                    logger.Log(logLevel, "Query Execution Time: {ExecutionTime}.", info.ExecutionTime);
                }
                else
                {
                    logger.Log(logLevel, "Query Execution Time: {ExecutionTime}. Records Affected: {RecordsAffected}.", info.ExecutionTime, info.RecordsAffected);
                }
                break;

            case TraceInfoStep.Error:
                {
                    logger.Log(logLevel, info.Exception, "Failed executing command.");
                    break;
                }

            case TraceInfoStep.Completed:
                {
                    if (info.RecordsAffected is null)
                    {
                        logger.Log(logLevel, "Total Execution Time: {TotalExecutionTime}.", info.ExecutionTime);
                    }
                    else
                    {
                        logger.Log(logLevel, "Total Execution Time: {TotalExecutionTime}. Rows Count: {RecordsAffected}.", info.ExecutionTime, info.RecordsAffected);
                    }
                    break;
                }
        }
#pragma warning restore CA1848 // Use the LoggerMessage delegates
    }

    /// <summary>
    /// Gets or sets default provider version for SQL Server. Set to <see cref="SqlServerVersion.v2008"/> dialect.
    /// </summary>
    public static SqlServerVersion SqlServerDefaultVersion { get; set; } = SqlServerVersion.v2008;

    /// <summary>
    /// Enables attaching entities to change tracker.
    /// Entities will be attached only if AsNoTracking() is not used in query and DataLinqContext is configured to track entities. 
    /// </summary>
    public virtual bool EnableChangeTracker { get; set; } = true;
}
