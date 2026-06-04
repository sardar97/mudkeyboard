#!/usr/bin/env bash
#
# Cloudflare Pages build script for the MudKeyboard docs site (Blazor WebAssembly).
#
# Cloudflare's build image ships with Node/Python/etc. but NOT the .NET SDK, so the
# previous build died at "dotnet: not found". This script installs .NET, adds the
# wasm-tools workload (required for the AOT publish), and publishes the WASM app.
#
# The static, deployable output lands in ./publish/wwwroot.
#
# Cloudflare Pages → Settings must match:
#   Production branch:       docs               (deploys come from the `docs` branch, never master)
#   Build command:           bash build.sh
#   Build output directory:  publish/wwwroot
#   Root directory:          /                  (repo root — leave blank/default)
#   Automatic deployments:   production branch only (so library-only pushes to master don't rebuild)
#
# To deploy the current master: git push origin origin/master:docs  (see RELEASE.md).
#
set -euo pipefail

DOTNET_CHANNEL="10.0"                 # docs site targets net10.0
DOTNET_INSTALL_DIR="${HOME}/.dotnet"

# Keep CI logs clean and skip the first-run experience.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

echo "==> Installing .NET SDK (channel ${DOTNET_CHANNEL})"
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_INSTALL_DIR}"

# Put the freshly installed SDK on PATH for the rest of this script.
export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
export PATH="${DOTNET_INSTALL_DIR}:${PATH}"

dotnet --info

echo "==> Installing wasm-tools workload (required for RunAOTCompilation)"
dotnet workload install wasm-tools

echo "==> Publishing docs site (Release, AOT)"
# AOT is passed on the command line (not set in the .csproj) so that an ordinary restore/build — e.g. CI —
# doesn't require the wasm-tools workload. See the comment in MudKeyboard.Docs.csproj.
dotnet publish src/MudKeyboard.Docs/MudKeyboard.Docs.csproj -c Release -p:RunAOTCompilation=true -o publish

echo "==> Done. Static site is in ./publish/wwwroot"
