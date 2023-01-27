using System.Data.Common;

namespace LinqToDB.DataLinq;

public sealed class DataLinqProviderInfo
{
    /// <summary>
    /// Gets or sets database connection instance.
    /// </summary>
    public DbConnection? Connection { get; set; }

    /// <summary>
    /// Gets or sets EF.Core context instance.
    /// </summary>
    public DataLinqContext? Context { get; set; }

    ///// <summary>
    ///// Gets or sets EF.Core context options instance.
    ///// </summary>
    //public IDbContextOptions? Options { get; set; }
}
