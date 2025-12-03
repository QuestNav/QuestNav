package gg.QuestNav.QuestNav;

import androidx.room.Database;
import androidx.room.RoomDatabase;

@Database(entities = {SystemConfig.class}, version = 1)
public abstract class AppDatabase extends RoomDatabase {
    public abstract SystemConfigDao systemConfigDao();
}
