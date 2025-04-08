#ifndef PASSTHROUGHCAMERA_CAMERA_INTERFACE_H
#define PASSTHROUGHCAMERA_CAMERA_INTERFACE_H

#include <android/native_window_jni.h>
#include <camera/NdkCameraDevice.h>
#include <camera/NdkCameraManager.h>
#include <jni.h>
#include <memory>

class CameraInterface {
public:
  static std::unique_ptr<CameraInterface> create();
  virtual ~CameraInterface() = default;
  virtual void startJni(JNIEnv* env, jobject surface) = 0;
  virtual void stop() = 0;
};

#endif //PASSTHROUGHCAMERA_CAMERA_INTERFACE_H
