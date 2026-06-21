namespace FishFarm.Domain.Enums;

/// <summary>
/// Role / position of a worker on a fish farm.
/// Explicit integer values are mandatory: never reorder or remove values
/// because the database stores these as INT. Add new values only at the end.
/// </summary>
public enum WorkerPosition
{
    CEO     = 1,
    Worker  = 2,
    Captain = 3
}
