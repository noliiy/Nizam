using Nizam.Desktop.Models;
using Nizam.Desktop.ViewModels;

namespace Nizam.Desktop.Controls;

public static class ActivityGridFilter
{
    public static IEnumerable<ActivityRow> Filter(
        IEnumerable<ActivityRow> rows,
        string? searchText = null,
        bool? criticalOnly = null)
    {
        IEnumerable<ActivityRow> q = rows;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var s = searchText.Trim();
            q = q.Where(r =>
                r.Code.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (criticalOnly == true)
            q = q.Where(r => r.IsCritical);

        return q;
    }

    public static IEnumerable<ActivityRow> Sort(
        IEnumerable<ActivityRow> rows,
        string column,
        bool ascending = true)
    {
        Func<ActivityRow, object?> key = column.ToLowerInvariant() switch
        {
            "kod" or "code" => r => r.Code,
            "ad" or "name" => r => r.Name,
            "süre" or "sure" or "duration" => r => r.DurationDays,
            "başlangıç" or "baslangic" or "start" => r => r.Start,
            "bitiş" or "bitis" or "finish" => r => r.Finish,
            "toplam bolluk" or "float" => r => r.TotalFloatDays,
            "kritik" or "critical" => r => r.IsCritical,
            _ => r => r.Code
        };

        return ascending ? rows.OrderBy(key) : rows.OrderByDescending(key);
    }
}
