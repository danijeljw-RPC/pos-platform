# Claude Code Ingestion Prompt

Use this prompt at the start of planning.

```text
You are planning a new configurable POS platform.

Read the markdown files in this planning pack.

The goal is to design a new POS system that supports hospitality, retail, food trucks, bakeries, pubs, restaurants, fast food, electronics stores, repair/service businesses, and multi-location chains.

Do not design a narrow restaurant POS.

Use the provided notes to produce:
1. A high-level architecture.
2. Domain model candidates.
3. Bounded contexts/modules.
4. MVP scope.
5. Phase roadmap.
6. Open questions.
7. ADR candidates.
8. Initial backlog epics.
9. Risks and assumptions.
10. Suggested .NET solution structure.

Key principles:
- Windows POS terminal uses .NET MAUI.
- Windows customer display uses a second MAUI window.
- Linux/Android/iPad/kiosk devices use PWA.
- KDS is a separate device/session.
- Payments use a provider adapter architecture.
- AU/NZ tax support must handle mixed GST and GST-free baskets.
- Global tax support must use tax lines, not one tax field.
- Receipts must preserve product names and use markers like F = GST-free.
- Audit logging is mandatory.
- Offline/local resilience should be designed from the start.
```
