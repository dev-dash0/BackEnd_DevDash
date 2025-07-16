using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevDash.Migrations
{
    /// <inheritdoc />
    public partial class userpremium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "statepricing",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "statepricing",
                table: "AspNetUsers");
        }
    }
}
