# Contributing to AVEraser

Thank you for your interest in contributing to AVEraser.

The primary way to contribute is by submitting verified signatures for antivirus products that are not yet included in the database.

---

## Submitting Antivirus Signatures

### 1. Check for existing coverage

Before submitting, verify that the product is not already included in the project.

Relevant locations:
- `signatures.json`
- Internal definitions within the application (e.g. `KnownAVs`)

---

### 2. Collect signature data

Signature data must be collected in a controlled and reproducible environment.

A virtual machine with a clean Windows installation is strongly recommended.

The following tools may assist in identifying product-specific components:

| Tool | Purpose |
|------|--------|
| Process Explorer | Identify running processes |
| Autoruns | Identify services, drivers, and startup entries |
| RegShot | Detect registry changes during installation |
| File Explorer | Identify installation directories |

Only include components that are clearly and exclusively associated with the analyzed product.

---

### 3. Submit via Issue Template

Create a new issue using the provided **AV Signature Submission** template.

Ensure that:
- All fields are completed accurately
- The data is based on direct observation
- The submission is internally consistent

> [!WARNING]
> Submissions must not include system-critical or shared Windows components.
> 
> The following are strictly prohibited:
> - Windows system processes (e.g. `svchost.exe`, `explorer.exe`, `lsass.exe`, `csrss.exe`, `winlogon.exe`)
> - System directories (e.g. `C:\Windows`, `C:\Windows\System32`)
> - Generic or commonly used components

Submissions that violate these rules will not be considered.

---

### 4. Review process

All submissions undergo both automated validation and manual review.

During review:
- The data is verified in a controlled environment
- Exclusivity of signatures is confirmed
- Potential risks or false positives are evaluated

Approved submissions are added to `signatures.json` via pull request and become available in subsequent application updates.

---

## Quality Requirements

A valid submission should meet the following criteria:

- All processes and services are exclusive to the specified product
- Registry keys are introduced by the product installation
- File paths are located outside of system directories
- Data has been verified on a clean Windows installation

Submissions that do not meet these requirements may be rejected.

---

## Rejection Criteria

Submissions may be rejected for the following reasons:

- Inclusion of Windows system processes or services
- Use of system directories or non-specific paths
- Lack of verification or insufficient evidence
- Incomplete or inconsistent data

---

## Reporting Issues

For bugs or unexpected behavior, open a GitHub issue and include:

- A clear description of the problem
- Steps to reproduce the issue
- Your Windows version and environment details

Supporting material such as logs or screenshots is recommended.

---

## Code Contributions

Pull requests are welcome.

Please ensure that:
- Changes are limited in scope and clearly described
- Existing coding style is followed (C# 7.3, no unnecessary dependencies)
- The application is tested on supported Windows versions

---

## Maintainer

AVEraser is maintained by Bentendo.

Community contributions play a critical role in maintaining the accuracy and coverage of the signature database.