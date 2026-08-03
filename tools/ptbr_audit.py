#!/usr/bin/env python3
"""Audit user-facing UI literals that still look English.

Implementation identifiers (registry names, package IDs, PowerShell source and pure
interpolation fragments) are excluded. Renderable UI must be Portuguese or explicitly
covered by a pt-BR catalog.
"""

from __future__ import annotations

import argparse
import html
import re
import sys
from pathlib import Path

# GitHub's Windows runner may expose a cp1252 console. Never let a status glyph crash
# the audit before its report can be uploaded.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

REPO = Path(__file__).resolve().parents[1]
ROOT = REPO / "SysManager" / "SysManager"
REPORT = REPO / "artifacts" / "ptbr-audit.txt"
CATALOGS = sorted((ROOT / "Services").glob("PtBr*Catalog*.cs"))

UI_ATTR = re.compile(r'\b(?:Text|Content|Header|ToolTip|AutomationProperties\.Name|Title)="([^"]+)"')
CS_STRING = re.compile(r'"((?:[^"\\]|\\.)*)"')
CATALOG_KEY = re.compile(r'^\s*\["((?:[^"\\]|\\.)*)"\]\s*=')
INTERPOLATION = re.compile(r"\{[^{}]+\}")

ENGLISH_HINTS = {
    "the", "and", "for", "with", "from", "your", "this", "that", "system",
    "scan", "cleanup", "update", "updates", "enable", "disable", "enabled",
    "disabled", "administrator", "select", "choose", "search", "files", "file",
    "process", "processes", "service", "services", "settings", "health",
    "performance", "network", "drive", "memory", "temperature", "available",
    "installed", "open", "save", "remove", "restore", "profile", "mode",
    "status", "details", "warning", "error", "loading", "running", "check",
    "quick", "privacy", "security", "battery", "about", "advanced",
    "monitor", "history", "repair", "manager", "recommended", "requires",
    "application", "applications", "current", "failed", "successful", "start",
    "stop", "refresh", "create", "delete", "export", "import", "configuration",
    "cancel", "cannot", "completed", "ready", "folder", "selected", "list",
}

TECHNICAL_ALLOW = {
    "CPU", "GPU", "BIOS", "UEFI", "RAM", "VRAM", "SSD", "HDD", "NVMe",
    "SMART", "NVIDIA", "AMD", "Intel", "Windows", "Windows Update", "DNS",
    "Hosts", "Ping", "Traceroute", "CLI", "PowerShell", "Winget", "WinGet",
    "Hyper-V", "WSL", "Docker", "Edge", "OneDrive", "Defender", "XMP", "EXPO",
    "Resizable BAR", "SAM", "FPS", "MHz", "GHz", "GB", "MB", "KB", "ms",
    "IPv4", "IPv6", "TCP", "UDP", "HTTP", "HTTPS", "USB", "SATA", "SCSI",
    "CSV", "XML", "SFC", "DISM", "PATH", "TEMP", "WebView2",
}

INTERNAL_TOKENS = (
    "Microsoft.Windows.", "Windows.SystemToast.", "SubscribedContent-",
    "Get-NetAdapter", "Get-ComputerRestorePoint", "Checkpoint-Computer",
    "Write-Warning", "Get-CimInstance", "Set-DnsClientServerAddress",
    "Software\\", "SYSTEM\\", "CurrentControlSet\\", "Policies\\",
)

CS_UI_MARKERS = (
    "Title", "Message", "Description", "Detail", "Status", "Label", "Tooltip",
    "Toast", "Dialog", "Content", "DisplayName", "Summary", "Recommendation",
)


def decode_csharp_string(value: str) -> str:
    return (value.replace("\\n", " ")
                 .replace("\\r", " ")
                 .replace("\\t", " ")
                 .replace('\\"', '"')
                 .strip())


def load_catalog_keys() -> set[str]:
    keys: set[str] = set()
    for catalog in CATALOGS:
        for line in catalog.read_text(encoding="utf-8", errors="ignore").splitlines():
            match = CATALOG_KEY.match(line)
            if match:
                keys.add(decode_csharp_string(match.group(1)).replace('\\\\', '\\').casefold())
    return keys


TRANSLATED = load_catalog_keys()


def skip(text: str) -> bool:
    text = text.strip()
    if not text or text in TECHNICAL_ALLOW:
        return True
    if text.casefold() in TRANSLATED:
        return True
    if text.startswith(("{Binding", "{DynamicResource", "{StaticResource", "{x:", "&#x")):
        return True
    if text.startswith(("http://", "https://", "pack://", "HKCU", "HKLM", "HKEY_")):
        return True
    if any(token in text for token in INTERNAL_TOKENS):
        return True
    if ".Replace(" in text or "$($_." in text:
        return True
    if re.fullmatch(r"[\d\W_]+", text):
        return True
    if re.fullmatch(r"[A-Z0-9_.:/\\%-]{2,}", text) and " " not in text:
        return True

    without_values = INTERPOLATION.sub("", text)
    if not without_values or not re.search(r"[A-Za-zÀ-ÿ]", without_values):
        return True

    # Scanner fragments caused by nested C# expressions are not complete UI literals.
    if "{(" in text or text.startswith((")}", "}}")) or text.endswith((" ?", "?", "{(")):
        return True
    return False


def looks_english(text: str) -> bool:
    if skip(text):
        return False
    words = re.findall(r"[A-Za-z]+", text.lower())
    return any(word in ENGLISH_HINTS for word in words)


def scan_xaml(path: Path) -> list[tuple[int, str]]:
    findings: list[tuple[int, str]] = []
    for lineno, line in enumerate(path.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
        if line.lstrip().startswith("<!--"):
            continue
        for match in UI_ATTR.finditer(line):
            text = html.unescape(match.group(1)).strip()
            if looks_english(text):
                findings.append((lineno, text))
    return findings


def scan_cs(path: Path) -> list[tuple[int, str]]:
    findings: list[tuple[int, str]] = []
    if path.name.startswith("PtBr"):
        return findings
    for lineno, line in enumerate(path.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
        stripped = line.strip()
        if not stripped or stripped.startswith("//") or "Log." in line:
            continue
        if "ManagementObjectSearcher" in line or "SELECT " in line or "Registry" in line:
            continue
        if not any(marker in line for marker in CS_UI_MARKERS):
            continue
        for match in CS_STRING.finditer(line):
            text = decode_csharp_string(match.group(1))
            if looks_english(text):
                findings.append((lineno, text))
    return findings


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict", action="store_true", help="exit non-zero when untranslated UI text is found")
    args = parser.parse_args()

    all_findings: list[tuple[Path, int, str]] = []
    for path in sorted(ROOT.rglob("*.xaml")):
        all_findings.extend((path, line, text) for line, text in scan_xaml(path))
    for path in sorted(ROOT.rglob("*.cs")):
        all_findings.extend((path, line, text) for line, text in scan_cs(path))

    REPORT.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "Optimize pt-BR audit",
        "====================",
        f"Possíveis textos visíveis ainda em inglês: {len(all_findings)}",
        f"Textos cobertos pelos catálogos: {len(TRANSLATED)}",
        "",
    ]
    for path, lineno, text in all_findings:
        relative = path.relative_to(REPO)
        lines.append(f"{relative}:{lineno}: {text}")
    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")

    for line in lines[:4]:
        print(line)
    print(f"Relatório: {REPORT}")
    if all_findings:
        for line in lines[5:25]:
            print(line)
        if len(all_findings) > 20:
            print(f"... e mais {len(all_findings) - 20} ocorrência(s).")

    return 1 if args.strict and all_findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
