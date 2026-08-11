using Nizam.Desktop.Models;

namespace Nizam.Desktop.Controls;

/// <summary>Computed rectangle for a visible Gantt bar.</summary>
public sealed class GanttBarRect
{
    public Guid ActivityId { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public bool IsCritical { get; init; }
    public bool IsMilestone { get; init; }
    public decimal Progress { get; init; }
}

/// <summary>Viewport + time window used for virtualized layout.</summary>
public sealed class GanttRenderModel
{
    public DateTime WindowStart { get; init; }
    public DateTime WindowEnd { get; init; }
    public double ViewportWidth { get; init; }
    public double RowHeight { get; init; } = 28;
    public double BarHeight { get; init; } = 18;
    public int FirstVisibleRow { get; init; }
    public int VisibleRowCount { get; init; }
    public GanttZoomLevel ZoomLevel { get; init; } = GanttZoomLevel.Day;
}

/// <summary>
/// Pure C# virtualized Gantt layout: only bars intersecting the time window
/// and visible row range produce rectangles. No WPF dependency.
/// </summary>
public static class VirtualizedGanttLayout
{
    public static IReadOnlyList<GanttBarRect> ComputeBars(
        IReadOnlyList<GanttBarModel> bars,
        GanttRenderModel render)
    {
        if (bars.Count == 0 || render.ViewportWidth <= 0 || render.WindowEnd <= render.WindowStart)
            return Array.Empty<GanttBarRect>();

        var windowTicks = (render.WindowEnd - render.WindowStart).TotalMinutes;
        if (windowTicks <= 0)
            return Array.Empty<GanttBarRect>();

        var lastRow = render.FirstVisibleRow + Math.Max(0, render.VisibleRowCount) - 1;
        var results = new List<GanttBarRect>();

        foreach (var bar in bars)
        {
            if (bar.RowIndex < render.FirstVisibleRow || bar.RowIndex > lastRow)
                continue;

            if (bar.Finish <= render.WindowStart || bar.Start >= render.WindowEnd)
                continue;

            var clippedStart = bar.Start < render.WindowStart ? render.WindowStart : bar.Start;
            var clippedFinish = bar.Finish > render.WindowEnd ? render.WindowEnd : bar.Finish;

            var x = (clippedStart - render.WindowStart).TotalMinutes / windowTicks * render.ViewportWidth;
            var width = Math.Max(
                bar.IsMilestone ? 8 : 2,
                (clippedFinish - clippedStart).TotalMinutes / windowTicks * render.ViewportWidth);

            var y = (bar.RowIndex - render.FirstVisibleRow) * render.RowHeight
                    + (render.RowHeight - render.BarHeight) / 2.0;

            results.Add(new GanttBarRect
            {
                ActivityId = bar.ActivityId,
                X = x,
                Y = y,
                Width = width,
                Height = bar.IsMilestone ? render.BarHeight : render.BarHeight,
                IsCritical = bar.IsCritical,
                IsMilestone = bar.IsMilestone,
                Progress = bar.Progress
            });
        }

        return results;
    }
}

/// <summary>Alias / engine entry used by ViewModels and tests.</summary>
public static class GanttLayoutEngine
{
    public static IReadOnlyList<GanttBarRect> Layout(
        IReadOnlyList<GanttBarModel> bars,
        GanttRenderModel render)
        => VirtualizedGanttLayout.ComputeBars(bars, render);

    public static double PixelsPerMinute(GanttZoomLevel zoom) => zoom switch
    {
        GanttZoomLevel.Day => 2.0,
        GanttZoomLevel.Week => 0.4,
        GanttZoomLevel.Month => 0.1,
        _ => 1.0
    };

    public static (DateTime Start, DateTime End) DefaultWindow(DateTime dataDate, GanttZoomLevel zoom)
    {
        return zoom switch
        {
            GanttZoomLevel.Day => (dataDate.Date.AddDays(-7), dataDate.Date.AddDays(21)),
            GanttZoomLevel.Week => (dataDate.Date.AddDays(-28), dataDate.Date.AddDays(84)),
            GanttZoomLevel.Month => (dataDate.Date.AddMonths(-3), dataDate.Date.AddMonths(9)),
            _ => (dataDate.Date.AddDays(-14), dataDate.Date.AddDays(42))
        };
    }
}
