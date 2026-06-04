#!/usr/bin/env bash

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

echo "==> Installing wasm-tools workload (enables runtime relinking to trim dotnet.native.wasm)"
dotnet workload install wasm-tools

echo "==> Publishing docs site (Release)"
dotnet publish src/MudKeyboard.Docs/MudKeyboard.Docs.csproj -c Release -o publish

echo "==> Done. Static site is in ./publish/wwwroot"
