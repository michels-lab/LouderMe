# Michel's Lab Project Governance

This repository is part of the **Michel's Lab** software ecosystem.

## Authority

- Shared engineering rules, reusable implementation patterns, branding conventions, cloud/security standards and cross-app status live in `realmichelduarte/Michel-Software-Standards`.
- App-specific implementation truth remains in this repository and its own audit/project logs.
- The machine-readable relationship is declared in `.michelslab/project.yml`.

## Reporting contract

This repository is monitored by the master standards repository. Changes that affect only this product stay here. A change must also be promoted to the master repository when it creates or changes a reusable rule or cross-app concern, including:

- releases/versioning policy;
- shared architecture;
- Google/Supabase/other external services;
- cloud synchronization;
- authentication/authorization;
- secret requirements;
- security/privacy;
- update/distribution patterns;
- shared Michel's Lab branding/About behavior;
- reusable CI/release practices;
- cross-app gaps or decisions.

## Secret boundary

Only secret **names and storage locations** may be documented. Passwords, tokens, signing passwords, private keys, database passwords, refresh tokens and privileged service credentials must never be committed.

## Workflow

**Project records implementation → master polls project status → reusable findings are promoted to standards → future projects consume those standards.**
