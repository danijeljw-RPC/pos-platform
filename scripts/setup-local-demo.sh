#!/usr/bin/env bash
set -euo pipefail

# Daxa POS local demo setup helper (PLAN-0011).
#
# Prepares the minimum API data needed to exercise the PWA against the root Docker Compose
# stack: a demo location, a demo staff member (staff code + PIN), and a fresh device
# registration PIN. Safe to rerun — it reuses the named location/staff member instead of
# creating duplicates. See:
#   docs/superpowers/specs/2026-07-07-local-demo-setup-helper-design.md
#   docs/plans/active/PLAN-0011-local-demo-setup-helper.md
#
# This is local-development tooling only. It never writes to PostgreSQL directly and never
# prints the admin password or admin session token.

API_URL="${API_URL:-http://localhost:5118}"
PWA_URL="${PWA_URL:-http://localhost:8080}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@daxapos.local}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Local-Dev-Only-Passw0rd!}"
DEMO_LOCATION_NAME="${DEMO_LOCATION_NAME:-Local Demo Venue}"
DEMO_STAFF_CODE="${DEMO_STAFF_CODE:-TEST01}"
DEMO_STAFF_NAME="${DEMO_STAFF_NAME:-Test Cashier}"
DEMO_STAFF_PIN="${DEMO_STAFF_PIN:-246810}"

for cmd in curl jq; do
  command -v "$cmd" >/dev/null 2>&1 || {
    echo "ERROR: required command '$cmd' is not installed or not on PATH." >&2
    exit 1
  }
done

fail() {
  echo "" >&2
  echo "ERROR: $1" >&2
  exit 1
}

# Performs an HTTP request against $API_URL. Sets HTTP_STATUS and HTTP_BODY. Never logs the
# request body, so credentials passed as request data are never echoed.
http_request() {
  local method="$1" path="$2" auth_header="$3" data="${4:-}"
  local -a curl_args=(-sS -X "$method" "${API_URL}${path}" -H 'Content-Type: application/json')
  [[ -n "$auth_header" ]] && curl_args+=(-H "Authorization: $auth_header")
  [[ -n "$data" ]] && curl_args+=(-d "$data")

  local raw
  raw=$(curl "${curl_args[@]}" -w $'\n%{http_code}') ||
    fail "Request to ${method} ${path} failed — is the API reachable at ${API_URL}?"

  HTTP_BODY="${raw%$'\n'*}"
  HTTP_STATUS="${raw##*$'\n'}"
}

step_fail() {
  echo "" >&2
  echo "ERROR: $1 failed (HTTP ${HTTP_STATUS})." >&2
  echo "Response body: ${HTTP_BODY}" >&2
  exit 1
}

echo "==> Checking API health at ${API_URL}/health"
http_request GET /health ""
[[ "$HTTP_STATUS" == "200" ]] || step_fail "Health check"

echo "==> Logging in as ${ADMIN_EMAIL}"
LOGIN_BODY=$(jq -n --arg email "$ADMIN_EMAIL" --arg password "$ADMIN_PASSWORD" '{email:$email,password:$password}')
http_request POST /api/v1/auth/local/login "" "$LOGIN_BODY"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "Admin login"
ADMIN_TOKEN=$(jq -r '.sessionToken' <<<"$HTTP_BODY")
[[ -n "$ADMIN_TOKEN" && "$ADMIN_TOKEN" != "null" ]] || fail "Admin login response did not include a session token."

echo "==> Resolving bootstrap organisation"
http_request GET /api/v1/auth/me "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "auth/me"
ORG_ID=$(jq -r '.organisationId' <<<"$HTTP_BODY")
[[ -n "$ORG_ID" && "$ORG_ID" != "null" ]] || fail "Could not resolve organisationId from /auth/me."

echo "==> Resolving demo location '${DEMO_LOCATION_NAME}'"
http_request GET /api/v1/locations "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List locations"
LOC_ID=$(jq -r --arg name "$DEMO_LOCATION_NAME" '[.[] | select(.name == $name)][0].id // empty' <<<"$HTTP_BODY")

if [[ -n "$LOC_ID" ]]; then
  echo "    Reusing existing active location (${LOC_ID})"
else
  echo "    Creating location '${DEMO_LOCATION_NAME}'"
  LOCATION_BODY=$(jq -n --arg name "$DEMO_LOCATION_NAME" --arg orgId "$ORG_ID" '{name:$name,organisationId:$orgId}')
  http_request POST /api/v1/locations "Bearer ${ADMIN_TOKEN}" "$LOCATION_BODY"
  [[ "$HTTP_STATUS" == "201" ]] || step_fail "Create location"
  LOC_ID=$(jq -r '.id' <<<"$HTTP_BODY")
fi
# Note: GET /api/v1/locations only lists active locations, and location creation has no
# name-uniqueness check. If a location named "${DEMO_LOCATION_NAME}" exists but is inactive,
# it is invisible to this lookup and this helper cannot detect it — it will create a new active
# location with the same name rather than erroring. This is a known limitation of the current
# location API surface (see PLAN-0011 worker notes), not a case this script silently reactivates.

NORMALISED_STAFF_CODE=$(printf '%s' "$DEMO_STAFF_CODE" | tr '[:lower:]' '[:upper:]')

echo "==> Resolving demo staff member '${NORMALISED_STAFF_CODE}'"
http_request GET "/api/v1/staff-members?locationId=${LOC_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List staff members"
STAFF_ID=$(jq -r --arg code "$NORMALISED_STAFF_CODE" '[.[] | select(.staffCode == $code)][0].id // empty' <<<"$HTTP_BODY")

if [[ -n "$STAFF_ID" ]]; then
  echo "    Reusing existing active staff member (${STAFF_ID}); resetting PIN"
  http_request POST "/api/v1/staff-members/${STAFF_ID}/reset-pin" "Bearer ${ADMIN_TOKEN}"
  [[ "$HTTP_STATUS" == "200" ]] || step_fail "Reset staff PIN"
  CURRENT_STAFF_PIN=$(jq -r '.pin' <<<"$HTTP_BODY")
  PIN_NOTE=" (reset just now by this run — any previous PIN no longer works)"
else
  echo "    Creating staff member '${DEMO_STAFF_NAME}' (${NORMALISED_STAFF_CODE})"
  STAFF_BODY=$(jq -n \
    --arg name "$DEMO_STAFF_NAME" \
    --arg code "$DEMO_STAFF_CODE" \
    --arg pin "$DEMO_STAFF_PIN" \
    --arg locId "$LOC_ID" \
    '{displayName:$name,staffCode:$code,pin:$pin,locationId:$locId}')
  http_request POST /api/v1/staff-members "Bearer ${ADMIN_TOKEN}" "$STAFF_BODY"
  if [[ "$HTTP_STATUS" == "201" ]]; then
    STAFF_ID=$(jq -r '.id' <<<"$HTTP_BODY")
    CURRENT_STAFF_PIN="$DEMO_STAFF_PIN"
    PIN_NOTE=""
  elif [[ "$HTTP_STATUS" == "409" ]]; then
    # The active-staff listing above found nothing with this code, yet creation reports a
    # conflict — the only way both are true is a staff member with this code already exists in
    # the organisation but is inactive (List hides inactive staff; the Create conflict check does
    # not). This helper will not reactivate it automatically.
    fail "A staff member with code '${NORMALISED_STAFF_CODE}' already exists in this organisation but is not active, so it was not returned by the active-staff listing. This helper does not reactivate staff records. Choose a different DEMO_STAFF_CODE, or resolve the existing record through the API before rerunning."
  else
    step_fail "Create staff member"
  fi
fi

echo "==> Issuing a fresh device registration PIN"
REG_PIN_BODY=$(jq -n --arg locId "$LOC_ID" '{locationId:$locId}')
http_request POST /api/v1/device-registration-pins "Bearer ${ADMIN_TOKEN}" "$REG_PIN_BODY"
[[ "$HTTP_STATUS" == "201" ]] || step_fail "Create device registration PIN"
REG_PIN=$(jq -r '.pin' <<<"$HTTP_BODY")
REG_PIN_EXPIRY=$(jq -r '.expiresAtUtc' <<<"$HTTP_BODY")

echo ""
echo "======================================================================"
echo " Daxa POS local demo environment is ready"
echo "======================================================================"
echo " PWA URL:                  ${PWA_URL}"
echo " Location ID:              ${LOC_ID}"
echo ""
echo " Device registration PIN:  ${REG_PIN}"
echo " Registration PIN expires: ${REG_PIN_EXPIRY}"
echo ""
echo " Staff code:               ${NORMALISED_STAFF_CODE}"
echo " Staff PIN:                ${CURRENT_STAFF_PIN}${PIN_NOTE}"
echo "======================================================================"
echo ""
echo "Next steps:"
echo "  1. Open ${PWA_URL}/device-setup and enter the registration PIN above."
echo "  2. Then log in at ${PWA_URL}/login with the staff code and PIN above."
echo ""
