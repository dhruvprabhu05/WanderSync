# 4. Key Vault purge protection is disabled

Date: 2026-09-15

## Status

Accepted

## Context

Azure Key Vault offers soft delete (mandatory) and purge protection (optional).
Purge protection prevents a deleted vault from being permanently removed before
its retention window expires — and, critically, **cannot be turned off once
enabled**.

For a production system holding real secrets, purge protection is correct and
some compliance regimes require it.

This is a portfolio project on a $100 annual credit, where the environment is
expected to be destroyed and redeployed repeatedly — both to prove the Bicep
actually works from nothing, and to stop paying for idle resources. With purge
protection on, the vault name stays reserved for the full retention period and
redeployment fails.

## Decision

Leave purge protection disabled in `dev`. Keep soft delete at the 7-day minimum.

## Consequences

- `az group delete` followed by a fresh deployment works, which keeps the
  infrastructure code honest.
- An accidental vault deletion is recoverable for 7 days but permanently
  purgeable within that window.
- A `prod` environment should enable it. The parameter file is the right place
  for that difference, not the module.
