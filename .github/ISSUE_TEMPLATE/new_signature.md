---
name: "AV Signature Submission"
about: "Submit new antivirus product signatures for inclusion in the database"
title: "AV Signature Submission: [Product Name]"
labels: ["signature-submission", "needs-review"]
assignees: ["BentendoYT"]
---

## Submission Guidelines

All submissions are subject to manual review before being included in the database.  
Incomplete, unverifiable, or low-quality submissions may be rejected without further notice.

---

## Product Information

**Product Name**  
Provide the full official name of the antivirus product (e.g. "Zemana AntiMalware").

**Vendor / Publisher**  
Name of the company or organization responsible for the product.

**Version Tested**  
Specify the exact version used for analysis (e.g. 3.2.1).

**Official Website / Download URL**  
Provide a link to the official product page or a trusted download source.

---

## Signature Data

Submissions must include only identifiers that are exclusive to the specified antivirus product.

> [!WARNING]
> Submissions must not include any system-critical or shared components.
> 
> The following are strictly prohibited and will result in immediate rejection:
> 
> - Windows system processes (e.g. `svchost.exe`, `explorer.exe`, `csrss.exe`, `lsass.exe`, `winlogon.exe`)
> - Shared system services
> - Generic or commonly used application components

---

### Processes  
Executable names without file extension:

ExampleProcess1
ExampleProcess2


### Services  
Exact service names as listed in the Windows Service Manager:

ExampleService1
ExampleService2


### Registry Keys  
Paths under HKLM (omit the `HKLM\` prefix):

SOFTWARE\VendorName\ProductName
SOFTWARE\WOW6432Node\VendorName\ProductName


### File System Locations  
Full absolute paths:

C:\Program Files\VendorName\ProductName
C:\ProgramData\VendorName


---

## Verification Checklist

Please confirm all of the following before submitting:

- [ ] The product was installed and analyzed in a controlled environment (e.g. virtual machine)
- [ ] All listed processes and services are exclusive to this product
- [ ] No system-critical or shared Windows components are included
- [ ] Registry keys and file paths are not present on a clean Windows installation
- [ ] The submission is accurate and based on direct observation

---

## Methodology

Briefly describe how the data was collected (e.g. process monitoring tools, system snapshots, before/after comparison).
