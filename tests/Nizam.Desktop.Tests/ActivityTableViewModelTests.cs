using FluentAssertions;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;
using Nizam.Desktop.ViewModels;

namespace Nizam.Desktop.Tests;

public class ActivityTableViewModelTests
{
    private readonly FakeNizamApiClient _api = new();
    private readonly FakeDialogService _dialogs = new();

    private ActivityTableViewModel CreateSut()
    {
        var vm = new ActivityTableViewModel(_api, _dialogs);
        vm.SetProject(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"));
        return vm;
    }

    [Fact]
    public async Task NewActivity_Requires_Code_And_Name()
    {
        var vm = CreateSut();
        await vm.NewActivityCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Aktivite kodu ve adı zorunludur.");
        _api.CreatedActivities.Should().BeEmpty();
    }

    [Fact]
    public async Task NewActivity_Creates_And_Adds_Row()
    {
        var vm = CreateSut();
        vm.NewCode = "A1000";
        vm.NewName = "Kazı";
        vm.NewDurationDays = 2;

        await vm.NewActivityCommand.ExecuteAsync(null);

        vm.Activities.Should().ContainSingle();
        vm.Activities[0].Code.Should().Be("A1000");
        vm.Activities[0].Name.Should().Be("Kazı");
        vm.Activities[0].DurationDays.Should().BeApproximately(2, 0.01);
        _api.CreatedActivities.Should().ContainSingle();
    }

    [Fact]
    public async Task RunSchedule_Reloads_Activities()
    {
        var vm = CreateSut();
        _api.Activities.Add(new ActivityDto
        {
            Id = Guid.NewGuid(),
            ProjectId = vm.ProjectId,
            WbsId = vm.DefaultWbsId,
            ActivityCode = "A1",
            Name = "Test",
            OriginalDurationMinutes = 480,
            EarlyStart = new DateTime(2024, 1, 1, 8, 0, 0),
            EarlyFinish = new DateTime(2024, 1, 1, 17, 0, 0),
            IsCritical = true,
            TotalFloatMinutes = 0
        });

        await vm.RunScheduleCommand.ExecuteAsync(null);

        _api.ScheduleRuns.Should().Be(1);
        vm.Activities.Should().ContainSingle(a => a.Code == "A1" && a.IsCritical);
        _dialogs.Infos.Should().Contain(i => i.Contains("Zamanlama"));
    }

    [Fact]
    public async Task AddFsRelationship_Requires_Both_Ends()
    {
        var vm = CreateSut();
        await vm.AddFsRelationshipCommand.ExecuteAsync(null);
        vm.ErrorMessage.Should().Be("FS ilişkisi için öncül ve ardıl aktivite seçin.");
    }

    [Fact]
    public async Task AddFsRelationship_Calls_Api()
    {
        var vm = CreateSut();
        var pred = Guid.NewGuid();
        var succ = Guid.NewGuid();
        vm.FsPredecessorId = pred;
        vm.FsSuccessorId = succ;

        await vm.AddFsRelationshipCommand.ExecuteAsync(null);

        _api.Relationships.Should().ContainSingle(r =>
            r.PredecessorActivityId == pred &&
            r.SuccessorActivityId == succ &&
            r.RelationshipType == 0);
    }
}
