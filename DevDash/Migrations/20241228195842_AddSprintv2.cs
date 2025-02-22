using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevDash.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sprints_AspNetUsers_CreatedByUserId",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_Sprints_CreatedByUserId",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Sprints");

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedInvitation",
                table: "UserProjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedDate",
                table: "UserProjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "UserProjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Sprints",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_CreatedById",
                table: "Sprints",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Sprints_AspNetUsers_CreatedById",
                table: "Sprints",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sprints_AspNetUsers_CreatedById",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_Sprints_CreatedById",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "AcceptedInvitation",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "JoinedDate",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Sprints");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Sprints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Sprints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_CreatedByUserId",
                table: "Sprints",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sprints_AspNetUsers_CreatedByUserId",
                table: "Sprints",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
