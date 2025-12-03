package gg.QuestNav.QuestNav;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import androidx.room.Room;
import java.io.File;

/**
 * BroadcastReceiver to start QuestNav on boot. Reads the "enableAutoStartOnBoot" option from the
 * Room database.
 */
public class BootReceiver extends BroadcastReceiver {

    private static final String TAG = "BootReceiver";
    private static final String DB_NAME = "config.db";

    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();
        Log.d(TAG, "Received broadcast action: " + action);

        if (Intent.ACTION_BOOT_COMPLETED.equals(action)) {
            if (isAutoStartEnabled(context)) {
                Log.d(TAG, "Starting QuestNav");

                // Disable sleep mode
                Intent sleepIntent = new Intent("com.oculus.vrpowermanager.automation_disable");
                context.sendBroadcast(sleepIntent);

                Intent launchIntent = new Intent(context, com.unity3d.player.UnityPlayerGameActivity.class);
                launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                context.startActivity(launchIntent);
            } else {
                Log.d(TAG, "Not starting QuestNav, option disabled");
            }
        }
    }

    private boolean isAutoStartEnabled(Context context) {
        File externalFilesDir = context.getExternalFilesDir(null);
        if (externalFilesDir == null) {
            Log.e(TAG, "External files directory not available");
            return true;
        }

        String dbPath = new File(externalFilesDir, DB_NAME).getAbsolutePath();
        Log.d(TAG, "Looking for database at: " + dbPath);

        if (!new File(dbPath).exists()) {
            Log.d(TAG, "Database not found, defaulting to auto-start enabled");
            return true;
        }

        AppDatabase db = null;
        try {
            db = Room.databaseBuilder(context.getApplicationContext(), AppDatabase.class, DB_NAME)
                    .createFromFile(new File(dbPath))
                    .allowMainThreadQueries() // For simplicity in this BroadcastReceiver
                    .build();

            SystemConfig config = db.systemConfigDao().getSystemConfig();

            if (config != null) {
                Log.d(TAG, "Read enableAutoStartOnBoot: " + config.enableAutoStartOnBoot);
                return config.enableAutoStartOnBoot != null ? config.enableAutoStartOnBoot : true;
            } else {
                Log.d(TAG, "No config found, defaulting to auto-start enabled");
                return true;
            }
        } catch (Exception e) {
            Log.e(TAG, "Error reading database with Room: " + e.getMessage(), e);
            return true; // Default to enabled on error
        } finally {
            if (db != null && db.isOpen()) {
                db.close();
            }
        }
    }
}
