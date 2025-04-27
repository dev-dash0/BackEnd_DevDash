using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevDash.Migrations
{
    /// <inheritdoc />
    public partial class disableAcceptedInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedInvitation",
                table: "UserTenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptedInvitation",
                table: "UserTenants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
