# PLAN-0011 Worker Notes — Local Demo Setup Helper

## Current Status

Design approved. Planning documents created. Implementation has not started.

## Human Decisions

- Create a runnable script, not only a command reference.
- Make the script safe to rerun against an existing local database.
- Keep browser device registration manual.

## Repository Findings

- The root `compose.yaml` runs the complete local stack.
- The PWA has no default staff PIN or demo cashier.
- Bootstrap admin credentials configure admin API access only.
- Location and staff list endpoints allow API-level reuse.
- Existing staff PINs cannot be recovered; the supported reset endpoint returns a new raw PIN.
- Device registration PINs are single-use by default and expire after 15 minutes.

## Assumptions

- Local developers have `bash`, `curl`, and `jq`.
- The script's default credentials match `.env.example`, but environment overrides remain
  available.

## Next Step

After human review of the design document, write the detailed implementation plan and implement
the helper.
