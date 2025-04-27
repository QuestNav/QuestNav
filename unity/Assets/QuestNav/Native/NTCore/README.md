# Building NTCore Natives for use
1. Clone **allwpilib** to a your device.
    ```shell
        git clone https://github.com/wpilibsuite/allwpilib.git
        cd allwpilib
    ```
2. Ensure you have Visual Studio 2022 with support for C++ installed
3. Build the NTcore project. This can take up to 10 minutes!
   ```shell
   ./gradlew :ntcore:build -PwithPlatform=windowsx86-64,androidarm64
   ```
4. Copy the binaries to their respective subfolder under the `Plugins` directory in the Unity project.
They are located here after compiling:
   ```shell
   # Windows
   ntcore/build/libs/ntcore/shared/windowsx86-64/release/ntcore.dll
   
   # Android
   ntcore/build/libs/ntcore/shared/linuxathena/release/libntcore.so
   ```

You're done!
