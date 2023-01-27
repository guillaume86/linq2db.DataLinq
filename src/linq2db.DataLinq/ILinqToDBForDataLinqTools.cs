using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataLinq;

using DataProvider;
using Mapping;
using Metadata;
using Data;

/// <summary>
/// Interface for EF Core - LINQ To DB integration bridge.
/// </summary>
public interface ILinqToDBForDataLinqTools
{
    /// <summary>
    /// Clears internal caches
    /// </summary>
    void ClearCaches();

    /// <summary>
    /// Returns LINQ To DB provider, based on provider data from EF Core.
    /// </summary>
    /// <param name="providerInfo">Provider information, extracted from EF Core.</param>
    /// <param name="connectionInfo">Database connection information.</param>
    /// <returns>LINQ TO DB provider instance.</returns>
    IDataProvider? GetDataProvider(DataLinqProviderInfo providerInfo, DataLinqConnectionInfo connectionInfo);

    /// <summary>
    /// Creates metadata provider for specified EF Core data model. Default implementation uses
    /// <see cref="DataLinqMetadataReader"/> metadata provider.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="accessor">EF Core service provider.</param>
    /// <returns>LINQ To DB metadata provider for specified EF Core model.</returns>
    IMetadataReader? CreateMetadataReader(
        System.Data.Linq.Mapping.MetaModel? model);

    /// <summary>
    /// Creates mapping schema using provided EF Core data model and metadata provider.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
    /// <param name="convertorSelector">EF Core registry for type conversion.</param>
    /// <returns>Mapping schema for provided EF Core model.</returns>
    MappingSchema CreateMappingSchema(System.Data.Linq.Mapping.MetaModel model, IMetadataReader metadataReader);

    /// <summary>
    /// Returns mapping schema using provided EF Core data model and metadata provider.
    /// </summary>
    /// <param name="model">EF Core data model.</param>
    /// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
    /// <param name="convertorSelector">EF Core registry for type conversion.</param>
    /// <returns>Mapping schema for provided EF Core model.</returns>
    MappingSchema GetMappingSchema(System.Data.Linq.Mapping.MetaModel model, IMetadataReader? metadataReader);

    /// <summary>
    /// Transforms EF Core expression tree to LINQ To DB expression.
    /// </summary>
    /// <param name="expression">EF Core expression tree.</param>
    /// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
    /// <param name="ctx">Optional DataLinqContext instance.</param>
    /// <param name="model">EF Core data model instance.</param>
    /// <returns>Transformed expression.</returns>
    Expression TransformExpression(Expression expression, IDataContext? dc, DataLinqContext? ctx, System.Data.Linq.Mapping.MetaModel? model);

    /// <summary>
    /// Extracts <see cref="DataLinqContext"/> instance from <see cref="IQueryable"/> object.
    /// </summary>
    /// <param name="query">EF Core query.</param>
    /// <returns>Current <see cref="DataLinqContext"/> instance.</returns>
    DataLinqContext? GetCurrentContext(IQueryable query);

    ///// <summary>
    ///// Extracts EF Core connection information object from <see cref="IDataLinqContextOptions"/>.
    ///// </summary>
    ///// <param name="options"><see cref="IDataLinqContextOptions"/> instance.</param>
    ///// <returns>EF Core connection data.</returns>
    //DataLinqConnectionInfo ExtractConnectionInfo(IDataLinqContextOptions? options);

    ///// <summary>
    ///// Extracts EF Core data model instance from <see cref="IDataLinqContextOptions"/>.
    ///// </summary>
    ///// <param name="options"><see cref="IDataLinqContextOptions"/> instance.</param>
    ///// <returns>EF Core data model instance.</returns>
    //System.Data.Linq.Mapping.MetaModel? ExtractModel(IDataLinqContextOptions? options);

    ///// <summary>
    ///// Creates logger used for logging Linq To DB connection calls.
    ///// </summary>
    ///// <param name="options"><see cref="IDataLinqContextOptions"/> instance.</param>
    ///// <returns>Logger instance.</returns>
    //ILogger? CreateLogger(IDataLinqContextOptions? options);

    ///// <summary>
    ///// Logs DataConnection information.
    ///// </summary>
    ///// <param name="info"></param>
    ///// <param name="logger"></param>
    //void LogConnectionTrace(TraceInfo info, ILogger logger);

    /// <summary>
    /// Enables attaching entities to change tracker.
    /// Entities will be attached only if AsNoTracking() is not used in query and DataLinqContext is configured to track entities. 
    /// </summary>
    bool EnableChangeTracker { get; set; }
}
