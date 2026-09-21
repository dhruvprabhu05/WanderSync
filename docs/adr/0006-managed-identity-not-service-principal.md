# 6. CI deploys with a user-assigned managed identity

Date: 2026-09-15

## Status

Accepted

## Context

GitHub Actions needs to authenticate to Azure to deploy. Three options:

1. A service principal with a client secret, stored as a GitHub secret.
2. A service principal with OIDC federated credentials — no stored secret.
3. A user-assigned managed identity with OIDC federated credentials.

Option 1 is rejected outright: a long-lived credential in CI is the same class
of mistake as the Firebase admin key that this project was created in response
to (see ADR 1).

Options 2 and 3 are equivalent from GitHub's side. The deciding factor is that
this subscription lives in the `gatech.edu` tenant, where the account is an
ordinary user. Creating a service principal requires directory permissions that
institutional tenants normally withhold. Being Owner on a *subscription* grants
nothing in the *directory*.

A user-assigned managed identity is an Azure resource, not a directory object.
Subscription RBAC is sufficient to create one, and it supports federated
identity credentials identically.

## Decision

Deploy with a user-assigned managed identity, `id-wandersync-deploy`, holding
federated credentials scoped to:

- `repo:dhruvprabhu05@179248906/WanderSync@1370388608:environment:dev`
- `repo:dhruvprabhu05@179248906/WanderSync@1370388608:ref:refs/heads/main`
- `repo:dhruvprabhu05@179248906/WanderSync@1370388608:pull_request`

These use GitHub's ID-pinned subject format, which embeds the numeric owner and
repository IDs. The first attempt registered name-only subjects
(`repo:dhruvprabhu05/wandersync:...`) and login failed with `AADSTS700213`:
GitHub presented the ID form, and the match is exact and case-sensitive. The
ID form is also the safer one -- if the repository were renamed or deleted and
the name re-registered by someone else, a name-based subject would trust their
workflows; an ID-pinned subject cannot be claimed that way.

A job that declares `environment: dev` presents the `environment:` subject, not
the `ref:` one, which is why the environment credential exists separately.

granted **Contributor on the resource group**, not the subscription.

## Consequences

- No credential exists to leak, rotate, or expire.
- The trust is pinned to one repository and specific refs; the client ID is not
  sensitive because possessing it grants nothing.
- Least privilege by scope: a compromised workflow cannot reach beyond
  `rg-wandersync-dev`.
- Bicep therefore deploys at resource-group scope. The resource group itself is
  created once by a bootstrap command outside the pipeline.
- A second resource group (`prod`) will need its own role assignment.
