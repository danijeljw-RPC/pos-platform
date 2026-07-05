using System.Text.Json;
using DaxaPos.Application.Events;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;

namespace DaxaPos.Api.Audit;

/// <summary>
/// In-process domain-event handlers writing <see cref="AuditEvent"/> rows (ADR-0010, ADR-0014's
/// Handler I/O Rule — a fast, local DB write is fine directly in a handler). Hosted in
/// <c>DaxaPos.Api</c>, not <c>DaxaPos.Persistence</c>, per ADR-0015 §4: no reference-graph change
/// for a single DB-touching handler consumer.
/// </summary>
public sealed class LocalUserLoginSucceededAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<LocalUserLoginSucceededDomainEvent>
{
    public async Task HandleAsync(LocalUserLoginSucceededDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = "LocalUserLoginSucceeded",
            EntityType = nameof(AuthSession),
            EntityId = domainEvent.AuthSessionId,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Only raised when a real <see cref="User"/> was matched (wrong password, or locked out) — see
/// <see cref="LocalUserLoginFailedDomainEvent"/>.
/// </summary>
public sealed class LocalUserLoginFailedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<LocalUserLoginFailedDomainEvent>
{
    public async Task HandleAsync(LocalUserLoginFailedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = "LocalUserLoginFailed",
            EntityType = nameof(User),
            EntityId = domainEvent.UserId,
            Reason = domainEvent.FailureReason,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AuthSessionRevokedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<AuthSessionRevokedDomainEvent>
{
    public async Task HandleAsync(AuthSessionRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            StaffMemberId = domainEvent.StaffMemberId,
            EventType = "AuthSessionRevoked",
            EntityType = nameof(AuthSession),
            EntityId = domainEvent.AuthSessionId,
            Reason = domainEvent.Reason,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Writes one <see cref="AuditEvent"/> row per lifecycle action on an <see cref="Organisation"/>/
/// <see cref="Location"/>/<see cref="Terminal"/> (PLAN-0003 Milestone D). <c>EventType</c> is built
/// as <c>$"{EntityType}{Action}"</c> (e.g. <c>"OrganisationCreated"</c>), matching the
/// <c>"LocalUserLoginSucceeded"</c>-style naming already used by the Milestone C handlers above.
/// </summary>
public sealed class OrganisationLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<OrganisationLifecycleDomainEvent>
{
    public async Task HandleAsync(OrganisationLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(Organisation)}{domainEvent.Action}",
            EntityType = nameof(Organisation),
            EntityId = domainEvent.OrganisationId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class LocationLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<LocationLifecycleDomainEvent>
{
    public async Task HandleAsync(LocationLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(Location)}{domainEvent.Action}",
            EntityType = nameof(Location),
            EntityId = domainEvent.LocationId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TerminalLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<TerminalLifecycleDomainEvent>
{
    public async Task HandleAsync(TerminalLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            TerminalId = domainEvent.TerminalId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(Terminal)}{domainEvent.Action}",
            EntityType = nameof(Terminal),
            EntityId = domainEvent.TerminalId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PLAN-0003 Milestone E device-lifecycle audit handlers (ADR-0008's audit requirements). Note
/// Before/AfterValue are jsonb columns — any snapshot must be serialized JSON, never a bare
/// string (see the Milestone D worker-notes bug report).
/// </summary>
public sealed class DeviceRegistrationPinCreatedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<DeviceRegistrationPinCreatedDomainEvent>
{
    public async Task HandleAsync(DeviceRegistrationPinCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            UserId = domainEvent.UserId,
            EventType = "DeviceRegistrationPinCreated",
            EntityType = nameof(DeviceRegistrationPin),
            EntityId = domainEvent.PinId,
            AfterValue = JsonSerializer.Serialize(new { domainEvent.ExpiresAtUtc, domainEvent.MaxUses }),
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeviceRegistrationPinRevokedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<DeviceRegistrationPinRevokedDomainEvent>
{
    public async Task HandleAsync(DeviceRegistrationPinRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            UserId = domainEvent.UserId,
            EventType = "DeviceRegistrationPinRevoked",
            EntityType = nameof(DeviceRegistrationPin),
            EntityId = domainEvent.PinId,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeviceRegisteredAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<DeviceRegisteredDomainEvent>
{
    public async Task HandleAsync(DeviceRegisteredDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            DeviceId = domainEvent.DeviceId,
            EventType = "DeviceRegistered",
            EntityType = nameof(Device),
            EntityId = domainEvent.DeviceId,
            AfterValue = JsonSerializer.Serialize(new
            {
                DeviceType = domainEvent.DeviceTypeName,
                domainEvent.DeviceCredentialId,
                domainEvent.PinId,
            }),
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Only raised when the presented PIN matched a real row resolving to a single tenant — an
/// unknown PIN writes nothing (no tenant for the non-nullable TenantId); see
/// <see cref="DeviceRegistrationFailedDomainEvent"/>.
/// </summary>
public sealed class DeviceRegistrationFailedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<DeviceRegistrationFailedDomainEvent>
{
    public async Task HandleAsync(DeviceRegistrationFailedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            EventType = "DeviceRegistrationFailed",
            EntityType = nameof(DeviceRegistrationPin),
            EntityId = domainEvent.PinId,
            Reason = domainEvent.FailureReason,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeviceCredentialRotatedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<DeviceCredentialRotatedDomainEvent>
{
    public async Task HandleAsync(DeviceCredentialRotatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            DeviceId = domainEvent.DeviceId,
            UserId = domainEvent.UserId,
            EventType = "DeviceCredentialRotated",
            EntityType = nameof(DeviceCredential),
            EntityId = domainEvent.NewCredentialId,
            BeforeValue = JsonSerializer.Serialize(new { domainEvent.OldCredentialId }),
            AfterValue = JsonSerializer.Serialize(new { domainEvent.NewCredentialId }),
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeviceRevokedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<DeviceRevokedDomainEvent>
{
    public async Task HandleAsync(DeviceRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            DeviceId = domainEvent.DeviceId,
            UserId = domainEvent.UserId,
            EventType = "DeviceRevoked",
            EntityType = nameof(Device),
            EntityId = domainEvent.DeviceId,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PLAN-0003 Milestone F staff-member audit handlers. <c>EventType</c> for the lifecycle event is
/// <c>$"StaffMember{Action}"</c> (<c>"StaffMemberCreated"</c>, <c>"StaffMemberPinReset"</c>,
/// <c>"StaffMemberRoleAssigned"</c>), matching the Milestone D lifecycle-handler convention.
/// </summary>
public sealed class StaffMemberLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<StaffMemberLifecycleDomainEvent>
{
    public async Task HandleAsync(StaffMemberLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            StaffMemberId = domainEvent.StaffMemberId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(StaffMember)}{domainEvent.Action}",
            EntityType = nameof(StaffMember),
            EntityId = domainEvent.StaffMemberId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StaffPinLoginSucceededAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<StaffPinLoginSucceededDomainEvent>
{
    public async Task HandleAsync(StaffPinLoginSucceededDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            DeviceId = domainEvent.DeviceId,
            StaffMemberId = domainEvent.StaffMemberId,
            EventType = "StaffPinLoginSucceeded",
            EntityType = nameof(AuthSession),
            EntityId = domainEvent.AuthSessionId,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Raised for every failed staff PIN login attempt — including unknown staff codes, unlike the
/// unknown-email/unknown-PIN precedents, because the trusted device context supplies the tenant.
/// </summary>
public sealed class StaffPinLoginFailedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<StaffPinLoginFailedDomainEvent>
{
    public async Task HandleAsync(StaffPinLoginFailedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            DeviceId = domainEvent.DeviceId,
            StaffMemberId = domainEvent.StaffMemberId,
            EventType = "StaffPinLoginFailed",
            EntityType = nameof(StaffMember),
            EntityId = domainEvent.StaffMemberId,
            Reason = domainEvent.FailureReason,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StaffMemberDisabledAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<StaffMemberDisabledDomainEvent>
{
    public async Task HandleAsync(StaffMemberDisabledDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            StaffMemberId = domainEvent.StaffMemberId,
            UserId = domainEvent.UserId,
            EventType = "StaffMemberDisabled",
            EntityType = nameof(StaffMember),
            EntityId = domainEvent.StaffMemberId,
            AfterValue = JsonSerializer.Serialize(new { IsActive = false, domainEvent.SessionsRevoked }),
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PLAN-0004 Milestone C tax-configuration audit handlers (OI-0007's explicit audit requirement:
/// who, when, old config, new config). <c>EventType</c> follows the Milestone D
/// <c>$"{EntityType}{Action}"</c> convention (e.g. <c>"TaxDefinitionCreatedFromTemplate"</c>).
/// </summary>
public sealed class TaxDefinitionLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<TaxDefinitionLifecycleDomainEvent>
{
    public async Task HandleAsync(TaxDefinitionLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(TaxDefinition)}{domainEvent.Action}",
            EntityType = nameof(TaxDefinition),
            EntityId = domainEvent.TaxDefinitionId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TaxCategoryLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<TaxCategoryLifecycleDomainEvent>
{
    public async Task HandleAsync(TaxCategoryLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(TaxCategory)}{domainEvent.Action}",
            EntityType = nameof(TaxCategory),
            EntityId = domainEvent.TaxCategoryId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TaxCategoryDefinitionChangedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<TaxCategoryDefinitionChangedDomainEvent>
{
    public async Task HandleAsync(TaxCategoryDefinitionChangedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            LocationId = domainEvent.LocationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(TaxCategoryDefinition)}{domainEvent.Action}",
            EntityType = nameof(TaxCategoryDefinition),
            EntityId = domainEvent.TaxCategoryDefinitionId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PLAN-0004 Milestone D product-catalogue audit handlers (OI-0007's audit requirement: who, when,
/// old config, new config, whether a product was archived and replaced). The archive-and-replace
/// flow raises two <see cref="ProductLifecycleDomainEvent"/>s (one per affected row, <c>"Archived"</c>
/// and <c>"CreatedFromReplace"</c>) rather than a single combined event — each row gets its own
/// audit trail entry, matching this file's one-event-per-affected-entity convention throughout.
/// </summary>
public sealed class ProductCategoryLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<ProductCategoryLifecycleDomainEvent>
{
    public async Task HandleAsync(ProductCategoryLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(ProductCategory)}{domainEvent.Action}",
            EntityType = nameof(ProductCategory),
            EntityId = domainEvent.ProductCategoryId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ProductLifecycleAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<ProductLifecycleDomainEvent>
{
    public async Task HandleAsync(ProductLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = $"{nameof(Product)}{domainEvent.Action}",
            EntityType = nameof(Product),
            EntityId = domainEvent.ProductId,
            BeforeValue = domainEvent.BeforeValue,
            AfterValue = domainEvent.AfterValue,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
