package com.questnav.passthroughcamera;

import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import android.Manifest;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.view.Surface;
import android.view.SurfaceHolder;
import android.view.SurfaceView;
import android.widget.TextView;

import com.questnav.passthroughcamera.databinding.ActivityMainBinding;

public class MainActivity extends AppCompatActivity {

    // Used to load the 'passthroughcamera' library on application startup.
    static {
        System.loadLibrary("passthroughcamera");
    }

    public native String stringFromJNI();
    public native void start(Surface surface);
    public native void stop();

    SurfaceView surfaceView;
    SurfaceHolder surfaceHolder;



    private ActivityMainBinding binding;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {

            ActivityCompat.requestPermissions(
                    this,
                    new String[] { Manifest.permission.CAMERA },
                    1);
            return;
        }


        binding = ActivityMainBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());

        // Example of a call to a native method
        TextView tv = binding.sampleText;
        tv.setText(stringFromJNI());

        surfaceView = binding.cameraView;
        surfaceHolder = surfaceView.getHolder();
        surfaceHolder.addCallback(new cameraCallback());
    }

    @Override
    protected void onDestroy() {
        stop();
        super.onDestroy();
    }

    private final class cameraCallback implements SurfaceHolder.Callback {
        @Override
        public void surfaceCreated(SurfaceHolder holder) {
            start(holder.getSurface());
        }
        @Override
        public void surfaceDestroyed(SurfaceHolder holder) {
            stop();
        }
        @Override
        public void surfaceChanged(SurfaceHolder holder, int format, int width, int height) {

        }
    }

}