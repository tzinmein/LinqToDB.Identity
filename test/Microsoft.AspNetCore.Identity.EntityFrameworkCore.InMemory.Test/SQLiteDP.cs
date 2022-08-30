using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class SQLiteDP : SQLiteDataProvider
{
    public SQLiteDP(string name) : base(name) {}
}