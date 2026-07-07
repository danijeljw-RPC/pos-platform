namespace DaxaPos.Web.Api;

// Client-side mirrors of the DaxaPos.Api Identity endpoint contracts (PLAN-0003). Kept local to
// DaxaPos.Web rather than shared via project reference: the PWA only ever talks to the API over
// HTTP/JSON, never in-process, so there is no compile-time coupling to gain from sharing the
// server's record types.

public sealed record DeviceRegistrationRequest(string Pin, string DeviceType, string? Name);

public sealed record DeviceRegistrationResult(
    Guid DeviceId,
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    string DeviceType,
    string Name,
    string DeviceToken);

public sealed record StaffPinLoginRequest(Guid LocationId, string StaffCode, string Pin);

public sealed record StaffPinLoginResult(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    Guid StaffMemberId,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AuthContextResult(
    Guid TenantId,
    Guid? OrganisationId,
    Guid? LocationId,
    Guid? TerminalId,
    Guid? UserId,
    Guid? StaffMemberId,
    Guid? DeviceId,
    string AuthMethod,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

// Back Office (PLAN-0006 Milestone B) client-side mirrors. Local login is username/password
// (ADR-0013 local admin portal login), independent of device registration — no DeviceContext is
// needed to reach Back Office at all.

public sealed record LocalLoginRequest(string Email, string Password);

public sealed record LocalLoginResult(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record LocationResult(Guid Id, Guid OrganisationId, string Name, bool IsActive);

public sealed record DeviceResult(Guid Id, Guid LocationId, string DeviceType, string Name, bool HasActiveCredential);

public sealed record CreateDeviceRegistrationPinRequest(Guid LocationId, int? MaxUses);

/// <summary>Server returns the raw PIN exactly once — nothing later can retrieve it again.</summary>
public sealed record DeviceRegistrationPinCreatedResult(Guid Id, Guid LocationId, string Pin, DateTimeOffset ExpiresAtUtc, int MaxUses);

public sealed record DeviceRegistrationPinResult(
    Guid Id,
    Guid LocationId,
    DateTimeOffset ExpiresAtUtc,
    int MaxUses,
    int UsedCount,
    DateTimeOffset? RevokedAtUtc);

public sealed record ProductCategoryResult(Guid Id, string Name, int DisplayOrder, bool IsActive);

public sealed record ProductResult(Guid Id, Guid ProductCategoryId, string Name, string? Sku, decimal BasePrice, bool IsActive, bool IsArchived);

public sealed record MenuResult(Guid Id, Guid? LocationId, string Name, bool IsActive);

// Terminal sales screen (PLAN-0006 Milestone C) client-side mirrors of
// ResolvedMenuEndpoints' response DTOs. TaxTreatment is deliberately omitted: it's an unconfigured
// numeric enum server-side (no JsonStringEnumConverter) and this milestone's UI never needs to
// interpret it — the resolved Price/IsTaxInclusive is already the full answer for display.

// Milestone C.1: modifier group/option data now included in the resolved-menu projection so the
// sales screen can prompt for required/optional selections before adding a line.

public sealed record ResolvedModifierResult(Guid Id, string Name, decimal PriceDelta);

public sealed record ResolvedModifierGroupResult(
    Guid Id,
    string Name,
    int SelectionMin,
    int SelectionMax,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<ResolvedModifierResult> Modifiers);

public sealed record ResolvedMenuItemResult(
    Guid ProductId,
    string ProductName,
    int DisplayOrder,
    decimal Price,
    bool IsTaxInclusive,
    string TaxCategoryCode,
    IReadOnlyList<ResolvedModifierGroupResult> ModifierGroups);

public sealed record ResolvedMenuSectionResult(
    Guid MenuId,
    Guid MenuSectionId,
    string SectionName,
    int DisplayOrder,
    IReadOnlyList<ResolvedMenuItemResult> Items);

public sealed record ResolvedMenuResult(Guid LocationId, IReadOnlyList<ResolvedMenuSectionResult> Sections);

// Terminal sales screen real order wiring (PLAN-0006 Milestone C.1) — client mirrors of
// OrderEndpoints' request/response DTOs. Only the fields the sales screen actually reads/writes
// are mirrored (no ProductVariantId/tax-line detail — the sales screen shows server totals, it
// never re-derives them).

public sealed record CreateOrderRequest(Guid TerminalId, string? Notes = null);

public sealed record AddOrderLineRequest(Guid ProductId, int Quantity, IReadOnlyList<Guid>? ModifierIds, string? Notes);

/// <summary>Mirrors <c>DaxaPos.Domain.Enums.OrderStatus</c> ordinal-for-ordinal — the API has no
/// <c>JsonStringEnumConverter</c> configured, so this serialises as the same integer.</summary>
public enum OrderStatusResult
{
    Open = 0,
    Held = 1,
    Completed = 2,
    Voided = 3,
    Cancelled = 4,
}

/// <summary>Mirrors <c>DaxaPos.Domain.Enums.OrderLineStatus</c> ordinal-for-ordinal (see
/// <see cref="OrderStatusResult"/> remarks).</summary>
public enum OrderLineStatusResult
{
    Active = 0,
    Voided = 1,
}

public sealed record OrderLineModifierResult(Guid Id, Guid ModifierId, string NameSnapshot, decimal PriceDeltaSnapshot);

public sealed record OrderLineResult(
    Guid Id,
    Guid ProductId,
    int Quantity,
    string ProductNameSnapshot,
    decimal UnitPriceSnapshot,
    decimal LineTotalAmount,
    string? Notes,
    OrderLineStatusResult Status,
    IReadOnlyList<OrderLineModifierResult> Modifiers);

public sealed record OrderResult(
    Guid Id,
    Guid TerminalId,
    OrderStatusResult Status,
    decimal SubtotalAmount,
    decimal TotalTaxAmount,
    decimal GrandTotalAmount,
    IReadOnlyList<OrderLineResult> Lines);

// Back Office Terminals page (PLAN-0006 Milestone C.1) — admin-only, explicit-bearer, mirrors
// TerminalEndpoints' DTOs.

public sealed record TerminalResult(Guid Id, Guid LocationId, Guid? DeviceId, string Name, bool IsActive);

public sealed record CreateTerminalRequest(string Name, Guid LocationId);

public sealed record AssignTerminalDeviceRequest(Guid? DeviceId);
