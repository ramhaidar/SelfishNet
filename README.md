# SelfishNetV3

SelfishNetV3 is a Windows desktop tool for discovering devices on a local network and controlling their connectivity by using ARP redirection/spoofing. The current codebase is a .NET Windows Forms application with an optional Visual Studio installer project.

> Use this only on networks and devices you own or are authorized to administer. ARP spoofing and traffic blocking can disrupt other users' network access.

## Current status

This project is still in development. The app builds from source, but packet capture and ARP control require a supported Windows/Npcap runtime and must be tested while running elevated.

## Features

- Select an active network adapter with a gateway before ARP operations start.
- Discover devices connected to the local network.
- Display each discovered device's host name, IP address, and MAC address.
- Show live upload/download packet-rate estimates in KB/s.
- Configure per-device download and upload caps.
- Block/unblock selected devices.
- Start and stop ARP redirecting/spoofing from the toolbar.
- Minimize to the Windows tray and restore from the tray menu.
- Check for Npcap/WinPcap compatibility before starting packet-capture work.

## Requirements

- Windows.
- .NET SDK 10.x to build from source.
- Npcap 1.87 or newer from <https://npcap.com/>.
  - Enable **WinPcap API-compatible Mode** during Npcap installation.
  - Legacy WinPcap is not supported; the app uses Npcap's `wpcap.dll` compatibility API.
- Administrator privileges. The app manifest requests `requireAdministrator`.
- A network adapter with an active gateway.
- Compatible network hardware/drivers for the packet-capture and ARP-control behavior you intend to use.
- `Computerfont.ttf` from `SelfishNetV3/Resources` for the intended UI font when using installed builds.

## Build from source

From the repository root:

```powershell
dotnet build "SelfishNetV3\SelfishNet.csproj" -c Release -p:Platform=x86 --nologo
```

The solution can also be built with:

```powershell
dotnet build "SelfishNetv3.sln" -c Release -p:Platform=x86 --nologo
```

The solution includes `SelfishNetV3Setup/SelfishNetV3Setup.vdproj`, which is a Visual Studio installer project. Treat it as installer tooling rather than the normal `dotnet build` target.

## Publish

The repository includes a PowerShell publish script:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Build-Publish.ps1
```

Configured publish outputs are:

- `SelfishNetV3/publish/win-x86-framework-dependent`
- `SelfishNetV3/publish/win-x86-self-contained`

## Run/use basics

1. Install Npcap 1.87 or newer with **WinPcap API-compatible Mode** enabled.
2. Run SelfishNetV3 as administrator.
3. Select a network adapter that is up and has a gateway.
4. Use **Network Discovery** to scan for hosts.
5. Use the grid to review host name, IP, MAC, upload/download activity, caps, and block/spoof state.
6. Use the toolbar to start or stop redirecting/spoofing.

Installed builds should also follow `SelfishNetV3/Resources/Installation Instructions.txt`, including installing `Computerfont.ttf` if the UI font is missing.

## Project layout

- `SelfishNetv3.sln` - Visual Studio solution.
- `SelfishNetV3/SelfishNet.csproj` - main Windows Forms project.
- `SelfishNetV3/Program.cs` - app entry point; starts `ArpForm`.
- `SelfishNetV3/ArpForm.cs` - main ARP/network-control UI and startup flow.
- `SelfishNetV3/CAdapter.cs` - network adapter selection and gateway checks.
- `SelfishNetV3/CArp.cs` - ARP discovery, spoofing/redirection, packet accounting, and background worker threads.
- `SelfishNetV3/PcapNetCompat.cs` - in-repo compatibility layer for Npcap's WinPcap-style `wpcap.dll` API.
- `SelfishNetV3/PC.cs` and `SelfishNetV3/PcList.cs` - discovered host model and list management.
- `SelfishNetV3/Resources/` - icons, images, font, and install instructions.
- `SelfishNetV3Setup/` - Visual Studio installer project.

## Troubleshooting

- **Npcap not detected:** install Npcap 1.87 or newer and enable WinPcap API-compatible Mode.
- **No adapter appears:** verify the adapter is up and has a non-`0.0.0.0` gateway.
- **Packet capture/redirection does not work:** run as administrator and confirm Npcap is installed for the same process architecture you are building/running.
- **UI font looks wrong:** install `SelfishNetV3/Resources/Computerfont.ttf`.
- **Build fails because the SDK is missing:** install a .NET 10 SDK and retry the build command above.

## License

This project is licensed under the GNU General Public License v3.0. See [`LICENSE`](LICENSE) for the full license text.
