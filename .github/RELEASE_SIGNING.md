# Android release signing

The `Publish Release` workflow builds QuestNav on the Unity runner. A separate GitHub-hosted job signs `Android.apk` and verifies its package, version, and signing certificate before the APK can be attached to the release.

The QuestNav Android Release certificate has SHA-256 fingerprint `6B:BA:5E:B9:1C:40:75:3A:AE:0A:A4:49:C5:5C:7B:6A:5B:DC:68:10:8C:17:3C:EF:89:97:CB:46:90:68:3F:1B`. To verify a downloaded APK, run `apksigner verify --print-certs Android.apk` and compare the signer certificate SHA-256.

## GitHub environment

Configure the `android-release` environment in `QuestNav/QuestNav`. Allow only the `main` branch, require an independent release reviewer, and disable administrator bypass. Tiny builds the release candidate in the restricted Unity runner group. The permanent signing key stays in the GitHub-hosted signing job.

| Environment setting | Type | Value |
| --- | --- | --- |
| `ANDROID_KEYSTORE_BASE64` | Secret | Base64 encoding of the complete permanent Android keystore |
| `ANDROID_KEYSTORE_PASSWORD` | Secret | Keystore password |
| `ANDROID_KEY_PASSWORD` | Secret | Password for the signing key |
| `ANDROID_KEY_ALIAS` | Variable | Private-key entry alias |
| `ANDROID_SIGNER_SHA256` | Variable | SHA-256 fingerprint of that alias’s signing certificate, with or without colons |

Read the public fingerprint with `keytool -list -v -keystore <file> -alias <alias>`. Keep a recoverable backup of the keystore and its credentials under the release maintainers’ control. Use that same key for subsequent releases.

## Maven publishing

CI publishes snapshots from `main` through the `maven-snapshots` environment. The manually dispatched release workflow uses `maven-releases`, which requires independent approval. Both environments allow only `main`. Configure `PUBLISH_USERNAME` and `PUBLISH_PASSWORD` separately in each environment, using registry credentials scoped to the corresponding repository.
