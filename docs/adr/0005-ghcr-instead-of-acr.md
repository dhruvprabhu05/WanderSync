# 5. Container images live in GitHub Container Registry, not ACR

Date: 2026-09-15

## Status

Accepted

## Context

Azure Container Registry is the default choice for images consumed by Azure
Container Apps, and it integrates cleanly with managed identity for
authentication.

It also costs about $5/month on the Basic tier — roughly $60 over twelve
months, against a total credit of $100. The registry would consume more of the
budget than everything else in this architecture combined.

GitHub Container Registry (`ghcr.io`) is free for public images, images are
already being built by GitHub Actions in the same repository, and Container
Apps pulls public images with no registry credentials at all.

## Decision

Publish API images to `ghcr.io/dhruvprabhu05/wandersync-api`, tagged with the
commit SHA. No ACR in the architecture.

## Consequences

- Roughly $60/year saved — over half the available credit.
- Images are public. Acceptable: the image contains no secrets, and the source
  is public anyway. A private repository would need a pull secret in the
  Container App configuration.
- Loses ACR-specific features not needed here: geo-replication, Defender
  vulnerability scanning, managed-identity pull, ACR Tasks.
- Migrating to ACR later is a registry URL change plus a `registries[]` block.
