# Payment Provider Roadmap

## Target markets

Rollout direction:

```text
AU/NZ
↓
Singapore + Hong Kong
↓
UK
↓
US/Canada
↓
APAC + NA + EMEA broader rollout
```

## Provider roadmap by market

| Phase | Markets | Primary providers to support |
|---:|---|---|
| 1 | Australia | Tyro, Zeller, Square, Stripe Terminal, Windcave |
| 2 | New Zealand | Stripe Terminal, Windcave, Adyen, Worldline/ANZ-style partners |
| 3 | Singapore | Stripe Terminal, Adyen, Windcave, Worldline, Global Payments |
| 4 | Hong Kong | Adyen, Windcave, Worldline, Global Payments |
| 5 | UK | Stripe Terminal, Square, Adyen, Worldline, Global Payments |
| 6 | US/Canada | Stripe Terminal, Square, Adyen, Global Payments |

## Strategic provider set

| Provider | AU | NZ | SG | HK | UK | US | CA | Role |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Stripe Terminal | Yes | Yes | Yes | Validate | Yes | Yes | Yes | Best global SaaS-style baseline |
| Adyen | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Best enterprise/global provider |
| Windcave | Yes | Yes | Yes/global | Yes/global | Yes/global | Yes/global | Yes/global | Strong AU/NZ + global gateway/terminal option |
| Square | Yes | No | No | No | Yes | Yes | Yes | Strong SMB provider where available |
| Tyro | Yes | No | No | No | No | No | No | Best AU-specific EFTPOS |
| Zeller | Yes | No | No | No | No | No | No | AU-specific modern EFTPOS |
| Worldline | Partner-dependent | Partner-dependent | Yes/global | Yes/global | Yes | Yes | Available via partners | Global terminal/acquiring option |
| Global Payments | Yes/partner | Yes/partner | Yes | Yes | Yes | Yes | Yes | Broad global acquiring/ISV option |

## AU/NZ launch recommendation

```text
1. Manual EFTPOS
2. Tyro
3. Zeller
4. Square
5. Stripe Terminal
6. Windcave
```

## APAC expansion

Singapore:

```text
1. Stripe Terminal
2. Adyen
3. Windcave
4. Worldline
5. Global Payments
```

Hong Kong:

```text
1. Adyen
2. Windcave
3. Worldline
4. Global Payments
```

## UK

```text
1. Stripe Terminal
2. Square
3. Adyen
4. Worldline
5. Global Payments
```

## US/Canada

```text
1. Stripe Terminal
2. Square
3. Adyen
4. Global Payments
```

## Provider selection screen concept

```text
Settings
  Payments
    Add payment provider

    [ Tyro ]
    [ Zeller ]
    [ Square ]
    [ Stripe Terminal ]
    [ Windcave ]
    [ Adyen ]
    [ Worldline ]
    [ Global Payments ]
```

After selecting provider:

```text
Provider: Square
Status: Not connected

[Connect Square account]
[Select location]
[Select terminal]
[Test payment]
[Set as default]
```
