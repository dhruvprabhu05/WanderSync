# 0007. Identity: Entra External ID behind issuer-agnostic JWT validation

- Status: Accepted
- Date: 2026-10-19

## Context

WanderSync needs consumer sign-up and sign-in for the Android app and bearer-token
authentication for the API. The intended provider is Microsoft Entra External ID
(the CIAM successor to Azure AD B2C), which keeps the project on Azure end to end.

Creating the external tenant under the Azure for Students subscription failed.
The portal's Create button was disabled for the university-managed account. A
Bicep deployment of `Microsoft.AzureActiveDirectory/ciamDirectories` was then
rejected with `RequestDisallowedByAzure` by the subscription's region policy (see
ADR 0003).

We considered moving off Azure entirely, to Firebase or Vercel. We rejected that
because hosting, CI/CD, OIDC deployment and Container Apps already work. The
block affects one resource type, not the platform.

## Decision

1. The API validates standard OIDC JWTs and names no provider in code.
   `AddJwtBearer()` binds `Authority` and `ValidAudiences` from the
   `Authentication:Schemes:Bearer` configuration section. Changing provider is a
   configuration change.
2. The development identity tenant is an Entra External ID 30-day trial tenant.
   A trial tenant needs no Azure subscription, so the student subscription's
   region policy does not apply to it. It was created from the university
   account. A personal account is added as a second Global Administrator, so
   access survives that account being deactivated after graduation.
3. Every endpoint is deny-by-default. A fallback authorization policy requires
   an authenticated caller with a `sub` claim, and anonymous endpoints (health
   probes, ping) opt out explicitly with `AllowAnonymous()`.
4. The app fails at startup if `Authority` or `ValidAudiences` is missing,
   rather than booting, passing its health probes and then rejecting every
   call.

## Consequences

- The trial tenant expires after 30 days unless it is linked to a subscription.
  If linking hits the same policy, we move to another OIDC provider (Firebase
  Auth, Auth0 or Clerk) by changing configuration. The API code and its test
  suite do not change.
- Tests never contact a real identity provider. `ApiFactory` swaps OIDC
  metadata discovery for a per-run signing key. Issuer, audience, lifetime and
  signature validation still run unmodified. The suite asserts the rejection
  paths: no token, forged signature, expired token, wrong audience, wrong issuer
  and missing subject.
- Anonymous callers get 401 for unknown routes, not 404, so route existence is
  not exposed to unauthenticated clients.
