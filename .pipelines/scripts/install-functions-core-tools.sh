#!/usr/bin/env bash
set -euo pipefail

source /etc/os-release
if [[ "$ID" != ubuntu || "$(dpkg --print-architecture)" != amd64 ]]; then
    echo "Azure Functions Core Tools installation requires an Ubuntu x64 agent." >&2
    exit 1
fi

if ! dpkg-query --show --showformat='${Status}' packages-microsoft-prod 2>/dev/null | grep -qx 'install ok installed'; then
    repositoryPackage=$(mktemp --suffix=.deb)
    trap 'rm -f "$repositoryPackage"' EXIT
    curl --fail --silent --show-error --location --retry 3 \
        --output "$repositoryPackage" \
        "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb"
    sudo -n dpkg --install "$repositoryPackage"
fi

sudo -n apt-get update
sudo -n env DEBIAN_FRONTEND=noninteractive apt-get install --yes --no-install-recommends azure-functions-core-tools-4
func --version
