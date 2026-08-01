using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Dsw2026Tpi.Data;

public class JsonHolidayProvider : IHolidayProvider
{
    private readonly ILogger<JsonHolidayProvider> _logger;
    private HashSet<DateOnly>? _cachedHolidays;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public JsonHolidayProvider(ILogger<JsonHolidayProvider> logger)
    {
        _logger = logger;
    }

    private record HolidayItemDto(DateOnly date);

    public async Task<HashSet<DateOnly>> GetHolidayAsync()
    {
        if (_cachedHolidays != null)
        {
            return _cachedHolidays;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_cachedHolidays != null)
                return _cachedHolidays;
            var filePath = Path.Combine(AppContext.BaseDirectory, "Sources", "feriados.json");

            if (!File.Exists(filePath))
                filePath = Path.Combine(AppContext.BaseDirectory, "feriados.json");

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Archivo feriados.json no encontrado en la ruta {filePath}");
                _cachedHolidays = [];
                return _cachedHolidays;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<HolidayItemDto>>(jsonContent, options);

            _cachedHolidays = items?.Select(x => x.date).ToHashSet() ?? [];
            return _cachedHolidays;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
