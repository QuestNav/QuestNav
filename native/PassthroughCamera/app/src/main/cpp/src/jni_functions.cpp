#include <jni.h>
#include <string>
#include "camera_interface.h"

static std::unique_ptr<CameraInterface> camera;

extern "C" {
  JNIEXPORT jstring JNICALL
  Java_com_questnav_passthroughcamera_MainActivity_stringFromJNI(
          JNIEnv* env,
          jobject /* this */) {
      std::string hello = "Hello from C++";
      return env->NewStringUTF(hello.c_str());
  };

  JNIEXPORT void JNICALL
  Java_com_questnav_passthroughcamera_MainActivity_start(JNIEnv* env, jobject, jobject surface);

  JNIEXPORT void JNICALL
  Java_com_questnav_passthroughcamera_MainActivity_stop(JNIEnv* env, jobject);
}



JNIEXPORT void JNICALL Java_com_questnav_passthroughcamera_MainActivity_start(JNIEnv* env, jobject, jobject surface) {
  camera = CameraInterface::create();
  camera->startJni(env, surface);
}

JNIEXPORT void JNICALL Java_org_freedesktop_nativecamera2_NativeCamera2_stop(JNIEnv *env, jobject) {
    if (camera) {
      camera->stop();
    }
}