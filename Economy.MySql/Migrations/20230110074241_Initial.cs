using Microsoft.EntityFrameworkCore.Migrations;

namespace Economy.MySql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feli_Economy_MySql_Accounts",
                columns: table => new
                {
                    OwnerId = table.Column<string>(nullable: false),
                    OwnerType = table.Column<string>(nullable: false),
                    Balance = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feli_Economy_MySql_Accounts", x => new { x.OwnerId, x.OwnerType });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feli_Economy_MySql_Accounts");
        }
    }
}
