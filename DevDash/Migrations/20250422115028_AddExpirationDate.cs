using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevDash.Migrations
{
    /// <inheritdoc />
    public partial class AddExpirationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "PasswordReset");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "PasswordReset",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PasswordReset");

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "PasswordReset",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "PasswordReset",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "PasswordReset",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "PasswordReset",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "PasswordReset",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "PasswordReset",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "PasswordReset",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
