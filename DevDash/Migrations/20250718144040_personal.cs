using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevDash.Migrations
{
    /// <inheritdoc />
    public partial class personal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_personaltenantId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_personaltenantId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "personaltenantId",
                table: "AspNetUsers",
                newName: "PersonalTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PersonalTenantId",
                table: "AspNetUsers",
                column: "PersonalTenantId",
                unique: true,
                filter: "[PersonalTenantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_PersonalTenantId",
                table: "AspNetUsers",
                column: "PersonalTenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_PersonalTenantId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PersonalTenantId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "PersonalTenantId",
                table: "AspNetUsers",
                newName: "personaltenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_personaltenantId",
                table: "AspNetUsers",
                column: "personaltenantId",
                unique: true,
                filter: "[personaltenantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_personaltenantId",
                table: "AspNetUsers",
                column: "personaltenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
