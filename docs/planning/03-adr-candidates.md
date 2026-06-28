# ADR Candidates

## ADR — Use .NET MAUI for Windows POS terminals

Decision:

```text
Use .NET MAUI for Windows POS terminals and customer-facing second display.
```

Rationale:

```text
Native Windows app feel
Full-screen/borderless support
Second window/customer display support
Better hardware integration path
Suitable for counter POS terminals
```

## ADR — Use PWA for non-Windows devices

Decision:

```text
Use PWA for Linux, Android, iPad, kiosks, and KDS.
```

Rationale:

```text
Linux MAUI is not a reliable commercial baseline
PWA works well on tablets and kiosk browsers
Easy updates
Consistent UI delivery
```

## ADR — Use payment provider adapter architecture

Decision:

```text
All payment providers must implement a common payment terminal interface.
```

Rationale:

```text
Supports AU/NZ first
Supports global expansion
Avoids hard-coding one provider
Allows venue-specific provider selection
```

## ADR — Use tax-line based calculation

Decision:

```text
Tax is calculated per order line and represented as one or more tax lines.
```

Rationale:

```text
Supports AU/NZ GST
Supports GST-free mixed baskets
Supports US stacked taxes
Supports VAT/GST/global expansion
```

## ADR — Store tax snapshots on order lines

Decision:

```text
Store calculated tax data on order lines at sale time.
```

Rationale:

```text
Historical accuracy
Reporting accuracy
Product tax settings can change later
```

## ADR — Customer display as second MAUI window

Decision:

```text
Use a separate MAUI window for customer-facing display on Windows POS devices.
```

Rationale:

```text
Cleaner than stretching one app across monitors
Supports different layouts
Better customer experience
```

## ADR — KDS as separate device/session

Decision:

```text
KDS screens are separate from the POS terminal and should run independently.
```

Rationale:

```text
Kitchen/bar prep should not depend on the counter POS machine
Easier deployment
Supports multiple prep stations
```
