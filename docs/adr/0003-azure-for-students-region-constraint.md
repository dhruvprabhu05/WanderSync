# 3. Region is constrained by the Azure for Students offer

Date: 2026-09-15

## Status

Accepted

## Context

The subscription backing this project is an Azure for Students offer
(`MS-AZR-0170P`): $100 of credit over 12 months, no credit card, and the
subscription is disabled rather than billed when the credit runs out.

That offer enforces an undocumented, per-subscription region allow-list at the
ARM layer. It is not visible as an Azure Policy assignment and the portal's
region picker does not reflect it — `eastus` appears selectable and is not.

Resource groups are exempt, which makes them a misleading probe: creating
`rg-wandersync-dev` in `eastus` succeeded, and creating a managed identity in
the same region then failed with `RequestDisallowedByAzure`.

Probing with throwaway managed identities across fifteen candidate regions gave
the real answer:

| Allowed | Blocked |
| --- | --- |
| `eastus2`, `canadacentral` | `eastus`, `centralus`, `westus2`, `westus3`, `uksouth`, `westeurope`, `northeurope`, `swedencentral`, `polandcentral`, `germanywestcentral`, `spaincentral`, `uaenorth`, `brazilsouth` |

## Decision

Deploy everything to **`eastus2`**. Constrain the `location` parameter in
`infra/main.bicep` with `@allowed(['eastus2', 'canadacentral'])` so an invalid
region fails at compile time rather than halfway through a deployment.

## Consequences

- Both permitted regions are first-tier and support every service this
  architecture needs, so nothing was given up.
- The allow-list is per-subscription and can change. The probe script is
  recorded here so it can be re-run rather than rediscovered.
- A subscription upgrade would lift the restriction; the `@allowed` decorator
  is then the only thing to widen.
