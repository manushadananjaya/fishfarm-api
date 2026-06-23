using FishFarm.Domain.Common;
using FishFarm.Domain.Enums;

namespace FishFarm.Domain.Entities;

public sealed class FarmWorker : BaseAuditableEntity
{
    public Guid FishFarmId { get; set; }

    public Guid PersonId { get; set; }

    public WorkerPosition Position { get; set; }

    public FishFarm FishFarm { get; set; } = default!;
    public Person Person { get; set; } = default!;
}
