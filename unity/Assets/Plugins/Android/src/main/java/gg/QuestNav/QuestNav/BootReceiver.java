package gg.QuestNav.QuestNav;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.util.Log;

import java.io.File;

/**
 * BroadcastReceiver to start QuestNav on boot. Reads the "enableAutoStartOnBoot" option from the SQLite database
 * set in the Unity code.
 */
public class BootReceiver extends BroadcastReceiver {

    private static final String TAG = "BootReceiver";
    private static final String DB_NAME = "config.db";
    private static final String TABLE_NAME = "System";
    private static final String COLUMN_AUTO_START = "enableAutoStartOnBoot";

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

        SQLiteDatabase db = null;
        Cursor cursor = null;
        
        try {
            db = SQLiteDatabase.openDatabase(dbPath, null, SQLiteDatabase.OPEN_READONLY);
            cursor = db.query(TABLE_NAME, new String[]{COLUMN_AUTO_START}, "id = 1", null, null, null, null);
            
            if (cursor != null && cursor.moveToFirst()) {
                int value = cursor.getInt(cursor.getColumnIndexOrThrow(COLUMN_AUTO_START));
                Log.d(TAG, "Read enableAutoStartOnBoot: " + value);
                return value == 1;
            }
            
            Log.d(TAG, "No config found, defaulting to auto-start enabled");
            return true;
            
        } catch (Exception e) {
            Log.e(TAG, "Error reading database: " + e.getMessage());
            return true; // Default to enabled on error
            
        } finally {
            if (cursor != null) cursor.close();
            if (db != null) db.close();
        }
    }
}