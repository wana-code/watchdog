using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherV2.Models;

namespace WatcherV2.Data
{
    public class DataContext : DbContext
    {
        public DbSet<GeneralModel> SetupGeneral { get; set; }
        public DbSet<ShortcutModel> SetupShortcut { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = DBConfig.GetConnectionString();
            // SQLite 데이터베이스 연결 문자열 설정
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}
