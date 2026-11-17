using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.IO;
using System.Web.Hosting;

namespace NikeStore.Infrastructure
{
    internal static class LocalDemoDatabase
    {
        private const string DatabaseName = "NikeStoreDemo";

        public static void EnsureCreated()
        {
            var entry = ConfigurationManager.ConnectionStrings["NikeStoreDBEntities"];
            if (entry == null || !string.Equals(entry.ProviderName, "System.Data.EntityClient", StringComparison.OrdinalIgnoreCase))
                return;

            var entity = new EntityConnectionStringBuilder(entry.ConnectionString);
            var connection = new SqlConnectionStringBuilder(entity.ProviderConnectionString);
            if (!string.Equals(connection.DataSource, @"(LocalDB)\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(connection.InitialCatalog, DatabaseName, StringComparison.OrdinalIgnoreCase) ||
                !connection.IntegratedSecurity || !string.IsNullOrEmpty(connection.AttachDBFilename))
                return;

            var script = HostingEnvironment.MapPath("~/App_Data/NikeStoreDemo.sql");
            if (string.IsNullOrEmpty(script) || !File.Exists(script))
                throw new FileNotFoundException("Khong tim thay App_Data/NikeStoreDemo.sql trong du an.");

            try
            {
                Initialize(connection, File.ReadAllText(script));
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    "Khong khoi tao duoc NikeStoreDemo. Kiem tra SQL Server Express LocalDB trong Visual Studio Installer. " +
                    "Xem loi SQL chi tiet ben duoi hoac mo HUONG_DAN_CHAY.txt de dung CSDL rieng.", ex);
            }
        }

        private static void Initialize(SqlConnectionStringBuilder connection, string script)
        {
            // Đóng kết nối thật sẽ giải phóng khóa kể cả khi việc tạo database bị lỗi.
            var master = new SqlConnectionStringBuilder(connection.ConnectionString)
            {
                InitialCatalog = "master",
                Pooling = false
            };
            using (var db = new SqlConnection(master.ConnectionString))
            {
                db.Open();
                using (var create = db.CreateCommand())
                {
                    create.CommandTimeout = 120;
                    create.CommandText = @"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = N'NikeStoreDemo.Setup',
    @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 60000;
IF @result < 0 THROW 50000, 'Cannot acquire the NikeStoreDemo setup lock.', 1;
IF DB_ID(N'NikeStoreDemo') IS NULL EXEC(N'CREATE DATABASE [NikeStoreDemo]');";
                    create.ExecuteNonQuery();
                }

                db.ChangeDatabase(DatabaseName);
                using (var transaction = db.BeginTransaction())
                using (var setup = new SqlCommand(script, db, transaction))
                {
                    setup.CommandTimeout = 120;
                    setup.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
        }
    }
}
