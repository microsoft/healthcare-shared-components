#!/usr/bin/env bash
set -euo pipefail

source /etc/os-release
if [[ "$ID" != ubuntu || "$(dpkg --print-architecture)" != amd64 ]]; then
    echo "Azure Functions Core Tools installation requires an Ubuntu x64 agent." >&2
    exit 1
fi

if ! dpkg-query --show --showformat='${Status}' packages-microsoft-prod 2>/dev/null | grep -qx 'install ok installed'; then
    if ! command -v debsig-verify >/dev/null || ! command -v gpg >/dev/null; then
        sudo -n apt-get update
        sudo -n env DEBIAN_FRONTEND=noninteractive apt-get install --yes --no-install-recommends debsig-verify gpg
    fi

    verificationDirectory=$(mktemp -d)
    trap 'rm -rf -- "$verificationDirectory"' EXIT
    mkdir --mode=700 "$verificationDirectory/gnupg"

    # Ubuntu 25.10 onward requires microsoft-2025.asc and its corresponding fingerprint.
    expectedFingerprint=BC528686B50D79E339D3721CEB3E94ADBE1229CF
    keyId=${expectedFingerprint: -16}
    keyFile="$verificationDirectory/microsoft.asc"
    curl --fail --silent --show-error --location --retry 3 \
        --output "$keyFile" "https://packages.microsoft.com/keys/microsoft.asc"
    actualFingerprint=$(gpg --no-options --homedir "$verificationDirectory/gnupg" --batch \
        --show-keys --with-colons "$keyFile" \
        | awk -F: '$1 == "pub" { primary = 1 } $1 == "fpr" && primary { print $10; primary = 0 }')
    if [[ "$actualFingerprint" != "$expectedFingerprint" ]]; then
        echo "Microsoft signing key fingerprint verification failed." >&2
        exit 1
    fi

    mkdir -p "$verificationDirectory/keyrings/$keyId" "$verificationDirectory/policies/$keyId"
    gpg --no-options --homedir "$verificationDirectory/gnupg" --batch \
        --output "$verificationDirectory/keyrings/$keyId/microsoft.gpg" --dearmor "$keyFile"
    cat > "$verificationDirectory/policies/$keyId/microsoft.pol" <<EOF
<?xml version="1.0"?>
<!DOCTYPE Policy SYSTEM "https://www.debian.org/debsig/1.0/policy.dtd">
<Policy xmlns="https://www.debian.org/debsig/1.0/">
  <Origin Name="Microsoft" id="$keyId" Description="Microsoft release signing"/>
  <Selection>
    <Required Type="origin" File="microsoft.gpg" id="$keyId"/>
  </Selection>
  <Verification MinOptional="0">
    <Required Type="origin" File="microsoft.gpg" id="$keyId"/>
  </Verification>
</Policy>
EOF

    repositoryPackage="$verificationDirectory/packages-microsoft-prod.deb"
    curl --fail --silent --show-error --location --retry 3 \
        --output "$repositoryPackage" \
        "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb"
    debsig-verify --policies-dir "$verificationDirectory/policies" \
        --keyrings-dir "$verificationDirectory/keyrings" "$repositoryPackage"
    sudo -n dpkg --install "$repositoryPackage"
fi

sudo -n apt-get update
sudo -n env DEBIAN_FRONTEND=noninteractive apt-get install --yes --no-install-recommends azure-functions-core-tools-4

if ! verificationOutput=$(dpkg --verify azure-functions-core-tools-4); then
    printf 'Azure Functions Core Tools file verification failed.\n%s\n' "$verificationOutput" >&2
    exit 1
fi

if [[ -n "$verificationOutput" ]]; then
    printf 'Azure Functions Core Tools installed files do not match package metadata.\n%s\n' "$verificationOutput" >&2
    exit 1
fi

func --version
