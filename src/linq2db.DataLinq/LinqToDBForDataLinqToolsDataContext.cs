using System;
using System.Linq.Expressions;

namespace LinqToDB.DataLinq;


using DataProvider;
using Linq;

/// <summary>
/// linq2db EF.Core data context.
/// </summary>
public class LinqToDBForDataLinqToolsDataContext : DataContext, IExpressionPreprocessor
{
    readonly DataLinqContext? _context;
    readonly System.Data.Linq.Mapping.MetaModel _model;
    readonly Func<Expression, IDataContext, DataLinqContext?, System.Data.Linq.Mapping.MetaModel, Expression>? _transformFunc;

    /// <summary>
    /// Creates instance of context.
    /// </summary>
    /// <param name="context">EF.Core database context.</param>
    /// <param name="dataProvider">lin2db database provider instance.</param>
    /// <param name="connectionString">Connection string.</param>
    /// <param name="model">EF.Core model.</param>
    /// <param name="transformFunc">Expression converter.</param>
    public LinqToDBForDataLinqToolsDataContext(
        DataLinqContext? context,
        IDataProvider dataProvider,
        string connectionString,
        System.Data.Linq.Mapping.MetaModel model,
        Func<Expression, IDataContext, DataLinqContext?, System.Data.Linq.Mapping.MetaModel, Expression>? transformFunc) : base(dataProvider, connectionString)
    {
        _context = context;
        _model = model;
        _transformFunc = transformFunc;
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
        return _transformFunc(expression, this, _context, _model);
    }
}
