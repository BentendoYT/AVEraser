# <img width="1440" height="256" alt="AVEraser" src="https://github.com/user-attachments/assets/bf8a308d-d211-4480-9733-f38e7f7a0f3a" />


AVEraser is an advanced antivirus residue removal tool for Windows, designed to detect and eliminate leftover components from previously installed security software.

Even after official uninstallers have been executed, many antivirus products leave behind files, services, registry entries, and background components. AVEraser ensures a clean system state, making it suitable for reinstallations, system optimization, or switching between security solutions.

---

## Key Features

- Comprehensive detection of antivirus remnants across multiple system layers
- Removal of leftover:
  - Processes
  - Services
  - Registry entries
  - File system artifacts
- Detection and optional removal of bundled components (e.g. browser add-ons, VPN clients)
- Multi-stage deletion strategy for maximum reliability:
  - Standard .NET deletion
  - Command-line removal (`rd /s /q`)
  - PowerShell fallback
  - Secure deletion methods
- Boot-time deletion for locked or protected files
- Ability to disable self-protection mechanisms before cleanup
- Detailed cleanup reporting with clear status indicators
- Streamlined user interface designed for clarity and efficiency
- Integrated update system with version tracking

---

## How It Works

AVEraser uses a curated signature database to identify components that are uniquely associated with specific antivirus products.

Each entry is:
- Collected through controlled testing environments
- Verified before inclusion
- Maintained via a structured review process

The tool compares your system against this database and safely removes matching artifacts.

---

## Supported Products

AVEraser currently supports residue detection for a wide range of antivirus solutions, including but not limited to:

- Avast
- AVG
- Avira
- Bitdefender
- Kaspersky
- Norton / Symantec
- Malwarebytes
- McAfee / Trellix
- ESET
- Trend Micro
- Sophos
- and others

The list is continuously expanded through verified community contributions.

---

## Installation

1. Download the latest version from the [Releases](../../releases) page  
2. Run the installer (`AVEraserSetup_x64.exe`)  
3. Follow the on-screen instructions  

---

## Usage

1. Run AVEraser with administrator privileges  
2. Start a system scan  
3. Review detected residues  
4. Select the components to remove  
5. Execute cleanup  
6. Reboot if prompted to complete removal of locked files  

---

## Signature Database

The detection engine is powered by a structured and review-based signature database.

- Community submissions are accepted via GitHub Issues
- All submissions are automatically validated
- Entries are manually reviewed before being merged
- System-critical components are strictly excluded

See `signatures.json` for details.

---

## System Requirements

- Windows 10 or newer  
- .NET Framework 4.7.2 or higher  
- Administrator privileges  

---

## Disclaimer

AVEraser performs low-level system modifications, including service removal and registry changes.

While all signatures are reviewed, use of this software is at your own risk. It is strongly recommended to create a system backup or restore point before performing cleanup operations.

---

## License

This project is licensed under the MIT License.  
See the [LICENSE](LICENSE) file for more information.

---

## Maintainer

Bentendo © 2026
