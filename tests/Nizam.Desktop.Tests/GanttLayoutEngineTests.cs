using FluentAssertions;
using Nizam.Desktop.Controls;
using Nizam.Desktop.Models;

namespace Nizam.Desktop.Tests;

public class GanttLayoutEngineTests
{
    [Fact]
    public void Layout_Virtualizes_Only_Visible_Rows_In_Time_Window()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0);
        var bars = new List<GanttBarModel>
        {
            // Row 0 — fully in window
            new()
            {
                ActivityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Start = start.AddDays(1),
                Finish = start.AddDays(3),
                RowIndex = 0,
                IsCritical = true
            },
            // Row 1 — partially clipped at left
            new()
            {
                ActivityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Start = start.AddDays(-2),
                Finish = start.AddDays(2),
                RowIndex = 1
            },
            // Row 5 — outside visible row range
            new()
            {
                ActivityId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Start = start.AddDays(1),
                Finish = start.AddDays(2),
                RowIndex = 5
            },
            // Row 2 — entirely outside time window (after)
            new()
            {
                ActivityId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Start = start.AddDays(40),
                Finish = start.AddDays(42),
                RowIndex = 2
            }
        };

        var render = new GanttRenderModel
        {
            WindowStart = start,
            WindowEnd = start.AddDays(10),
            ViewportWidth = 1000,
            FirstVisibleRow = 0,
            VisibleRowCount = 3,
            RowHeight = 30,
            BarHeight = 20
        };

        var rects = GanttLayoutEngine.Layout(bars, render);

        rects.Should().HaveCount(2);
        rects.Select(r => r.ActivityId).Should().BeEquivalentTo(new[]
        {
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        });

        var barA = rects.Single(r => r.ActivityId == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        barA.X.Should().BeApproximately(100, 0.5); // 1 day / 10 days * 1000
        barA.Width.Should().BeApproximately(200, 0.5); // 2 days
        barA.Y.Should().BeApproximately(5, 0.1); // (30-20)/2
        barA.IsCritical.Should().BeTrue();

        var barB = rects.Single(r => r.ActivityId == Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        barB.X.Should().BeApproximately(0, 0.5); // clipped to window start
        barB.Width.Should().BeApproximately(200, 0.5); // 2 visible days
        barB.Y.Should().BeApproximately(35, 0.1); // row 1
    }

    [Fact]
    public void Layout_Returns_Empty_When_Viewport_Invalid()
    {
        var bars = new[]
        {
            new GanttBarModel
            {
                ActivityId = Guid.NewGuid(),
                Start = DateTime.Today,
                Finish = DateTime.Today.AddDays(1),
                RowIndex = 0
            }
        };

        var render = new GanttRenderModel
        {
            WindowStart = DateTime.Today,
            WindowEnd = DateTime.Today,
            ViewportWidth = 100,
            FirstVisibleRow = 0,
            VisibleRowCount = 10
        };

        GanttLayoutEngine.Layout(bars, render).Should().BeEmpty();
    }

    [Fact]
    public void Milestone_Has_Minimum_Width()
    {
        var start = new DateTime(2024, 6, 1);
        var bars = new[]
        {
            new GanttBarModel
            {
                ActivityId = Guid.NewGuid(),
                Start = start.AddDays(5),
                Finish = start.AddDays(5),
                RowIndex = 0,
                IsMilestone = true
            }
        };

        var render = new GanttRenderModel
        {
            WindowStart = start,
            WindowEnd = start.AddDays(10),
            ViewportWidth = 1000,
            FirstVisibleRow = 0,
            VisibleRowCount = 5
        };

        var rect = GanttLayoutEngine.Layout(bars, render).Should().ContainSingle().Subject;
        rect.Width.Should().Be(8);
        rect.IsMilestone.Should().BeTrue();
    }
}
