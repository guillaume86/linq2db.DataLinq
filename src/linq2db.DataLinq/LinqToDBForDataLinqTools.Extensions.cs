using System.Data.Linq;

namespace LinqToDB.DataLinq;

public static partial class LinqToDBForDataLinqTools
{
    /// <summary>
    /// Converts EF.Core <see cref="DbSet{TEntity}"/> instance to LINQ To DB <see cref="ITable{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">Mapping entity type.</typeparam>
    /// <param name="table">EF.Core <see cref="DbSet{TEntity}"/> instance.</param>
    /// <returns>LINQ To DB <see cref="ITable{T}"/> instance.</returns>
    public static ITable<T> ToLinqToDBTable<T>(this Table<T> table)
        where T : class
    {
        var context = table.Context;
        if (context == null)
            throw new LinqToDBForDataLinqToolsException($"Can not load current context from {nameof(table)}");

        var dc = CreateLinqToDbContext(context);
        return dc.GetTable<T>();
    }

    /// <summary>
    /// Converts EF.Core <see cref="DbSet{TEntity}"/> instance to LINQ To DB <see cref="ITable{T}"/> instance
    /// using existing LINQ To DB <see cref="IDataContext"/> instance.
    /// </summary>
    /// <typeparam name="T">Mapping entity type.</typeparam>
    /// <param name="table">EF.Core <see cref="DbSet{TEntity}"/> instance.</param>
    /// <param name="dataContext">LINQ To DB data context instance.</param>
    /// <returns>LINQ To DB <see cref="ITable{T}"/> instance.</returns>
    public static ITable<T> ToLinqToDBTable<T>(this Table<T> table, IDataContext dataContext)
        where T : class
    {
        return dataContext.GetTable<T>();
    }
}
