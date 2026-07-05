namespace DaxaPos.Persistence.Seed;

/// <summary>
/// Fixed GUIDs for the seeded <c>TaxDefinitionTemplate</c> catalogue (PLAN-0004 Milestone B).
/// Deterministic by design — <c>HasData</c> seed rows must have stable keys across migrations.
/// </summary>
internal static class TaxSeedIds
{
    public static readonly Guid AuGst10TemplateId = new("00000000-0000-0000-0003-000000000001");
    public static readonly Guid AuGstFreeTemplateId = new("00000000-0000-0000-0003-000000000002");
    public static readonly Guid NzGst15TemplateId = new("00000000-0000-0000-0003-000000000003");
    public static readonly Guid NzZeroRatedTemplateId = new("00000000-0000-0000-0003-000000000004");
    public static readonly Guid NzExemptTemplateId = new("00000000-0000-0000-0003-000000000005");
}
