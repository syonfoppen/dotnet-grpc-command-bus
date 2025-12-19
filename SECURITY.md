# Security Policy – dotnet-grpc-command-bus

## Supported Versions

The dotnet-grpc-command-bus project is currently in an early, experimental stage.

At this time:
- The project is considered **alpha** (`v0.x`)
- The primary focus is on learning, design exploration, and architectural discussion
- APIs and behavior may change without notice

While no formal security guarantees are provided at this stage, responsible security reporting
is already supported to prepare for future reference and production-grade implementations.

---

## Reporting a Vulnerability

If you discover a potential security issue, please follow these steps:

**Do NOT open a public GitHub Issue for the vulnerability.**

Instead, report it privately using one of the following methods:

- **GitHub Security Advisories** (preferred)
- **Email:** syon@syonfoppen.nl

Private reporting helps prevent potential misuse before the issue is properly assessed
and, if necessary, addressed.

---

## Encryption for Secure Communication

For sensitive vulnerability reports, you may encrypt your communication using the PGP key below:

- **PGP Fingerprint:**  
  `5982 EF3B 32E7 672D D299 CAB3 45EA A161 4436 9252`

- **Public Key**:
  [Download PGP key](https://keys.openpgp.org/vks/v1/by-fingerprint/5982EF3B32E7672DD299CAB345EAA16144369252)    

Using encryption is recommended when sharing detailed technical information or proof-of-concept material.

---

## What to Include in Your Report

To help assess and reproduce the issue efficiently, please include:

- A clear description of the vulnerability
- Steps to reproduce the issue or proof-of-concept code (if available)
- The potential impact and affected components
- Suggested mitigation or remediation steps (if known)

Incomplete reports may take longer to triage.

---

## Response Process

Once a vulnerability report is received:

1. An acknowledgement will be sent within **7 days**
2. The issue will be reviewed and its severity assessed
3. If action is required, a fix or mitigation approach will be determined
4. You will be kept informed of progress where appropriate

Response timelines may vary depending on the complexity and scope of the issue.

---

## Disclosure Policy

This project follows a **coordinated disclosure** approach:

1. Vulnerabilities are reported privately
2. A fix or mitigation is prepared when applicable
3. A security advisory may be published after resolution
4. Credit is given to the reporter unless anonymity is requested

Public disclosure before coordination may result in the report being deprioritized.

---

## Scope

### Current scope (v0.x)

The following are considered in-scope for security-related discussion:

- Command contracts and serialization formats
- gRPC interfaces and transport usage
- Idempotency and dispatch semantics
- Design decisions that could lead to security issues when used incorrectly

### Out of scope (for now)

- Third-party dependencies
- Hosting infrastructure
- User-specific deployments
- Production hardening and operational security

Future versions may expand the scope as the project matures.

---

## Responsible Disclosure Commitment

We are committed to:

- Engaging respectfully with security researchers
- Addressing legitimate security concerns in good faith
- Avoiding public exposure of vulnerabilities before mitigation
- Giving credit where appropriate

As this is a learning-focused hobby project, responses may not always be immediate,
but all serious reports will be reviewed.

---
