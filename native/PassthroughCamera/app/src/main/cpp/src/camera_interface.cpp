#include "camera_interface.h"
#include "tag36h11.h"
#include "image_u8.h"

#include <android/log.h>
#include <android/native_window_jni.h>
#include <apriltag.h>
#include <camera/NdkCameraDevice.h>
#include <camera/NdkCameraManager.h>
#include <media/NdkImage.h>
#include <media/NdkImageReader.h>

#define  LOG_TAG    "passthrough-camera-native"
#define  LOGI(...)  __android_log_print(ANDROID_LOG_INFO,LOG_TAG,__VA_ARGS__)
#define  LOGE(...)  __android_log_print(ANDROID_LOG_ERROR,LOG_TAG,__VA_ARGS__)

// The Quest passthrough cameras are tagged with custom metadata tags for source type and position
static constexpr int CAMERA_SOURCE_TAG = 0x80004d00;
static constexpr int CAMERA_POSITION_TAG = CAMERA_SOURCE_TAG + 1;

// Passthrough cameras are marked as source type 0 and left/right are 0/1
static constexpr int CAMERA_SOURCE_PASSTHROUGH = 0;
static constexpr int PASSTHROUGH_LEFT = 0;
static constexpr int PASSTHROUGH_RIGHT = 1;

static constexpr int HAMMING = 1;
static constexpr int APRIL_THREADS = 4;
static constexpr double DECIMATE_FACTOR = 4.0;

class CameraInterfaceImpl final: public CameraInterface {
public:
  CameraInterfaceImpl() {
    tagFamily = tag36h11_create();
    tagDetector = apriltag_detector_create();
    apriltag_detector_add_family_bits(tagDetector, tagFamily, HAMMING);
    tagDetector->nthreads = APRIL_THREADS;
    tagDetector->quad_decimate = DECIMATE_FACTOR;
  }

  ~CameraInterfaceImpl() override {
    closeCamera();
  }
  void startJni(JNIEnv* env) override {
    openCamera();
    setupImageReader();
    setupSessionAndTargets();
  }

  void stop() override {
    closeCamera();
  }

  bool detectTag(image_u8_t* buffer) {
    auto detection = apriltag_detector_detect(tagDetector, buffer);
    if (detection == nullptr || detection->size == 0) {
      return false;
    }
    int size = detection->size;
    for (int i = 0; i < size; i++) {
      LOGI("Tag ID: %c", detection->data[i]);
    }
    return true;
  }

private:
  ANativeWindow* nativeWindow{nullptr};
  AImageReader* imageReader{nullptr};
  ACameraDevice* cameraDevice{nullptr};
  ACaptureRequest* captureRequest{nullptr};
  ACameraOutputTarget* cameraOutputTarget{nullptr};
  ACaptureSessionOutput* sessionOutput{nullptr};
  ACaptureSessionOutputContainer* captureSessionOutputContainer{nullptr};
  ACameraCaptureSession* captureSession{nullptr};

  AImageReader_ImageListener imageListener{};
  ACameraDevice_StateCallbacks deviceStateCallbacks{};
  ACameraCaptureSession_stateCallbacks captureSessionStateCallbacks{};

  apriltag_family_t* tagFamily{nullptr};
  apriltag_detector_t* tagDetector{nullptr};



  static void onDisconnected(void* context, ACameraDevice* device) {
    LOGI("Camera(id: %s) is disconnected.\n", ACameraDevice_getId(device));
  }

  static void onError(void* context, ACameraDevice* device, int error) {
    LOGE("Error(code: %d) on Camera(id: %s).\n", error, ACameraDevice_getId(device));
  }

  static void onReady(void* context, ACameraCaptureSession* session) {
    LOGI("Session is ready. %p\n", session);
  }

  static void onActive(void* context, ACameraCaptureSession* session) {
    LOGI("Session is activated. %p\n", session);
  }

  static void onClosed(void* context, ACameraCaptureSession* session) {
    LOGI("Session is closed. %p\n", session);
  }

  static void onImageAvailable(void* context, AImageReader* reader) {
    auto start = std::chrono::steady_clock::now();
    auto* cameraInterface = reinterpret_cast<CameraInterfaceImpl*>(context);
    AImage* image{nullptr};
    media_status_t media_status = AMEDIA_OK;
    media_status = AImageReader_acquireLatestImage(reader, &image);
    if (media_status != AMEDIA_OK || image == nullptr) {
      LOGE("Failed to acquire latest image");
      AImage_delete(image);
      return;
    }
    int32_t format;
    media_status = AImage_getFormat(image, &format);
    if (media_status != AMEDIA_OK) {
      LOGE("Failed to get image format");
      AImage_delete(image);
      return;
    }
    if (format != AIMAGE_FORMAT_YUV_420_888) {
      LOGE("Unsupported image format");
      AImage_delete(image);
      return;
    }
    uint8_t* yBuffer;
    int yLen;
    int yRowStride;

    AImage_getPlaneData(image, 0, &yBuffer, &yLen);
    AImage_getPlaneRowStride(image, 0, &yRowStride);

    image_u8 img {
      .width = 1920,
      .height = 1080,
      .stride = yRowStride,
      .buf = yBuffer
    };
  /*
    bool result = cameraInterface->detectTag(&img);
    if (result) {
      LOGI("Tag detected");
    } else {
      LOGI("Tag not detected");
    }
    */
    AImage_delete(image);
    auto end = std::chrono::steady_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    LOGI("Image processing took %lld ns", duration.count());
  }

  void openCamera()
  {
    ACameraIdList* cameraIdList = nullptr;
    ACameraMetadata* cameraMetadata = nullptr;

    const char* selectedCameraId = nullptr;
    camera_status_t camera_status = ACAMERA_OK;
    ACameraManager* cameraManager = ACameraManager_create();

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
    if (camera_status != ACAMERA_OK) {
      LOGE("Error retrieving camera characteristics.");
      return;
    }

    deviceStateCallbacks.onDisconnected = onDisconnected;
    deviceStateCallbacks.onError = onError;

    camera_status = ACameraManager_openCamera(cameraManager, selectedCameraId,
                                              &deviceStateCallbacks, &cameraDevice);

    if (camera_status != ACAMERA_OK) {
      LOGE("Error opening camera.");
      return;
    }

    camera_status = ACameraDevice_createCaptureRequest(cameraDevice, TEMPLATE_RECORD,
                                                       &captureRequest);
    if (camera_status != ACAMERA_OK) {
      LOGE("Error creating capture request.");
      return;
    }

    // 30 fps
    const int64_t value = 33333333;
    camera_status = ACaptureRequest_setEntry_i64(captureRequest,ACAMERA_SENSOR_FRAME_DURATION,
                                                 1, &value);
    if (camera_status != ACAMERA_OK) {
      LOGE("Failed to set frame duration");
      return;
    }

    captureSessionStateCallbacks.onReady = onReady;
    captureSessionStateCallbacks.onActive = onActive;
    captureSessionStateCallbacks.onClosed = onClosed;

    ACameraMetadata_free(cameraMetadata);
    ACameraManager_deleteCameraIdList(cameraIdList);
    ACameraManager_delete(cameraManager);
  }

  void setupImageReader() {
    media_status_t media_status = AMEDIA_OK;
    media_status = AImageReader_new(1920, 1080, AIMAGE_FORMAT_YUV_420_888, 2, &imageReader);
    if (media_status != AMEDIA_OK || imageReader == nullptr) {
      LOGE("Failed to create image reader");
      return;
    }
    imageListener.context = this;
    imageListener.onImageAvailable = onImageAvailable;

    media_status = AImageReader_setImageListener(imageReader, &imageListener);
    if (media_status != AMEDIA_OK) {
      LOGE("Failed to set image listener");
      return;
    }
    media_status = AImageReader_getWindow(imageReader, &nativeWindow);
    if (media_status != AMEDIA_OK  || nativeWindow == nullptr) {
      LOGE("Failed to get image reader window");
      return;
    }
    ANativeWindow_acquire(nativeWindow);
  }

  void setupSessionAndTargets() {
    ACameraOutputTarget_create(nativeWindow, &cameraOutputTarget);
    ACaptureRequest_addTarget(captureRequest, cameraOutputTarget);

    ACaptureSessionOutputContainer_create(&captureSessionOutputContainer);

    ACaptureSessionOutput_create(nativeWindow, &sessionOutput);
    ACaptureSessionOutputContainer_add(captureSessionOutputContainer, sessionOutput);

    ACameraDevice_createCaptureSession(cameraDevice, captureSessionOutputContainer,
                                       &captureSessionStateCallbacks, &captureSession);

    ACameraCaptureSession_setRepeatingRequest(captureSession, nullptr, 1, &captureRequest, nullptr);
  }

  void closeCamera() {
    camera_status_t camera_status = ACAMERA_OK;
    media_status_t media_status = AMEDIA_OK;

    imageListener.context = nullptr;
    imageListener.onImageAvailable = nullptr;
    media_status = AImageReader_setImageListener(imageReader, &imageListener);
    if (media_status != AMEDIA_OK) {
      LOGE("Failed to clear image listener");
    }

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

    if (nativeWindow != nullptr) {
      ANativeWindow_release(nativeWindow);
      nativeWindow = nullptr;
    }

    if (imageReader != nullptr) {
     AImageReader_delete(imageReader);
     imageReader = nullptr;
    }

    LOGI("Close Camera\n");
  }
};

std::unique_ptr<CameraInterface> CameraInterface::create() {
  return std::make_unique<CameraInterfaceImpl>();
}
