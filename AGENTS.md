# AGENTS.md

## Repo shape
- This is a single Windows Forms app, not a web/service repo. Main project: `SelfishNetV3\SelfishNet.csproj`; solution: `SelfishNetv3.sln`.
- The solution also contains `SelfishNetV3Setup\SelfishNetV3Setup.vdproj`; treat it as Visual Studio installer tooling, not the normal `dotnet build` target.
- `Program.Main()` starts `ArpForm`, not the `main` form.

## Commands
- Focused build: `dotnet build "SelfishNetV3\SelfishNet.csproj" -c Release -p:Platform=x86 --nologo`
- Solution build: `dotnet build "SelfishNetv3.sln" -c Release -p:Platform=x86 --nologo`
- Publish both configured x86 outputs: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Build-Publish.ps1`
- Publish outputs are `SelfishNetV3\publish\win-x86-framework-dependent` and `SelfishNetV3\publish\win-x86-self-contained`.
- No test, lint, formatter, typecheck, CI, or pre-commit config was found; use a focused `dotnet build` as the default verification unless you add those tools.

## Runtime/build quirks
- Target framework is `net10.0-windows7.0` with `UseWindowsForms=true` and `OutputType=WinExe`.
- Several configurations allow unsafe code; do not remove unsafe/PInvoke code as cleanup unless you are replacing the native interop path deliberately.
- `SelfishNetV3\app.manifest` requests `requireAdministrator`; packet capture/runtime testing must run elevated.
- Npcap 1.87+ is required with WinPcap API-compatible Mode enabled. `PcapNetCompat.cs` resolves `wpcap.dll` from Npcap install paths and checks registry/service version.
- README also calls out compatible Wi-Fi/monitor-mode hardware for network control features.

## Licensing
- Repository license is GNU General Public License v3.0; keep `README.md` and root `LICENSE` references consistent when changing docs or packaging.
- Do not add dependencies, assets, or copied code with terms incompatible with GPL-3.0 without calling out the licensing impact.

## Architecture notes
- `ArpForm` owns app startup flow, tray icon, Npcap check, adapter selection, `PcList`, and the `CArp` controller.
- `CAdapter` selects an active network interface with a gateway before ARP work starts.
- `CArp` handles ARP discovery, redirect/spoofing, packet accounting, and background threads around the local `CPcapNet` API.
- `PcapNetCompat.cs` is the in-repo PInvoke compatibility layer for the legacy PcapNet-style calls; changes here affect native packet capture/loading.

## Editing cautions
- WinForms forms have paired `.Designer.cs` and `.resx` files. If editing UI manually, keep control names, event hookups, and resources consistent.
- `SelfishNet.csproj` references a few `bin\...` icon files as content; do not assume every `bin` path is disposable without checking project references.
- Existing source includes legacy/decompiled-style patterns and Spanish comments; avoid broad style rewrites while fixing behavior.
