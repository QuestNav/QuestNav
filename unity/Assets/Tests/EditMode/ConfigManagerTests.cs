using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QuestNav.Config;
using QuestNav.WebServer;
using SQLite;
using ConfigTables = QuestNav.Config.Config;
using DetectionMode = QuestNav.Config.Config.AprilTagDetectorMode.DetectionMode;

namespace QuestNav.Tests
{
    public class ConfigManagerTests
    {
        private string dir;
        private string dbPath;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(
                Path.GetTempPath(),
                "questnav-config-" + Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(dir);
            dbPath = Path.Combine(dir, "config.db");
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(dir, true);
        }

        private static async Task<ConfigManager> OpenAsync(string path)
        {
            var manager = new ConfigManager(path);
            await manager.OpenAsync();
            return manager;
        }

        /// <summary>
        /// Creates a config database at <paramref name="path"/> with the given team number.
        /// </summary>
        private static async Task CreateDatabaseAsync(string path, int teamNumber)
        {
            var manager = await OpenAsync(path);
            await manager.SetTeamNumberAsync(teamNumber);
            await manager.CloseAsync();
        }

        private async Task<int> ReopenAndReadTeamNumberAsync()
        {
            var manager = await OpenAsync(dbPath);
            int teamNumber = await manager.GetTeamNumberAsync();
            await manager.CloseAsync();
            return teamNumber;
        }

        private static async Task<InvalidDataException> UploadRejectedAsync(
            ConfigManager manager,
            string candidatePath
        )
        {
            try
            {
                await manager.ReplaceDatabaseAsync(candidatePath);
            }
            catch (InvalidDataException ex)
            {
                return ex;
            }
            Assert.Fail("Upload was accepted");
            return null;
        }

        [Test]
        public async Task CorruptDatabaseIsMovedAsideAndRecreated()
        {
            await CreateDatabaseAsync(dbPath, 1234);

            // Garbage over the second page, past the schema that opening reads
            using (var file = new FileStream(dbPath, FileMode.Open, FileAccess.Write))
            {
                file.Seek(4096, SeekOrigin.Begin);
                file.Write(Enumerable.Repeat((byte)0xA5, 4096).ToArray(), 0, 4096);
            }

            Assert.AreEqual(
                new ConfigTables.Network().TeamNumber,
                await ReopenAndReadTeamNumberAsync()
            );
            Assert.AreEqual(1, Directory.GetFiles(dir, "config.db.corrupt-*").Length);
        }

        [Test]
        public async Task NonDatabaseFileIsMovedAsideAndRecreated()
        {
            File.WriteAllText(dbPath, "not a database, but long enough to have a header");

            Assert.AreEqual(
                new ConfigTables.Network().TeamNumber,
                await ReopenAndReadTeamNumberAsync()
            );
            Assert.AreEqual(1, Directory.GetFiles(dir, "config.db.corrupt-*").Length);
        }

        [Test]
        public async Task StoredUnimplementedDetectorModeIsResetOnOpen()
        {
            await (await OpenAsync(dbPath)).CloseAsync();
            using (var db = new SQLiteConnection(dbPath))
            {
                db.InsertOrReplace(
                    new ConfigTables.AprilTag
                    {
                        ID = 1,
                        AprilTagDetectorMode = (int)DetectionMode.ANCHOR_ENHANCED,
                    }
                );
            }

            var manager = await OpenAsync(dbPath);
            var mode = await manager.GetAprilTagDetectorModeAsync();
            await manager.CloseAsync();

            Assert.AreEqual(DetectionMode.TRADITIONAL, mode.Mode);
        }

        [Test]
        public async Task UploadIsAppliedOnTheNextStart()
        {
            await CreateDatabaseAsync(dbPath, 1234);
            string candidatePath = Path.Combine(dir, "upload.db");
            await CreateDatabaseAsync(candidatePath, 5678);

            var manager = await OpenAsync(dbPath);
            await manager.ReplaceDatabaseAsync(candidatePath);

            // The running app keeps the database it opened
            Assert.AreEqual(1234, await manager.GetTeamNumberAsync());
            await manager.CloseAsync();

            Assert.AreEqual(5678, await ReopenAndReadTeamNumberAsync());
            Assert.IsFalse(File.Exists(dbPath + ".pending"));
        }

        [Test]
        public async Task UploadWithTablesThatCannotBeMigratedIsRejected()
        {
            await CreateDatabaseAsync(dbPath, 1234);

            // The required table names, but none of the columns, so opening can't migrate them
            string candidatePath = Path.Combine(dir, "upload.db");
            using (var db = new SQLiteConnection(candidatePath))
            {
                foreach (string table in new[] { "Network", "System", "Camera", "Logging" })
                {
                    db.Execute($"CREATE TABLE {table} (Unrelated TEXT)");
                }
            }

            var manager = await OpenAsync(dbPath);
            await UploadRejectedAsync(manager, candidatePath);
            await manager.CloseAsync();

            Assert.IsFalse(File.Exists(dbPath + ".pending"));
            Assert.AreEqual(1234, await ReopenAndReadTeamNumberAsync());
        }

        [Test]
        public async Task UploadMissingConfigTablesIsRejected()
        {
            await CreateDatabaseAsync(dbPath, 1234);

            string candidatePath = Path.Combine(dir, "upload.db");
            using (var db = new SQLiteConnection(candidatePath))
            {
                db.Execute("CREATE TABLE Network (ID INTEGER PRIMARY KEY)");
            }

            var manager = await OpenAsync(dbPath);
            var ex = await UploadRejectedAsync(manager, candidatePath);
            await manager.CloseAsync();

            StringAssert.Contains("System", ex.Message);
            Assert.AreEqual(1234, await ReopenAndReadTeamNumberAsync());
        }

        [Test]
        public async Task ThrowingSubscriberDoesNotStopTheOthers()
        {
            var manager = new ConfigManager(dbPath);
            int received = -1;
            manager.OnTeamNumberChanged += _ => throw new InvalidOperationException("subscriber");
            manager.OnTeamNumberChanged += teamNumber => received = teamNumber;

            await manager.InitializeAsync();
            await manager.CloseAsync();

            Assert.AreEqual(new ConfigTables.Network().TeamNumber, received);
        }

        [Test]
        public async Task InitialEventsArriveBeforeTheEnableThatUsesThem()
        {
            var manager = new ConfigManager(dbPath);
            var order = new List<string>();
            manager.OnEnableHighQualityStreamsChanged += _ => order.Add("streamHighQuality");
            manager.OnEnablePassthroughStreamChanged += _ => order.Add("streamEnable");
            manager.OnAprilTagDetectorModeChanged += _ => order.Add("aprilTagMode");
            manager.OnEnableAprilTagDetectorChanged += _ => order.Add("aprilTagEnable");

            await manager.InitializeAsync();
            await manager.CloseAsync();

            // Enabling the stream picks its mode at once, limited unless high quality is on
            Assert.Less(order.IndexOf("streamHighQuality"), order.IndexOf("streamEnable"));
            // Enabling AprilTag reserves the camera at the resolution from the mode event
            Assert.Less(order.IndexOf("aprilTagMode"), order.IndexOf("aprilTagEnable"));
        }

        [Test]
        public void VideoStreamWithoutFrameSourceCanBeCreated()
        {
            // The passthrough stream can fail to start; the web server must still come up
            Assert.DoesNotThrow(() => new VideoStreamProvider(null));
        }
    }
}
