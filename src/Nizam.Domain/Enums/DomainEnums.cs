namespace Nizam.Domain.Enums;

public enum ProjectStatus
{
    Draft = 0,
    Planning = 1,
    Approved = 2,
    Active = 3,
    OnHold = 4,
    Completed = 5,
    Cancelled = 6,
    Archived = 7
}

public enum ActivityType
{
    TaskDependent = 0,
    ResourceDependent = 1,
    StartMilestone = 2,
    FinishMilestone = 3,
    LevelOfEffort = 4,
    WbsSummary = 5
}

public enum ActivityStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    OnHold = 3
}

public enum RelationshipType
{
    FinishToStart = 0,
    StartToStart = 1,
    FinishToFinish = 2,
    StartToFinish = 3
}

public enum ConstraintType
{
    None = 0,
    StartOn = 1,
    StartOnOrAfter = 2,
    StartOnOrBefore = 3,
    FinishOn = 4,
    FinishOnOrAfter = 5,
    FinishOnOrBefore = 6,
    MandatoryStart = 7,
    MandatoryFinish = 8
}

public enum ScheduleRunStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum BaselineType
{
    OriginalContract = 0,
    Approved = 1,
    Revised = 2,
    InternalTarget = 3
}

public enum BaselineStatus
{
    Draft = 0,
    Approved = 1
}
