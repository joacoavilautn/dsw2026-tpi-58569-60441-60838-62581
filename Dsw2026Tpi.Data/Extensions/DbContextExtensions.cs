using Dsw2026Tpi.Data.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dsw2026Tpi.Data.Extensions;

public static class DbContextExtensions
{
    public static void Seedwork<T>(this DbContext context, string dataSource) where T : class
    {
        if (context.Set<T>().Any()) return;
        var normalizedPath = dataSource.Replace('\\', Path.DirectorySeparatorChar);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, normalizedPath));
        var entities = JsonSerializer.Deserialize<List<T>>(json, JsonOptions.JsonSerializerOptions);
        if (entities == null || entities.Count == 0) return;
        context.Set<T>().AddRange(entities);
        context.SaveChanges();
    }
}