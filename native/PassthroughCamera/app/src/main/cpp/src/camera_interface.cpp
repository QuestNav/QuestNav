#include "camera_interface.h"

#include <android/log.h>

#define  LOG_TAG    "passthrough-camera-native"
#define  LOGI(...)  __android_log_print(ANDROID_LOG_INFO,LOG_TAG,__VA_ARGS__)
#define  LOGE(...)  __android_log_print(ANDROID_LOG_ERROR,LOG_TAG,__VA_ARGS__)


class CameraInterfaceImpl final: public CameraInterface {
public:
  CameraInterfaceImpl() {

  }

  ~CameraInterfaceImpl() override {
    stop();
  }
  void startJni(JNIEnv* env, jobject surface) override {
    theNativeWindow = ANativeWindow_fromSurface(env, surface);
    openCamera();

    ACameraOutputTarget_create(theNativeWindow, &cameraOutputTarget);
    ACaptureRequest_addTarget(captureRequest, cameraOutputTarget);

    ACaptureSessionOutput_create(theNativeWindow, &sessionOutput);
    ACaptureSessionOutputContainer_add(captureSessionOutputContainer, sessionOutput);

    ACameraDevice_createCaptureSession(cameraDevice, captureSessionOutputContainer,
                                       &captureSessionStateCallbacks, &captureSession);

    ACameraCaptureSession_setRepeatingRequest(captureSession, nullptr, 1, &captureRequest, nullptr);
  }

  void stop() override {
    closeCamera();
    if (theNativeWindow != nullptr) {
      ANativeWindow_release(theNativeWindow);
      theNativeWindow = nullptr;
    }
  }

private:
  ANativeWindow *theNativeWindow;
  ACameraDevice *cameraDevice;
  ACaptureRequest *captureRequest;
  ACameraOutputTarget *cameraOutputTarget;
  ACaptureSessionOutput *sessionOutput;
  ACaptureSessionOutputContainer *captureSessionOutputContainer;
  ACameraCaptureSession *captureSession;

  ACameraDevice_StateCallbacks deviceStateCallbacks;
  ACameraCaptureSession_stateCallbacks captureSessionStateCallbacks;

  static void camera_device_on_disconnected(void *context, ACameraDevice *device) {
    LOGI("Camera(id: %s) is disconnected.\n", ACameraDevice_getId(device));
  }

  static void camera_device_on_error(void *context, ACameraDevice *device, int error) {
    LOGE("Error(code: %d) on Camera(id: %s).\n", error, ACameraDevice_getId(device));
  }

  static void capture_session_on_ready(void *context, ACameraCaptureSession *session) {
    LOGI("Session is ready. %p\n", session);
  }

  static void capture_session_on_active(void *context, ACameraCaptureSession *session) {
    LOGI("Session is activated. %p\n", session);
  }

  static void capture_session_on_closed(void *context, ACameraCaptureSession *session) {
    LOGI("Session is closed. %p\n", session);
  }

  void openCamera()
  {
    ACameraIdList *cameraIdList = nullptr;
    ACameraMetadata *cameraMetadata = nullptr;

    const char *selectedCameraId = nullptr;
    camera_status_t camera_status = ACAMERA_OK;
    ACameraManager *cameraManager = ACameraManager_create();

    camera_status = ACameraManager_getCameraIdList(cameraManager, &cameraIdList);
    if (camera_status != ACAMERA_OK) {
      LOGE("camera not ok\n");
      return;
    }

    if (cameraIdList->numCameras < 1) {
      LOGE("not enough cameras\n");
      return;
    }

    selectedCameraId = cameraIdList->cameraIds[1];

    camera_status = ACameraManager_getCameraCharacteristics(cameraManager, selectedCameraId,
                                                            &cameraMetadata);

    deviceStateCallbacks.onDisconnected = camera_device_on_disconnected;
    deviceStateCallbacks.onError = camera_device_on_error;

    camera_status = ACameraManager_openCamera(cameraManager, selectedCameraId,
                                              &deviceStateCallbacks, &cameraDevice);

    camera_status = ACameraDevice_createCaptureRequest(cameraDevice, TEMPLATE_PREVIEW,
                                                       &captureRequest);

    ACaptureSessionOutputContainer_create(&captureSessionOutputContainer);

    captureSessionStateCallbacks.onReady = capture_session_on_ready;
    captureSessionStateCallbacks.onActive = capture_session_on_active;
    captureSessionStateCallbacks.onClosed = capture_session_on_closed;

    ACameraMetadata_free(cameraMetadata);
    ACameraManager_deleteCameraIdList(cameraIdList);
    ACameraManager_delete(cameraManager);
  }

  void closeCamera() {
    camera_status_t camera_status = ACAMERA_OK;

    if (captureRequest != nullptr) {
      ACaptureRequest_free(captureRequest);
      captureRequest = nullptr;
    }

    if (cameraOutputTarget != nullptr) {
      ACameraOutputTarget_free(cameraOutputTarget);
      cameraOutputTarget = nullptr;
    }

    if (cameraDevice != nullptr) {
      camera_status = ACameraDevice_close(cameraDevice);

      if (camera_status != ACAMERA_OK) {
        LOGE("Failed to close CameraDevice.\n");
      }
      cameraDevice = nullptr;
    }

    if (sessionOutput != nullptr) {
      ACaptureSessionOutput_free(sessionOutput);
      sessionOutput = nullptr;
    }

    if (captureSessionOutputContainer != nullptr) {
      ACaptureSessionOutputContainer_free(captureSessionOutputContainer);
      captureSessionOutputContainer = nullptr;
    }

    LOGI("Close Camera\n");
  }
};

std::unique_ptr<CameraInterface> CameraInterface::create() {
  return std::make_unique<CameraInterfaceImpl>();
}