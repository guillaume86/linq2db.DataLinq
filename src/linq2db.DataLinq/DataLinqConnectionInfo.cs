﻿using System.Data.Common;

namespace LinqToDB.DataLinq;

public sealed class DataLinqConnectionInfo
{
    /// <summary>
    /// Gets or sets database connection instance.
    /// </summary>
    public DbConnection? Connection { get; set; }

    /// <summary>
    /// Gets or sets database connection string.
    /// </summary>
    public string? ConnectionString { get; set; }
}
