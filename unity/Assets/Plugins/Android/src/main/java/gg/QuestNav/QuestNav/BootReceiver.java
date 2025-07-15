package gg.QuestNav.QuestNav;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.util.Log;

public class BootReceiver extends BroadcastReceiver {

    private static final String TAG = "BootReceiver";

    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();
        Log.d(TAG, "Received broadcast action: " + action);

        if (Intent.ACTION_BOOT_COMPLETED.equals(action)) {
            SharedPreferences prefs = context.getSharedPreferences(
                    context.getPackageName() + ".v2.playerprefs", Context.MODE_PRIVATE);
            if (prefs.getInt("AutoStart", 1) == 1) {
                Log.d(TAG, "Starting QuestNav");
                Intent launchIntent = new Intent(context, com.unity3d.player.UnityPlayerGameActivity.class);
                launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                context.startActivity(launchIntent);
            } else {
                Log.d(TAG, "Not starting QuestNav, option disabled");
            }
        }
    }
}