using System;
using System.Data;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.DataProvider.SapHana;

namespace LinqToDB.DataLinq
{
	using System.Diagnostics.CodeAnalysis;
	using Data;
	using DataProvider;
	using Linq;
	using Expressions;
	using LinqToDB.Interceptors;
	using System.Data.Common;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// linq2db EF.Core data connection.
    /// </summary>
    public class LinqToDBForDataLinqToolsDataConnection : DataConnection, IExpressionPreprocessor, IEntityServiceInterceptor
	{
		readonly System.Data.Linq.Mapping.MetaModel? _model;
		readonly Func<Expression, IDataContext, DataLinqContext?, System.Data.Linq.Mapping.MetaModel?, Expression>? _transformFunc;

		private System.Data.Linq.Mapping.MetaType?   _lastEntityType;
		private Type?          _lastType;
		private IStateManager? _stateManager;

		private static IMemoryCache _entityKeyGetterCache = new MemoryCache(new MemoryCacheOptions { });

		private static MethodInfo TryGetEntryMethodInfo =
			MemberHelper.MethodOf<IStateManager>(sm => sm.TryGetEntry(null!, Array.Empty<object>()));

		/// <summary>
		/// Change tracker enable flag.
		/// </summary>
		public bool      Tracking { get; set; }

		/// <summary>
		/// EF.Core database context.
		/// </summary>
		public DataLinqContext? Context  { get; }

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">linq2db database provider.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="model">EF.Core data model.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForDataLinqToolsDataConnection(
			DataLinqContext?     context,
			[NotNull]   IDataProvider dataProvider,
			[NotNull]   string        connectionString,
			            System.Data.Linq.Mapping.MetaModel?       model,
			Func<Expression, IDataContext, DataLinqContext?, System.Data.Linq.Mapping.MetaModel?, Expression>? transformFunc) : base(dataProvider, connectionString)
		{
			Context          = context;
			_model           = model;
			_transformFunc   = transformFunc;
			CopyDatabaseProperties();
			if (LinqToDBForDataLinqTools.EnableChangeTracker)
				AddInterceptor(this);
		}

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">linq2db database provider.</param>
		/// <param name="transaction">Database transaction.</param>
		/// <param name="model">EF.Core data model.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForDataLinqToolsDataConnection(
			DataLinqContext?      context,
			[NotNull]   IDataProvider dataProvider,
			[NotNull]   DbTransaction transaction,
			            System.Data.Linq.Mapping.MetaModel?       model,
			Func<Expression, IDataContext, DataLinqContext?, System.Data.Linq.Mapping.MetaModel?, Expression>? transformFunc)
				: base(dataProvider, transaction)
		{
			Context          = context;
			_model           = model;
			_transformFunc   = transformFunc;
			CopyDatabaseProperties();
			if (LinqToDBForDataLinqTools.EnableChangeTracker)
				AddInterceptor(this);
		}

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">linq2db database provider.</param>
		/// <param name="connection">Database connection instance.</param>
		/// <param name="model">EF.Core data model.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForDataLinqToolsDataConnection(
			DataLinqContext?     context,
			[NotNull]   IDataProvider dataProvider,
			[NotNull]   DbConnection  connection,
			            System.Data.Linq.Mapping.MetaModel?       model,
			Func<Expression, IDataContext, DataLinqContext?, System.Data.Linq.Mapping.MetaModel?, Expression>? transformFunc) : base(dataProvider, connection)
		{
			Context          = context;
			_model           = model;
			_transformFunc   = transformFunc;
			CopyDatabaseProperties();
			if (LinqToDBForDataLinqTools.EnableChangeTracker)
				AddInterceptor(this);
		}

		/// <summary>
		/// Converts expression using convert function, passed to context.
		/// </summary>
		/// <param name="expression">Expression to convert.</param>
		/// <returns>Converted expression.</returns>
		public Expression ProcessExpression(Expression expression)
		{
			if (_transformFunc == null)
				return expression;
			return _transformFunc(expression, this, Context, _model);
		}

		private sealed class TypeKey
		{
			public TypeKey(System.Data.Linq.Mapping.MetaType entityType, System.Data.Linq.Mapping.MetaModel? model)
			{
				EntityType = entityType;
				Model = model;
			}

			public System.Data.Linq.Mapping.MetaType EntityType { get; }
			public System.Data.Linq.Mapping.MetaModel?     Model      { get; }

			private bool Equals(TypeKey other)
			{
				return EntityType.Equals(other.EntityType) && Equals(Model, other.Model);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != GetType())
				{
					return false;
				}

				return Equals((TypeKey)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (EntityType.GetHashCode() * 397) ^ (Model != null ? Model.GetHashCode() : 0);
				}
			}
		}

		object IEntityServiceInterceptor.EntityCreated(EntityCreatedEventData eventData, object entity)
		{
			// Do not allow to store in ChangeTracker temporary tables
			if ((eventData.TableOptions & TableOptions.IsTemporaryOptionSet) != 0)
				return entity;

			// Do not allow to store in ChangeTracker tables from different server
			if (eventData.ServerName != null)
				return entity;

			if (!LinqToDBForDataLinqTools.EnableChangeTracker
			    || !Tracking
			    || !Context!.ObjectTrackingEnabled)
				return entity;

			var type = entity.GetType();
			if (_lastType != type)
			{
				_lastType       = type;
				_lastEntityType = Context.Mapping.GetMetaType(type);
			}

			if (_lastEntityType == null)
				return entity;

			// Do not allow to store in ChangeTracker tables that has different name
			if (eventData.TableName != _lastEntityType.Table.TableName)
				return entity;

			_stateManager ??= IStateManager.Get(Context);

			// It is a real pain to register entity in change tracker
			//
			InternalEntityEntry? entry = null;

			var kacheKey = new TypeKey (_lastEntityType, _model);

			var retrievalFunc = _entityKeyGetterCache.GetOrCreate(kacheKey, ce =>
			{
				ce.SlidingExpiration = TimeSpan.FromHours(1);
				return CreateEntityRetrievalFunc(((TypeKey)ce.Key).EntityType);
			});

			if (retrievalFunc == null)
				return entity;

			entry = retrievalFunc(_stateManager, entity);

			entry ??= _stateManager.StartTrackingFromQuery(_lastEntityType, entity);

			return entry.Entity;
		}

		private Func<IStateManager, object, InternalEntityEntry?>? CreateEntityRetrievalFunc(MetaType entityType)
		{
			var stateManagerParam = Expression.Parameter(typeof(IStateManager), "sm");
			var objParam = Expression.Parameter(typeof(object), "o");

			var variable = Expression.Variable(entityType.Type, "e");
			var assignExpr = Expression.Assign(variable, Expression.Convert(objParam, entityType.Type));

			var key = entityType.IdentityMembers;

            if (key.Count == 0)
				return null;

			var arrayExpr = key.Select(p =>
					Expression.Convert(Expression.MakeMemberAccess(variable, p.Member),
						typeof(object)))
				.ToArray();

			if (arrayExpr.Length == 0)
				return null;

			var newArrayExpression = Expression.NewArrayInit(typeof(object), arrayExpr);
			var body =
				Expression.Block(new[] { variable },
					assignExpr,
					Expression.Call(stateManagerParam, TryGetEntryMethodInfo, Expression.Constant(key),
						newArrayExpression));

			var lambda =
				Expression.Lambda<Func<IStateManager, object, InternalEntityEntry?>>(body, stateManagerParam, objParam);

			return lambda.Compile();
		}

		private void CopyDatabaseProperties()
		{
			var commandTimeout = Context?.CommandTimeout;
			if (commandTimeout != null)
				CommandTimeout = commandTimeout.Value;
		}
	}
}

interface IStateManager
{
    InternalEntityEntry? TryGetEntry(object value, object[] objects);

	public static IStateManager Get(DataLinqContext context)
	{
		throw new NotImplementedException();
	}

    InternalEntityEntry? StartTrackingFromQuery(MetaType lastEntityType, object entity);
}

//public class StateManager : IStateManager
//{

//}

class InternalEntityEntry
{
    public object Entity { get; internal set; }
}