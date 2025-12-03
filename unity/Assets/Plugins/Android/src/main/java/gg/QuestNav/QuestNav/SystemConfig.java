package gg.QuestNav.QuestNav;

import androidx.room.ColumnInfo;
import androidx.room.Entity;
import androidx.room.PrimaryKey;

@Entity(tableName = "System")
public class SystemConfig {
    @PrimaryKey
    @ColumnInfo(name = "ID")
    public int id;

    @ColumnInfo(name = "EnableAutoStartOnBoot")
    public Boolean enableAutoStartOnBoot;
}
