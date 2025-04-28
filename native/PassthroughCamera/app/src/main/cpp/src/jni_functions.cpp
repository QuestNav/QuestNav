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
  Java_com_questnav_passthroughcamera_MainActivity_start(JNIEnv* env, jobject);

  JNIEXPORT void JNICALL
  Java_com_questnav_passthroughcamera_MainActivity_stop(JNIEnv* env, jobject);
}



JNIEXPORT void JNICALL Java_com_questnav_passthroughcamera_MainActivity_start(JNIEnv* env, jobject) {
  camera = CameraInterface::create();
  camera->startJni(env);
}

JNIEXPORT void JNICALL Java_com_questnav_passthroughcamera_MainActivity_stop(JNIEnv *env, jobject) {
    if (camera) {
      camera->stop();
    }
}