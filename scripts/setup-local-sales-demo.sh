#!/usr/bin/env bash
set -euo pipefail

# Daxa POS local sales demo setup helper (PLAN-0011 / PLAN-0006).
#
# Builds on setup-local-demo.sh and prepares the sales-ready catalogue/menu/tax/modifier data
# needed for manual PLAN-0006 PWA testing. This is local-development tooling only.

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

API_URL="${API_URL:-http://localhost:5118}"
PWA_URL="${PWA_URL:-http://localhost:8080}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@daxapos.local}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Local-Dev-Only-Passw0rd!}"
POSTGRES_DB="${POSTGRES_DB:-daxapos}"
POSTGRES_USER="${POSTGRES_USER:-daxapos}"

DEMO_TERMINAL_NAME="${DEMO_TERMINAL_NAME:-Front Counter 1}"
DEMO_PRODUCT_CATEGORY_NAME="${DEMO_PRODUCT_CATEGORY_NAME:-Coffee}"
DEMO_TAX_DEFINITION_CODE="${DEMO_TAX_DEFINITION_CODE:-AU_GST_10}"
DEMO_TAX_CATEGORY_CODE="${DEMO_TAX_CATEGORY_CODE:-AU_GST_TAXABLE}"
DEMO_PRODUCT_NAME="${DEMO_PRODUCT_NAME:-Flat White}"
DEMO_PRODUCT_PRICE="${DEMO_PRODUCT_PRICE:-5.50}"
DEMO_MODIFIER_GROUP_NAME="${DEMO_MODIFIER_GROUP_NAME:-Milk}"
DEMO_MODIFIER_NAME="${DEMO_MODIFIER_NAME:-Full cream}"
DEMO_MODIFIER_PRICE_DELTA="${DEMO_MODIFIER_PRICE_DELTA:-0}"
DEMO_MENU_NAME="${DEMO_MENU_NAME:-Local Sales Demo Menu}"
DEMO_MENU_SECTION_NAME="${DEMO_MENU_SECTION_NAME:-Coffee}"

for cmd in curl jq docker uuidgen; do
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

post_json() {
  local path="$1" data="$2" description="$3"
  http_request POST "$path" "Bearer ${ADMIN_TOKEN}" "$data"
  [[ "$HTTP_STATUS" == "201" || "$HTTP_STATUS" == "200" ]] || step_fail "$description"
}

psql_scalar() {
  local sql="$1"
  docker compose exec -T db psql \
    -v ON_ERROR_STOP=1 \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    -Atc "$sql"
}

new_uuid() {
  uuidgen | tr '[:upper:]' '[:lower:]'
}

echo "==> Running base local demo setup"
SETUP_OUTPUT="$("${SCRIPT_DIR}/setup-local-demo.sh")"
echo "$SETUP_OUTPUT"

LOC_ID=$(awk -F':  +' '/Location ID:/ {print $2; exit}' <<<"$SETUP_OUTPUT")
REG_PIN=$(awk -F':  +' '/Device registration PIN:/ {print $2; exit}' <<<"$SETUP_OUTPUT")
STAFF_CODE=$(awk -F':  +' '/Staff code:/ {print $2; exit}' <<<"$SETUP_OUTPUT")
STAFF_PIN=$(awk -F':  +' '/Staff PIN:/ {print $2; exit}' <<<"$SETUP_OUTPUT" | awk '{print $1}')

[[ -n "$LOC_ID" ]] || fail "Could not parse Location ID from setup-local-demo.sh output."
[[ -n "$REG_PIN" ]] || fail "Could not parse registration PIN from setup-local-demo.sh output."
[[ -n "$STAFF_CODE" ]] || fail "Could not parse staff code from setup-local-demo.sh output."
[[ -n "$STAFF_PIN" ]] || fail "Could not parse staff PIN from setup-local-demo.sh output."

echo "==> Logging in as ${ADMIN_EMAIL}"
LOGIN_BODY=$(jq -n --arg email "$ADMIN_EMAIL" --arg password "$ADMIN_PASSWORD" '{email:$email,password:$password}')
http_request POST /api/v1/auth/local/login "" "$LOGIN_BODY"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "Admin login"
ADMIN_TOKEN=$(jq -r '.sessionToken' <<<"$HTTP_BODY")
[[ -n "$ADMIN_TOKEN" && "$ADMIN_TOKEN" != "null" ]] || fail "Admin login response did not include a session token."

echo "==> Resolving organisation and tenant"
http_request GET /api/v1/auth/me "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "auth/me"
ORG_ID=$(jq -r '.organisationId' <<<"$HTTP_BODY")
TENANT_ID=$(jq -r '.tenantId' <<<"$HTTP_BODY")
[[ -n "$ORG_ID" && "$ORG_ID" != "null" ]] || fail "Could not resolve organisationId from /auth/me."
[[ -n "$TENANT_ID" && "$TENANT_ID" != "null" ]] || fail "Could not resolve tenantId from /auth/me."

echo "==> Resolving staff member '${STAFF_CODE}'"
http_request GET "/api/v1/staff-members?locationId=${LOC_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List staff members"
STAFF_ID=$(jq -r --arg code "$STAFF_CODE" '[.[] | select(.staffCode == $code)][0].id // empty' <<<"$HTTP_BODY")
[[ -n "$STAFF_ID" ]] || fail "Could not find staff member ${STAFF_CODE} at location ${LOC_ID}."

echo "==> Ensuring Staff role is assigned to demo staff"
psql_scalar "
INSERT INTO staff_member_roles (\"StaffMemberId\", \"RoleId\", \"TenantId\", \"LocationId\")
SELECT '${STAFF_ID}'::uuid, r.\"Id\", '${TENANT_ID}'::uuid, NULL
FROM roles r
WHERE r.\"Name\" = 'Staff'
  AND NOT EXISTS (
    SELECT 1 FROM staff_member_roles sr
    WHERE sr.\"StaffMemberId\" = '${STAFF_ID}'::uuid
      AND sr.\"RoleId\" = r.\"Id\"
  );
" >/dev/null

echo "==> Ensuring terminal '${DEMO_TERMINAL_NAME}' exists"
http_request GET /api/v1/terminals "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List terminals"
TERMINAL_ID=$(jq -r --arg name "$DEMO_TERMINAL_NAME" --arg loc "$LOC_ID" '[.[] | select(.name == $name and .locationId == $loc)][0].id // empty' <<<"$HTTP_BODY")
if [[ -n "$TERMINAL_ID" ]]; then
  echo "    Reusing terminal (${TERMINAL_ID})"
else
  TERMINAL_BODY=$(jq -n --arg name "$DEMO_TERMINAL_NAME" --arg locId "$LOC_ID" '{name:$name,locationId:$locId}')
  post_json /api/v1/terminals "$TERMINAL_BODY" "Create terminal"
  TERMINAL_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created terminal (${TERMINAL_ID})"
fi

echo "==> Ensuring venue tax configuration exists"
http_request GET /api/v1/venue-tax-configurations "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List venue tax configurations"
VENUE_TAX_ID=$(jq -r --arg loc "$LOC_ID" '[.[] | select(.locationId == $loc)][0].id // empty' <<<"$HTTP_BODY")
if [[ -n "$VENUE_TAX_ID" ]]; then
  echo "    Reusing venue tax configuration (${VENUE_TAX_ID})"
else
  VENUE_TAX_BODY=$(jq -n --arg locId "$LOC_ID" '{locationId:$locId,taxInclusivePricing:true,taxCalculationMode:0}')
  post_json /api/v1/venue-tax-configurations "$VENUE_TAX_BODY" "Create venue tax configuration"
  VENUE_TAX_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created venue tax configuration (${VENUE_TAX_ID})"
fi

echo "==> Ensuring AU GST tax definition and category linkage exists"
http_request GET /api/v1/tax-definitions "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List tax definitions"
TAX_DEF_ID=$(jq -r --arg code "$DEMO_TAX_DEFINITION_CODE" '[.[] | select(.code == $code)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$TAX_DEF_ID" ]]; then
  TAX_DEF_BODY=$(jq -n \
    --arg code "$DEMO_TAX_DEFINITION_CODE" \
    --arg orgId "$ORG_ID" \
    '{code:$code,name:"AU GST 10%",organisationId:$orgId,countryCode:"AU",regionCode:null,ratePercent:10,jurisdictionName:"Australia",jurisdictionType:0,includedInPrice:true,roundingMode:0,roundingPrecision:2,calculationScope:0,receiptMarkerCode:null,receiptMarkerLabel:null,reportingCategory:"GST"}')
  post_json /api/v1/tax-definitions "$TAX_DEF_BODY" "Create tax definition"
  TAX_DEF_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created tax definition (${TAX_DEF_ID})"
else
  echo "    Reusing tax definition (${TAX_DEF_ID})"
fi

http_request GET /api/v1/tax-categories "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List tax categories"
TAX_CAT_ID=$(jq -r --arg code "$DEMO_TAX_CATEGORY_CODE" '[.[] | select(.code == $code)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$TAX_CAT_ID" ]]; then
  TAX_CAT_BODY=$(jq -n --arg code "$DEMO_TAX_CATEGORY_CODE" --arg orgId "$ORG_ID" '{code:$code,name:"AU GST taxable",organisationId:$orgId,taxTreatment:0}')
  post_json /api/v1/tax-categories "$TAX_CAT_BODY" "Create tax category"
  TAX_CAT_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created tax category (${TAX_CAT_ID})"
else
  echo "    Reusing tax category (${TAX_CAT_ID})"
fi

http_request GET "/api/v1/tax-category-definitions?taxCategoryId=${TAX_CAT_ID}&locationId=${LOC_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List tax category definitions"
TAX_CAT_DEF_ID=$(jq -r --arg def "$TAX_DEF_ID" '[.[] | select(.taxDefinitionId == $def)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$TAX_CAT_DEF_ID" ]]; then
  TAX_CAT_DEF_BODY=$(jq -n --arg catId "$TAX_CAT_ID" --arg defId "$TAX_DEF_ID" --arg locId "$LOC_ID" '{taxCategoryId:$catId,taxDefinitionId:$defId,locationId:$locId,priority:0}')
  post_json /api/v1/tax-category-definitions "$TAX_CAT_DEF_BODY" "Create tax category definition"
  TAX_CAT_DEF_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created tax category definition (${TAX_CAT_DEF_ID})"
else
  echo "    Reusing tax category definition (${TAX_CAT_DEF_ID})"
fi

echo "==> Ensuring product catalogue and required modifier exist"
http_request GET /api/v1/product-categories "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List product categories"
PRODUCT_CATEGORY_ID=$(jq -r --arg name "$DEMO_PRODUCT_CATEGORY_NAME" '[.[] | select(.name == $name)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$PRODUCT_CATEGORY_ID" ]]; then
  PRODUCT_CATEGORY_BODY=$(jq -n --arg name "$DEMO_PRODUCT_CATEGORY_NAME" --arg orgId "$ORG_ID" '{name:$name,displayOrder:0,organisationId:$orgId}')
  post_json /api/v1/product-categories "$PRODUCT_CATEGORY_BODY" "Create product category"
  PRODUCT_CATEGORY_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created product category (${PRODUCT_CATEGORY_ID})"
else
  echo "    Reusing product category (${PRODUCT_CATEGORY_ID})"
fi

http_request GET /api/v1/products "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List products"
PRODUCT_ID=$(jq -r --arg name "$DEMO_PRODUCT_NAME" '[.[] | select(.name == $name)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$PRODUCT_ID" ]]; then
  PRODUCT_BODY=$(jq -n \
    --arg name "$DEMO_PRODUCT_NAME" \
    --arg orgId "$ORG_ID" \
    --arg productCategoryId "$PRODUCT_CATEGORY_ID" \
    --arg taxCategoryId "$TAX_CAT_ID" \
    --argjson price "$DEMO_PRODUCT_PRICE" \
    '{name:$name,organisationId:$orgId,productCategoryId:$productCategoryId,taxCategoryId:$taxCategoryId,description:"Local sales demo product",sku:null,barcode:null,basePrice:$price}')
  post_json /api/v1/products "$PRODUCT_BODY" "Create product"
  PRODUCT_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created product (${PRODUCT_ID})"
else
  echo "    Reusing product (${PRODUCT_ID})"
fi

http_request GET /api/v1/modifier-groups "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List modifier groups"
MODIFIER_GROUP_ID=$(jq -r --arg name "$DEMO_MODIFIER_GROUP_NAME" '[.[] | select(.name == $name)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$MODIFIER_GROUP_ID" ]]; then
  MODIFIER_GROUP_BODY=$(jq -n --arg name "$DEMO_MODIFIER_GROUP_NAME" --arg orgId "$ORG_ID" '{name:$name,organisationId:$orgId,selectionMin:1,selectionMax:1,isRequired:true}')
  post_json /api/v1/modifier-groups "$MODIFIER_GROUP_BODY" "Create modifier group"
  MODIFIER_GROUP_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created modifier group (${MODIFIER_GROUP_ID})"
else
  echo "    Reusing modifier group (${MODIFIER_GROUP_ID})"
fi

http_request GET "/api/v1/modifiers?modifierGroupId=${MODIFIER_GROUP_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List modifiers"
MODIFIER_ID=$(jq -r --arg name "$DEMO_MODIFIER_NAME" '[.[] | select(.name == $name)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$MODIFIER_ID" ]]; then
  MODIFIER_BODY=$(jq -n --arg name "$DEMO_MODIFIER_NAME" --arg groupId "$MODIFIER_GROUP_ID" --argjson priceDelta "$DEMO_MODIFIER_PRICE_DELTA" '{name:$name,modifierGroupId:$groupId,priceDelta:$priceDelta}')
  post_json /api/v1/modifiers "$MODIFIER_BODY" "Create modifier"
  MODIFIER_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created modifier (${MODIFIER_ID})"
else
  echo "    Reusing modifier (${MODIFIER_ID})"
fi

echo "==> Ensuring product is linked to required modifier group"
PMG_ID=$(new_uuid)
psql_scalar "
INSERT INTO product_modifier_groups (\"Id\", \"TenantId\", \"ProductId\", \"ModifierGroupId\", \"DisplayOrder\", \"CreatedAtUtc\")
SELECT '${PMG_ID}'::uuid, '${TENANT_ID}'::uuid, '${PRODUCT_ID}'::uuid, '${MODIFIER_GROUP_ID}'::uuid, 0, now()
WHERE NOT EXISTS (
  SELECT 1 FROM product_modifier_groups
  WHERE \"ProductId\" = '${PRODUCT_ID}'::uuid
    AND \"ModifierGroupId\" = '${MODIFIER_GROUP_ID}'::uuid
);
" >/dev/null

echo "==> Ensuring menu, section, and menu item exist"
http_request GET "/api/v1/menus?locationId=${LOC_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List menus"
MENU_ID=$(jq -r --arg name "$DEMO_MENU_NAME" --arg loc "$LOC_ID" '[.[] | select(.name == $name and .locationId == $loc)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$MENU_ID" ]]; then
  MENU_BODY=$(jq -n --arg name "$DEMO_MENU_NAME" --arg orgId "$ORG_ID" --arg locId "$LOC_ID" '{name:$name,organisationId:$orgId,locationId:$locId}')
  post_json /api/v1/menus "$MENU_BODY" "Create menu"
  MENU_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created menu (${MENU_ID})"
else
  echo "    Reusing menu (${MENU_ID})"
fi

http_request GET "/api/v1/menu-sections?menuId=${MENU_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "List menu sections"
MENU_SECTION_ID=$(jq -r --arg name "$DEMO_MENU_SECTION_NAME" '[.[] | select(.name == $name)][0].id // empty' <<<"$HTTP_BODY")
if [[ -z "$MENU_SECTION_ID" ]]; then
  MENU_SECTION_BODY=$(jq -n --arg name "$DEMO_MENU_SECTION_NAME" --arg menuId "$MENU_ID" '{name:$name,menuId:$menuId,displayOrder:0}')
  post_json /api/v1/menu-sections "$MENU_SECTION_BODY" "Create menu section"
  MENU_SECTION_ID=$(jq -r '.id' <<<"$HTTP_BODY")
  echo "    Created menu section (${MENU_SECTION_ID})"
else
  echo "    Reusing menu section (${MENU_SECTION_ID})"
fi

MENU_ITEM_ID=$(new_uuid)
psql_scalar "
INSERT INTO menu_section_items (\"Id\", \"TenantId\", \"MenuSectionId\", \"ProductId\", \"DisplayOrder\", \"CreatedAtUtc\")
SELECT '${MENU_ITEM_ID}'::uuid, '${TENANT_ID}'::uuid, '${MENU_SECTION_ID}'::uuid, '${PRODUCT_ID}'::uuid, 0, now()
WHERE NOT EXISTS (
  SELECT 1 FROM menu_section_items
  WHERE \"MenuSectionId\" = '${MENU_SECTION_ID}'::uuid
    AND \"ProductId\" = '${PRODUCT_ID}'::uuid
);
" >/dev/null

echo "==> Verifying resolved menu contains the demo product and required modifier"
http_request GET "/api/v1/menus/resolved?locationId=${LOC_ID}" "Bearer ${ADMIN_TOKEN}"
[[ "$HTTP_STATUS" == "200" ]] || step_fail "Resolve menu"
RESOLVED_COUNT=$(jq -r --arg productId "$PRODUCT_ID" --arg groupId "$MODIFIER_GROUP_ID" --arg modifierId "$MODIFIER_ID" '
  [ .sections[].items[]
    | select(.productId == $productId)
    | select(any(.modifierGroups[]?; .id == $groupId and .isRequired == true and .selectionMin == 1 and .selectionMax == 1 and any(.modifiers[]?; .id == $modifierId)))
  ] | length
' <<<"$HTTP_BODY")
[[ "$RESOLVED_COUNT" != "0" ]] || fail "Resolved menu did not include the demo product with its required modifier."

echo ""
echo "======================================================================"
echo " Daxa POS local sales demo environment is ready"
echo "======================================================================"
echo " PWA URL:                  ${PWA_URL}"
echo " Back Office URL:          ${PWA_URL}/back-office"
echo " API URL:                  ${API_URL}"
echo ""
echo " Location ID:              ${LOC_ID}"
echo " Terminal ID:              ${TERMINAL_ID}"
echo " Product:                  ${DEMO_PRODUCT_NAME} (${PRODUCT_ID})"
echo " Required modifier:        ${DEMO_MODIFIER_GROUP_NAME} -> ${DEMO_MODIFIER_NAME}"
echo ""
echo " Device registration PIN:  ${REG_PIN}"
echo " Staff code:               ${STAFF_CODE}"
echo " Staff PIN:                ${STAFF_PIN}"
echo "======================================================================"
echo ""
echo "Manual UI steps:"
echo "  1. Open ${PWA_URL}/device-setup and register this browser with PIN ${REG_PIN}."
echo "  2. Open ${PWA_URL}/back-office/login and sign in as ${ADMIN_EMAIL}."
echo "  3. Go to Back Office Terminals and assign the registered device to terminal '${DEMO_TERMINAL_NAME}'."
echo "  4. Open ${PWA_URL}/login and sign in with staff code ${STAFF_CODE} and PIN ${STAFF_PIN}."
echo "  5. Open ${PWA_URL}/sales; the '${DEMO_PRODUCT_NAME}' tile should be visible."
echo "  6. Select the required '${DEMO_MODIFIER_GROUP_NAME}' modifier and add the item to the order."
echo "  7. Use Pay to record Cash or Manual EFTPOS."
echo "  8. Open ${PWA_URL}/display to view the customer display surface."
echo ""
