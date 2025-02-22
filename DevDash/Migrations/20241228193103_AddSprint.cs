using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevDash.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_AspNetUsers_ProjectManagerId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Tenants_TenantId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProjects_Project_ProjectId",
                table: "UserProjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Project",
                table: "Project");

            migrationBuilder.RenameTable(
                name: "Project",
                newName: "Projects");

            migrationBuilder.RenameIndex(
                name: "IX_Project_TenantId",
                table: "Projects",
                newName: "IX_Projects_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Project_ProjectManagerId",
                table: "Projects",
                newName: "IX_Projects_ProjectManagerId");

            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sprints_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_CreatedByUserId",
                table: "Sprints",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProjects_Projects_ProjectId",
                table: "UserProjects",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProjects_Projects_ProjectId",
                table: "UserProjects");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "Projects");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Project");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_TenantId",
                table: "Project",
                newName: "IX_Project_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ProjectManagerId",
                table: "Project",
                newName: "IX_Project_ProjectManagerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Project",
                table: "Project",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_AspNetUsers_ProjectManagerId",
                table: "Project",
                column: "ProjectManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Tenants_TenantId",
                table: "Project",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProjects_Project_ProjectId",
                table: "UserProjects",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
