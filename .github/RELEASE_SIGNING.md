# Android release signing

The `Publish Release` workflow builds QuestNav on the Unity runner. A separate GitHub-hosted job signs `Android.apk` and verifies its package, version, and signing certificate before the APK can be attached to the release.

The QuestNav Android Release certificate has SHA-256 fingerprint `6B:BA:5E:B9:1C:40:75:3A:AE:0A:A4:49:C5:5C:7B:6A:5B:DC:68:10:8C:17:3C:EF:89:97:CB:46:90:68:3F:1B`. To verify a downloaded APK, run `apksigner verify --print-certs Android.apk` and compare the signer certificate SHA-256.

## GitHub environment

Configure the `android-release` environment in `QuestNav/QuestNav`. Restrict its deployment branches to the branches authorized to produce releases before adding the key.

| Environment setting | Type | Value |
| --- | --- | --- |
| `ANDROID_KEYSTORE_BASE64` | Secret | Base64 encoding of the complete permanent Android keystore |
| `ANDROID_KEYSTORE_PASSWORD` | Secret | Keystore password |
| `ANDROID_KEY_PASSWORD` | Secret | Password for the signing key |
| `ANDROID_KEY_ALIAS` | Variable | Private-key entry alias |
| `ANDROID_SIGNER_SHA256` | Variable | SHA-256 fingerprint of that alias’s signing certificate, with or without colons |

Read the public fingerprint with `keytool -list -v -keystore <file> -alias <alias>`. Keep a recoverable backup of the keystore and its credentials under the release maintainers’ control. Use that same key for subsequent releases.
