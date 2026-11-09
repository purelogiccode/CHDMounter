# Security Policy

## Supported versions

CHDMounter is maintained on the latest release line. Security fixes are shipped in new releases.

| Version | Supported |
|---------|-----------|
| 1.4.x   | Yes       |
| < 1.4   | No        |

## Reporting a vulnerability

**Please do not open a public issue for security vulnerabilities.**

Report vulnerabilities privately through GitHub's private vulnerability reporting:

<https://github.com/purelogiccode/CHDMounter/security/advisories/new>

If you cannot use that form, open a minimal issue asking for a private channel and do **not**
include exploit details, proof-of-concept code, affected files or personal data in the public
issue.

Please include:

- A description of the vulnerability and its impact.
- The affected version, platform and mount core (Dokan, WinFsp or FUSE).
- Steps to reproduce, or a minimal proof of concept if one is safe to share.
- Any suggested fix or mitigation.
- Whether you would like to be credited in the advisory.

## Response process

- We aim to acknowledge reports within **5 business days**.
- We will investigate, confirm the issue and agree on a disclosure timeline with the reporter.
- Fixes are developed privately and released as a new version. We will credit reporters in the
  advisory unless they prefer to remain anonymous.
- Please give us a reasonable opportunity to fix the issue before public disclosure.

This project does not currently operate a bug bounty program.

## Scope

Security issues in the following areas are in scope:

- CHD parsing, sector reading and filesystem reconstruction in CHDMounter.
- Path handling and file-system operations in the Dokan, WinFsp and FUSE mount backends.
- The virtual CUE/BIN/ISO/WAV export layer.
- Settings storage and encryption, including DPAPI handling on Windows.
- The update checker and bug-report client.
- Release packaging, GitHub Actions workflows and repository configuration.

Out of scope:

- Vulnerabilities in third-party dependencies, drivers or services (for example CHDSharp,
  VideoGameFileSystemParser, Dokan, WinFsp, libfuse or macFUSE). Please report those to the
  upstream project. If a dependency issue also affects CHDMounter, tell us so we can update the
  dependency.
- Issues that require physical access to the user's machine or an already-compromised host.
- Social engineering, spam or denial-of-service reports that do not demonstrate a concrete
  security impact.
- Unsupported versions.

## Security measures

This repository uses:

- **CodeQL** code scanning for C# on pushes, pull requests and a weekly schedule.
- **Dependabot alerts** and **Dependabot security updates** for NuGet and GitHub Actions.
- **Dependency review** on pull requests to block new high-severity vulnerable dependencies.
- **Secret scanning** with push protection.
- **Private vulnerability reporting** through GitHub Security Advisories.

## Safe harbor

We support good-faith security research. If you make a reasonable effort to avoid privacy
violations, service disruption and data loss while investigating and reporting an issue, we will
not pursue legal action or report you to law enforcement for that research. This safe harbor does
not cover activities that are illegal, extortive or harmful to users.
