# ADR-0010 — Financial Records, Ledger, and Audit

## Status

Accepted

## Context

Daxa POS processes payments, refunds, voids, discounts, and surcharges. Financial records must be accurate, tamper-evident, and reconstructable for tax compliance, dispute resolution, and regulatory requirements.

Silent editing of financial records (e.g. changing an order total after payment) is a common source of fraud and compliance failures in POS systems.

## Decision

Daxa POS treats financially meaningful records as **append-only or reversal-based**.

- Orders, payments, and refunds must not be silently edited after they are created.
- Corrections use explicit records: **void**, **refund**, **reversal**, or **adjustment**.
- Payment, refund, gift card, and store credit activity is ledgered — every movement has a signed record.
- All financially significant operations are written to the audit log with: who, what, when, terminal, location, before value, after value, reason, and linked entity IDs.
- Receipt reprints are audited.
- Refund receipts link to the original order and original payment.
- Discount overrides and manual price changes are audited with reason capture.

**Prohibited patterns:**

- `UPDATE orders SET total = X` (without a correction record).
- Silent deletion of order lines after payment.
- Payment amount changes without a reversal + new payment.

## Consequences

**Positive:**

- Full audit trail for every financial transaction.
- Reconstructable order and payment history.
- Compliant with AU/NZ tax record-keeping requirements.
- Reduces fraud risk.
- Clear basis for dispute resolution.

**Negative:**

- More complex than simple CRUD.
- Storage grows as reversal/adjustment records accumulate.
- UI must expose void/refund flows rather than simple edit.

## Alternatives Considered

1. **Simple CRUD with history log** — Rejected. History log can be deleted or modified; append-only ledger is more robust.
2. **Full event sourcing** — Valuable model; a hybrid approach (ledger + audit events) is preferred for pragmatism without sacrificing auditability.

## Open Questions

- How long should financial records be retained?
- Should tax invoices be immutable PDFs stored in object storage?
- What is the required retention period under AU/NZ tax law?

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0007 — Local/Hybrid Sync Principles](ADR-0007-local-hybrid-sync-principles.md)
- [Module: Audit](../../modules/audit.md)
- [Module: Payments](../../modules/payments.md)
- [Module: Refunds](../../modules/refunds.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)

---

## Acceptance Addendum

ADR-0010 is accepted.

Financial records, ledger entries, audit records, tax invoices, receipts, refunds, voids, and related reporting data must be treated as immutable business records once created.

The system must preserve historical accuracy by preventing edits to the source components of completed financial records. Where a record needs to be corrected, reversed, voided, or refunded, the system must create an additional financial event rather than rewriting the original record.

This ADR is accepted with a configurable retention model and generated-document strategy.

## Resolution of Open Questions

### How long should financial records be retained?

Financial record retention is controlled by system configuration.

The default retention period is:

```text
FinancialRecordRetentionDays = 2555
```

This is equivalent to seven years using a day-based configuration value.

The value must be stored as a number of days, not years or months. This avoids ambiguity around leap years, calendar month length, and jurisdiction-specific retention wording.

The retention period may be increased by changing the configured value.

The system must not hard-code retention periods specifically for AU, NZ, CA, US, or any other jurisdiction. Jurisdictional compliance is handled by configuration, deployment policy, and customer/operator responsibility.

A scheduled cleanup task must run daily and identify records older than the configured retention period.

The cleanup task must only delete records that are eligible for deletion under the active configuration and system policy.

The cleanup process must be auditable. At minimum, it should record:

- when the cleanup task ran;
- which client/location scope was processed;
- the configured retention period at the time of cleanup;
- how many records were eligible;
- how many records were deleted;
- whether any records were skipped;
- whether the task succeeded or failed.

### Should tax invoices be immutable PDFs stored in object storage?

Tax invoices and receipt PDFs are immutable once generated.

However, the system should not require every invoice or receipt PDF to be permanently stored as a separate object by default.

The preferred model is:

1. The source financial record is immutable.
2. The invoice/receipt rendering input is derived from that immutable source record.
3. The PDF generation service renders the document from the immutable source data.
4. The generated PDF output is immutable for that generated version.
5. The system may regenerate the same PDF later from the same immutable source data and template version.

This avoids unnecessary storage of large numbers of PDF files while still preserving document integrity.

The source data must contain enough information to regenerate the invoice or receipt accurately, including:

- invoice or receipt number;
- client and location identity;
- business name and tax identifiers;
- order/payment line data;
- tax line data;
- discounts, surcharges, refunds, and void markers;
- payment method summaries;
- issue timestamp;
- applicable currency;
- document template/version metadata where required.

Once the source financial record is created, its financial components must not be edited.

Only limited status-style fields may change, such as:

- void status;
- refund status;
- refund references;
- reversal references;
- audit metadata;
- sync/export status.

Those status changes must be represented as additional events or linked records where possible, not destructive edits to the original financial source data.

The PDF generation interface/service will be provided to the system and must generate documents from the immutable source data.

Object storage may still be used where needed, including:

- customer-requested PDF archival;
- legal or compliance-specific deployments;
- exported document bundles;
- offline sync packages;
- email delivery copies;
- signed or hash-sealed PDFs;
- cases where exact binary reproduction is required.

If a generated PDF is stored, it must be treated as immutable. A corrected invoice, refund note, or replacement document must be created as a new document rather than modifying the stored PDF.

### What is the required retention period under AU/NZ tax law?

The system does not hard-code retention periods based on AU, NZ, CA, US, or other jurisdictional tax law.

Instead, retention is set by configuration.

The default configuration is seven years expressed in days:

```text
FinancialRecordRetentionDays = 2555
```

This default can be extended where required by customer policy, jurisdictional requirements, franchise requirements, enterprise policy, or operator preference.

The product should provide sensible defaults, but legal compliance remains a configuration and operating policy concern rather than hard-coded application behaviour.

## Accepted Design Direction

### Immutable Financial Source Records

The system must preserve financial source records as immutable once created.

Examples include:

- completed orders;
- payment records;
- ledger entries;
- tax lines;
- invoice source records;
- receipt source records;
- refund records;
- void records;
- audit events.

Financial corrections must be additive.

The system must not rewrite historical totals, tax amounts, payment amounts, line values, or invoice components after the record has been created.

### Ledger and Audit Behaviour

The ledger and audit model must support forward-only financial history.

Examples:

- A refund creates a refund transaction linked to the original payment/order.
- A void marks the original record as voided and records the void event.
- A correction creates a new correcting event or replacement record.
- A tax change affects future records only and does not recalculate historical tax.

Audit records must capture who performed the action, when it occurred, what changed, and the business context where applicable.

### PDF Generation Strategy

PDFs are generated from immutable source records through a document generation interface/service.

The source record is the authoritative financial record.

The generated PDF is an output representation of that source record.

The system should avoid storing hundreds or thousands of generated PDFs by default where those PDFs can be reliably regenerated from immutable source data.

Where exact generated output must be preserved, the generated PDF may be stored in object storage with appropriate metadata and immutability rules.

### Retention Cleanup

A daily cleanup task must evaluate records against the configured retention value.

The cleanup process must respect:

- client scope;
- location scope;
- deployment mode;
- configured retention days;
- legal hold or manual retention flags, if implemented;
- export/sync status, if relevant.

The cleanup process must not delete records that are still required for active operational, audit, sync, refund, chargeback, or reporting workflows.

## Configuration

The minimum configuration value is:

```text
FinancialRecordRetentionDays = 2555
```

Recommended related configuration values:

```text
FinancialRecordCleanupEnabled = true
FinancialRecordCleanupSchedule = Daily
FinancialRecordCleanupTimeLocal = 03:00
PreserveGeneratedPdfByDefault = false
AllowPdfRegenerationFromSource = true
StoreGeneratedPdfWhenDeliveredExternally = false
```

Exact names may change during implementation, but the behaviour must remain configuration-driven.

## Consequences of the Accepted Addendum

This decision keeps the financial record model consistent across jurisdictions and deployment modes.

It avoids hard-coding AU/NZ-specific legal retention logic into the application.

It keeps historical financial records stable and auditable.

It avoids unnecessary long-term PDF storage by making immutable source records the authority and allowing PDFs to be regenerated from those records.

It allows customers or deployments to extend retention periods through configuration without code changes.

It also keeps the system open for future requirements such as object storage, signed PDFs, legal hold, archival exports, or jurisdiction-specific deployment profiles.

## Status Update

Status: **Accepted**
