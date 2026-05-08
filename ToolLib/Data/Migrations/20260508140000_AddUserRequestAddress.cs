using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolLib.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRequestAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "UserRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Address", table: "UserRequests");
        }
    }
}
