#!/usr/bin/env bash
set -euo pipefail
umask 077

apk=$1/Android.apk
destination=$2
tools="${ANDROID_HOME:?}/build-tools/36.0.0"
: "${RELEASE_VERSION:?}" "${RELEASE_CODE:?}" "${ANDROID_KEY_ALIAS:?}"
: "${ANDROID_KEYSTORE_BASE64:?}" "${ANDROID_KEYSTORE_PASSWORD:?}" "${ANDROID_KEY_PASSWORD:?}"
expected=$(printf '%s' "${ANDROID_SIGNER_SHA256:?}" | tr -d ':' | tr 'A-F' 'a-f')
if ! [[ $expected =~ ^[a-f0-9]{64}$ ]]; then
  echo 'ANDROID_SIGNER_SHA256 must be the SHA-256 fingerprint of the release certificate.' >&2
  exit 1
fi

package=$("$tools/aapt2" dump badging "$apk")
if grep -Fxq 'application-debuggable' <<< "$package"; then
  echo 'Refusing to sign a debuggable release APK.' >&2
  exit 1
fi
if [[ ${package%%$'\n'*} != "package: name='gg.QuestNav.QuestNav' versionCode='$RELEASE_CODE' versionName='$RELEASE_VERSION' "* ]]; then
  echo 'Candidate APK does not match the expected QuestNav package, version name, and version code.' >&2
  exit 1
fi
"$tools/apksigner" verify "$apk"

keydir=$(mktemp -d)
trap 'rm -rf -- "$keydir"' EXIT
printf '%s' "$ANDROID_KEYSTORE_BASE64" | base64 --decode > "$keydir/signing.keystore"
unset ANDROID_KEYSTORE_BASE64
"$tools/apksigner" sign --ks "$keydir/signing.keystore" --ks-key-alias "$ANDROID_KEY_ALIAS" \
  --ks-pass env:ANDROID_KEYSTORE_PASSWORD --key-pass env:ANDROID_KEY_PASSWORD \
  --v4-signing-enabled false --out "$keydir/Android.apk" "$apk"
unset ANDROID_KEYSTORE_PASSWORD ANDROID_KEY_PASSWORD

certs=$("$tools/apksigner" verify --verbose --print-certs "$keydir/Android.apk")
if ! grep -Fxq 'Number of signers: 1' <<< "$certs" ||
   ! grep -Fxq "Signer #1 certificate SHA-256 digest: $expected" <<< "$certs"; then
  echo 'Signed APK did not verify with the pinned QuestNav release certificate.' >&2
  printf '%s\n' "$certs" >&2
  exit 1
fi
mkdir -p "$destination"
install -m 0644 "$keydir/Android.apk" "$destination/Android.apk"
printf 'Verified QuestNav %s (%s), signing certificate SHA-256 %s\n' "$RELEASE_VERSION" "$RELEASE_CODE" "$expected"
