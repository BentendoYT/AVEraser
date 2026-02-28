# <img width="1514" height="278" alt="AVEraser" src="https://github.com/user-attachments/assets/01b67eae-038b-427d-9e7f-89e72fe5c9d3" />

AVEraser is a powerful antivirus residue cleaner for Windows built with .NET Framework. It detects and removes leftover files, registry keys, services, and processes from 21 known antivirus products — even after their official uninstallers have run — ensuring a completely clean system ready for a fresh install.

## Features

- Scans for residues of 21 major antivirus products including Avast, AVG, Bitdefender, Kaspersky, Norton, and more.
- Detects leftover processes, services, registry keys, and folders.
- Detects and removes bundled apps (e.g. Avast Secure Browser, McAfee WebAdvisor).
- Multi-tier folder deletion: .NET → `rd /s /q` → PowerShell → SDelete.
- Boot-time deletion scheduling for locked files that can't be removed while Windows is running.
- Kernel driver disabling to break antivirus self-protection before cleanup.
- Animated progress tracking with per-product status updates.
- Detailed cleanup report with color-coded results (succeeded / failed / skipped).
- One-click reboot option when boot-time deletions are pending.
- Auto-update system with changelog display and in-app installer.
- Clean borderless UI with smooth Windows 11 rounded corners.

## Installation

1. Download the latest release from the [Releases](../../releases) page.
2. Run the installer (`AVEraserSetup.exe`) and follow the instructions.

## Usage

1. Open **AVEraser** as Administrator.
2. Click **Scan** to search the system for antivirus residues.
3. Review the results — each detected product shows how many processes, services, registry keys, and folders were found.
4. Select the products you want to clean up (or use **Select All**).
5. Click **Delete Selected**.
6. If bundled apps are detected (e.g. browser or VPN), you will be asked whether to remove those as well.
7. Review the cleanup report. If any files were locked, click **Reboot Now** to complete the deletion on next boot.

## Supported Antivirus Products

| Product | Product | Product |
|---|---|---|
| Avast Antivirus | AVG Antivirus | Avira |
| Bitdefender | Kaspersky | Malwarebytes |
| McAfee / Trellix | Norton / Symantec | ESET NOD32 |
| Trend Micro | Sophos | F-Secure |
| G Data | Comodo | Panda Security |
| Webroot | Vipre | TotalAV |
| Cylance / BlackBerry | CrowdStrike Falcon | SentinelOne |

## System Requirements

- Windows 10 or higher
- .NET Framework 4.7.2 or higher
- Administrator privileges (required for service, registry, and file deletion)

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Author

Bentendo © 2026
