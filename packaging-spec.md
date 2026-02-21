# Speech Card Viewer — Packaging & Distribution Spec

## Context

The app is feature-complete and needs to be packaged for distribution so users can install and run it without a development environment. The app is built with Avalonia UI 11 on .NET 8, targeting macOS, Windows, and Linux.

## Current State

- **No publish configuration exists** — no publish profiles, build scripts, or CI/CD
- **No app metadata** in csproj — no version, product name, company, or copyright
- **Icon**: only the stock `avalonia-logo.ico` exists — no `.icns` (macOS) or `.png` (Linux)
- **No macOS `Info.plist`** for `.app` bundle
- **QuestPDF ships native binaries** per platform — self-contained publish auto-selects the correct ones per RID
- **`ViewLocator` uses reflection** — `PublishTrimmed` will break it; avoid trimming or fix the locator first

## Files to Modify/Create

| File | Action |
|---|---|
| `src/CardViewer/CardViewer.csproj` | Add metadata, publish settings |
| `build.sh` | New — cross-platform build script |
| `src/CardViewer/Assets/app.icns` | New — macOS icon (convert from .ico or create new) |
| `src/CardViewer/Assets/app.png` | New — 512x512 PNG for Linux |
| `src/CardViewer/Info.plist` | New — macOS app bundle metadata |

## Step 1: Add App Metadata to csproj

Add to the `<PropertyGroup>` in `CardViewer.csproj`:

```xml
<AssemblyName>CardViewer</AssemblyName>
<Version>1.0.0</Version>
<Product>Speech Card Viewer</Product>
<Description>Create speech outlines and practice with 3x5 index cards</Description>
<ApplicationIcon>Assets/avalonia-logo.ico</ApplicationIcon>
```

## Step 2: Create macOS Info.plist

Create `src/CardViewer/Info.plist` with standard macOS app bundle keys:
- `CFBundleIdentifier`: `com.cardviewer.app`
- `CFBundleDisplayName`: `Speech Card Viewer`
- `CFBundleVersion` / `CFBundleShortVersionString`: `1.0.0`
- `NSHighResolutionCapable`: `true`
- `CFBundleIconFile`: `app.icns`

## Step 3: Create Build Script

Create `build.sh` that publishes for all three platforms:

### macOS (Apple Silicon + Intel)
```
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```
Then assemble `.app` bundle:
```
CardViewer.app/
  Contents/
    Info.plist
    MacOS/
      CardViewer          (published binary)
    Resources/
      app.icns
```

### Windows
```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
Output: single `CardViewer.exe` — zip for distribution.

### Linux
```
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```
Output: single `CardViewer` binary — tarball for distribution.

### Build script output structure
```
dist/
  CardViewer-macos-arm64/CardViewer.app
  CardViewer-macos-x64/CardViewer.app
  CardViewer-win-x64/CardViewer.exe
  CardViewer-linux-x64/CardViewer
```

## Step 4: Icon Assets

- Convert the existing `avalonia-logo.ico` to `.icns` (macOS) and `.png` (Linux) using `sips` (available on macOS)
- Or: create a custom app icon later and regenerate all formats

## What This Does NOT Include (Future Work)

- **macOS code signing & notarization** — requires Apple Developer account ($99/yr). Without it, users must right-click > Open on first launch to bypass Gatekeeper.
- **Windows installer** (MSIX/InnoSetup) — the zip-with-exe approach is sufficient initially.
- **Linux .deb/.rpm packages** — tarball is sufficient initially.
- **DMG creation** — the `.app` bundle can be distributed as a zip initially.
- **CI/CD** (GitHub Actions) — can be added later to automate builds on push/tag.
- **Auto-update** — not included in v1.

## Verification

1. `./build.sh` completes without errors for all three platforms
2. macOS: double-click `CardViewer.app` — app launches, all features work
3. Windows: run `CardViewer.exe` — app launches
4. Linux: `./CardViewer` — app launches
5. Confirm no .NET runtime installation is required on any platform
6. `dotnet test` still passes (no regressions)
