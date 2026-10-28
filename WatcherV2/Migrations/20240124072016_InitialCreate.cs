using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatcherV2.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SetupGeneral",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcName = table.Column<string>(type: "TEXT", nullable: true),
                    UseShutdownDefence = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermitShutdownTime = table.Column<int>(type: "INTEGER", nullable: false),
                    UseRebootProcess = table.Column<bool>(type: "INTEGER", nullable: false),
                    RebootProcessHours = table.Column<int>(type: "INTEGER", nullable: false),
                    RebootProcessMins = table.Column<int>(type: "INTEGER", nullable: false),
                    RebootProcessSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    UseLog = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogPath = table.Column<string>(type: "TEXT", nullable: true),
                    ScreenWidthRatio = table.Column<int>(type: "INTEGER", nullable: false),
                    LanType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupGeneral", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetupShortcut",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShortcutIconFile = table.Column<string>(type: "TEXT", nullable: true),
                    ShortcutProcFile = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupShortcut", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SetupGeneral");

            migrationBuilder.DropTable(
                name: "SetupShortcut");
        }
    }
}
