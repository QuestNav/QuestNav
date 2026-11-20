# NTCore native libraries

The C# bindings in this folder call `ntcore` through P/Invoke. The native libraries live in
`Assets/Plugins/ntcore`, and `Assets/Plugins/ntcore/VERSION` records exactly which allwpilib tag, commit,
NDK and Maven artifacts they came from. Keep that file in sync whenever you replace a library.

| Platform | Files | Source |
|---|---|---|
| Android `arm64-v8a` (Quest) | `libntcore.so`, `libwpinet.so`, `libwpiutil.so`, `libdatalog.so` | Built from source with the Android NDK |
| Windows `x64` (Editor) | `ntcore.dll`, `wpinet.dll`, `wpiutil.dll`, `datalog.dll` | Official WPILib release artifacts |

All four libraries are required on each platform: `ntcore` links against `wpinet`, `wpiutil` and `datalog`.
The `.so` and `.dll` files are stored with Git LFS (see the repository `.gitattributes`), so run
`git lfs install` before committing new ones.

## Android arm64-v8a

Requirements: Linux or macOS, git, CMake 3.21+, Ninja, and the NDK bundled with the project's Unity version
(`<Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/NDK`, r27c for Unity 6000.3).

```bash
TAG=v2027.0.0-alpha-8
NDK=$HOME/Unity/Hub/Editor/6000.3.21f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK
WORK=$PWD

git clone --depth 1 --branch $TAG https://github.com/wpilibsuite/allwpilib src
git -C src rev-parse HEAD   # must match allwpilib-commit in VERSION

# The prefix maps keep absolute build paths out of the binaries, which makes the build reproducible
MAP="-ffile-prefix-map=$WORK/src=allwpilib -ffile-prefix-map=$WORK/build-android=build"
cmake -S src -B build-android -G Ninja \
  -DCMAKE_TOOLCHAIN_FILE="$NDK/build/cmake/android.toolchain.cmake" \
  -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-31 -DANDROID_STL=c++_static \
  -DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON \
  -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON \
  "-DCMAKE_C_FLAGS=$MAP" "-DCMAKE_CXX_FLAGS=$MAP" \
  -DWPILIB_WITH_NTCORE=ON -DWPILIB_WITH_WPIMATH=OFF -DWPILIB_WITH_WPILIB=OFF -DWPILIB_WITH_CSCORE=OFF \
  -DWPILIB_WITH_GUI=OFF -DWPILIB_WITH_EXAMPLES=OFF -DWPILIB_WITH_TESTS=OFF -DWPILIB_WITH_SIMULATION_MODULES=OFF \
  -DWPILIB_WITH_BENCHMARK=OFF -DWPILIB_WITH_EXECUTABLES=OFF -DWPILIB_WITH_DOCS=OFF -DWPILIB_WITH_WPICAL=OFF \
  -DWPILIB_WITH_FIELDS=OFF
ninja -C build-android ntcore datalog   # also builds wpinet and wpiutil

# The NDK always compiles with -g; stripping takes the libraries from ~84 MB to ~5 MB
for l in ntcore wpinet wpiutil datalog; do
  "$NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-strip" --strip-unneeded \
    -o build-android/lib$l.so build-android/lib/lib$l.so
done
```

Use `darwin-x86_64` instead of `linux-x86_64` in the `llvm-strip` path on macOS.

Before copying the stripped files to `Assets/Plugins/ntcore/Android/arm64-v8a`, check them:

- Every `LOAD` segment must be 16 KB aligned (`Align 0x4000` in `llvm-readelf -lW lib<name>.so`), which
  `ANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON` provides. The `.so.meta` files declare `Is16KbAligned: true`.
- `llvm-readelf -d libntcore.so` should list only the four libraries above plus `libc`, `libm` and `libdl`
  as `NEEDED`. A `libc++_shared.so` entry means `ANDROID_STL` was not `c++_static`.
- Every `[DllImport("ntcore")]` entry point in this folder must appear in `llvm-readelf --dyn-syms libntcore.so`.

## Windows x64

The Editor uses WPILib's published shared release DLLs for the same version:

```bash
VER=2027.0.0-alpha-8
for l in ntcore wpinet wpiutil datalog; do
  url=https://frcmaven.wpi.edu/artifactory/release/org/wpilib/$l/$l-cpp/$VER/$l-cpp-$VER-windowsx86-64.zip
  curl -fsSLO "$url" && curl -fsSLO "$url.sha1"
  echo "$(cat ${url##*/}.sha1)  ${url##*/}" | sha1sum -c -
  unzip -joq ${url##*/} windows/x86-64/shared/$l.dll -d Windows/x64
done
```

Copy the four DLLs (not the `*jni.dll` files) to `Assets/Plugins/ntcore/Windows/x64`. They need the
Visual C++ 2015-2022 x64 runtime, version 14.30 or newer.

## After updating

1. Replace the files in place so the existing `.meta` files, and their GUIDs and import settings, are kept.
2. Update `Assets/Plugins/ntcore/VERSION` with the tag, commit, NDK, artifact checksums and file hashes.
3. Check that `git lfs ls-files` lists every new `.so` and `.dll` before pushing.
4. If the NT C API changed, update the bindings in this folder to match `ntcore/src/main/native/include/wpi/nt/ntcore_c.h`.
