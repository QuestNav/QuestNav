# Building WPILib for Meta Quest 3 (Android arm64-v8a)

This guide covers building `wpiutil`, `wpinet`, and `ntcore` from WPILib for the Meta Quest 3.

## 1. Prerequisites

- Ubuntu/Debian-based Linux system
- Android SDK with NDK installed
- CMake 3.20+
- Ninja build system
- Git

## 2. Install Required Packages

```bash
sudo apt update
sudo apt install cmake ninja-build git protobuf-compiler libprotobuf-dev sdkmanager
```

### Accept Licenses
```bash
sdkmanager --licenses
```

### Install tools
```bash
dkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0" "ndk;r29"
```

### Verify NDK Installation

```bash
cat $ANDROID_NDK_HOME/source.properties
```

## 3. Build Protobuf for Android

ntcore requires protobuf. You must cross-compile it for Android arm64-v8a.

### Clone Protobuf

```bash
cd ~
git clone https://github.com/protocolbuffers/protobuf.git
cd protobuf
git checkout v32.3
```

### Build for Android

```bash
mkdir build-android && cd build-android

cmake .. \
    -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake \
    -DANDROID_ABI=arm64-v8a \
    -DANDROID_PLATFORM=android-32 \
    -DCMAKE_BUILD_TYPE=Release \
    -Dprotobuf_BUILD_TESTS=OFF \
    -Dprotobuf_BUILD_SHARED_LIBS=OFF \
    -Dprotobuf_BUILD_PROTOC_BINARIES=OFF \
    -Dabseil_BUILD_TESTING=OFF \
    -DCMAKE_INSTALL_PREFIX=$HOME/protobuf-android \
    -G Ninja

ninja && ninja install
```

The protobuf libraries will be installed to `~/protobuf-android`.

## 4. Build WPILib Components

### Clone allwpilib

```bash
cd ~
git clone https://github.com/wpilibsuite/allwpilib.git
cd allwpilib
```

### Build for Android

```bash
mkdir build-android && cd build-android

cmake .. \
    -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake \
    -DANDROID_ABI=arm64-v8a \
    -DANDROID_PLATFORM=android-32 \
    -DCMAKE_BUILD_TYPE=Release \
    -DWITH_JAVA=OFF \
    -DWITH_CSCORE=OFF \
    -DWITH_WPIMATH=OFF \
    -DWITH_WPILIB=OFF \
    -DWITH_GUI=OFF \
    -DWITH_EXAMPLES=OFF \
    -DWITH_TESTS=OFF \
    -DWITH_SIMULATION_MODULES=OFF \
    -DCMAKE_PREFIX_PATH=/home/$USER/protobuf-android \
    -DCMAKE_FIND_ROOT_PATH_MODE_PACKAGE=BOTH \
    -DProtobuf_PROTOC_EXECUTABLE=$(which protoc) \
    -G Ninja

ninja wpiutil wpinet ntcore
```

## 5. Locate Built Libraries

After a successful build, find the shared libraries:

```bash
find ~/allwpilib/build-android -name "*.so"
```

The libraries you need are:
- `libwpiutil.so`
- `libwpinet.so`
- `libntcore.so`

## Copy binaries

```bash
cp lib/*.so /mnt/c/Users/$USER/Documents/Code/QuestNav/unity/Assets/Plugins/ntcore/Android/arm64-v8a/
```