using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FooBooRealTime_back_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "Games",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "PlayerId", "Name" },
                values: new object[] { new Guid("09ac5e84-db5c-4131-0d1c-08dd1c5384cf"), "June" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "PlayerId",
                keyValue: new Guid("09ac5e84-db5c-4131-0d1c-08dd1c5384cf"));

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "Games",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
