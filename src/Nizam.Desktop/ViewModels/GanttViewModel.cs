using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Controls;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class GanttViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly IDialogService _dialogs;

    public GanttViewModel(INizamApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
        ZoomLevel = GanttZoomLevel.Week;
        DataDate = DateTime.Today;
        RecalculateWindow();
    }

    public ObservableCollection<GanttBarModel> Bars { get; } = new();
    public ObservableCollection<GanttBarRect> VisibleRects { get; } = new();

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private GanttZoomLevel _zoomLevel;

    [ObservableProperty]
    private DateTime _dataDate;

    [ObservableProperty]
    private DateTime _windowStart;

    [ObservableProperty]
    private DateTime _windowEnd;

    [ObservableProperty]
    private double _viewportWidth = 1200;

    [ObservableProperty]
    private int _firstVisibleRow;

    [ObservableProperty]
    private int _visibleRowCount = 40;

    [ObservableProperty]
    private string? _errorMessage;

    public string ZoomDayLabel => "Gün";
    public string ZoomWeekLabel => "Hafta";
    public string ZoomMonthLabel => "Ay";
    public string DataDateLabel => "Veri Tarihi";

    public void SetProject(Guid projectId) => ProjectId = projectId;

    partial void OnZoomLevelChanged(GanttZoomLevel value) => RecalculateWindow();
    partial void OnDataDateChanged(DateTime value) => RecalculateWindow();

    private void RecalculateWindow()
    {
        var (start, end) = GanttLayoutEngine.DefaultWindow(DataDate, ZoomLevel);
        WindowStart = start;
        WindowEnd = end;
        Relayout();
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (ProjectId == Guid.Empty)
            return;

        try
        {
            var activities = await _api.GetActivitiesAsync(ProjectId, cancellationToken);
            Bars.Clear();
            var index = 0;
            foreach (var a in activities)
            {
                if (a.EarlyStart is null || a.EarlyFinish is null)
                {
                    index++;
                    continue;
                }

                Bars.Add(new GanttBarModel
                {
                    ActivityId = a.Id,
                    ActivityCode = a.ActivityCode,
                    Name = a.Name,
                    Start = a.EarlyStart.Value,
                    Finish = a.EarlyFinish.Value,
                    IsCritical = a.IsCritical,
                    IsMilestone = a.OriginalDurationMinutes == 0,
                    Progress = a.PercentComplete,
                    RowIndex = index
                });
                index++;
            }

            Relayout();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
    }

    public void SyncFromActivities(IEnumerable<ActivityRow> activities)
    {
        Bars.Clear();
        var index = 0;
        foreach (var a in activities)
        {
            if (a.Start is null || a.Finish is null)
            {
                index++;
                continue;
            }

            Bars.Add(new GanttBarModel
            {
                ActivityId = a.Id,
                ActivityCode = a.Code,
                Name = a.Name,
                Start = a.Start.Value,
                Finish = a.Finish.Value,
                IsCritical = a.IsCritical,
                IsMilestone = a.DurationDays <= 0,
                Progress = a.PercentComplete,
                RowIndex = index
            });
            index++;
        }

        Relayout();
    }

    [RelayCommand]
    private void SetZoom(GanttZoomLevel level) => ZoomLevel = level;

    [RelayCommand]
    private async Task ApplyDataDateAsync(CancellationToken cancellationToken)
    {
        if (ProjectId == Guid.Empty)
            return;

        try
        {
            await _api.SetDataDateAsync(ProjectId, DataDate, cancellationToken);
            RecalculateWindow();
            _dialogs.ShowInfo($"Veri tarihi güncellendi: {DataDate:dd.MM.yyyy}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
    }

    public void Relayout()
    {
        var render = new GanttRenderModel
        {
            WindowStart = WindowStart,
            WindowEnd = WindowEnd,
            ViewportWidth = ViewportWidth,
            FirstVisibleRow = FirstVisibleRow,
            VisibleRowCount = VisibleRowCount,
            ZoomLevel = ZoomLevel
        };

        var rects = GanttLayoutEngine.Layout(Bars.ToList(), render);
        VisibleRects.Clear();
        foreach (var r in rects)
            VisibleRects.Add(r);
    }
}
