package gg.QuestNav.QuestNav;

import androidx.room.Dao;
import androidx.room.Query;

@Dao
public interface SystemConfigDao {
    @Query("SELECT * FROM System WHERE id = 1")
    SystemConfig getSystemConfig();
}
