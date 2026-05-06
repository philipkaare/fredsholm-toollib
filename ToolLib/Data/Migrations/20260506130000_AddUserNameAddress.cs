using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolLib.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNameAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AspNetUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FullName", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "Address", table: "AspNetUsers");
        }
    }
}
