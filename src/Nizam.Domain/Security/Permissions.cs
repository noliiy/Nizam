namespace Nizam.Domain.Security;

public static class Permissions
{
    public const string OrganizationManage = "organization.manage";
    public const string UserCreate = "user.create";
    public const string UserRead = "user.read";
    public const string ProjectCreate = "project.create";
    public const string ProjectRead = "project.read";
    public const string ProjectUpdate = "project.update";
    public const string ProjectDelete = "project.delete";
    public const string WbsCreate = "wbs.create";
    public const string WbsUpdate = "wbs.update";
    public const string WbsDelete = "wbs.delete";
    public const string ActivityCreate = "activity.create";
    public const string ActivityUpdate = "activity.update";
    public const string ActivityDelete = "activity.delete";
    public const string ScheduleRun = "schedule.run";
    public const string BaselineCreate = "baseline.create";
    public const string BaselineApprove = "baseline.approve";
    public const string ReportGenerate = "report.generate";
    public const string AiUse = "ai.use";

    public static IReadOnlyList<string> All { get; } =
    [
        OrganizationManage, UserCreate, UserRead,
        ProjectCreate, ProjectRead, ProjectUpdate, ProjectDelete,
        WbsCreate, WbsUpdate, WbsDelete,
        ActivityCreate, ActivityUpdate, ActivityDelete,
        ScheduleRun, BaselineCreate, BaselineApprove,
        ReportGenerate, AiUse
    ];
}

public static class RoleNames
{
    public const string OrganizationOwner = "OrganizationOwner";
    public const string Administrator = "Administrator";
    public const string ProjectManager = "ProjectManager";
    public const string PlanningEngineer = "PlanningEngineer";
    public const string Viewer = "Viewer";
}
