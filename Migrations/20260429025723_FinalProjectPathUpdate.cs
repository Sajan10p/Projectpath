using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projectpath.Migrations
{
    /// <inheritdoc />
    public partial class FinalProjectPathUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_TutorId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_StudentGroups_StudentGroupId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedByStudentId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedStudentId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupJoinRequests_AspNetUsers_StudentId",
                table: "GroupJoinRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupLeaveRequests_AspNetUsers_StudentId",
                table: "GroupLeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_StudentId",
                table: "ProgressUpdates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_TutorId",
                table: "ProgressUpdates");

            migrationBuilder.AddColumn<string>(
                name: "Program",
                table: "Projects",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectFileName",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectFilePath",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectType",
                table: "Projects",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistrationApproved",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationApprovedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationStatus",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SenderId = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiverId = table.Column<string>(type: "TEXT", nullable: false),
                    MessageText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId",
                table: "Submissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProjectId",
                table: "Submissions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_TutorId",
                table: "Assignments",
                column: "TutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_StudentGroups_StudentGroupId",
                table: "Assignments",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedByStudentId",
                table: "GroupInvites",
                column: "InvitedByStudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedStudentId",
                table: "GroupInvites",
                column: "InvitedStudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupJoinRequests_AspNetUsers_StudentId",
                table: "GroupJoinRequests",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupLeaveRequests_AspNetUsers_StudentId",
                table: "GroupLeaveRequests",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_StudentId",
                table: "ProgressUpdates",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_TutorId",
                table: "ProgressUpdates",
                column: "TutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_TutorId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_StudentGroups_StudentGroupId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedByStudentId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedStudentId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupJoinRequests_AspNetUsers_StudentId",
                table: "GroupJoinRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupLeaveRequests_AspNetUsers_StudentId",
                table: "GroupLeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_StudentId",
                table: "ProgressUpdates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_TutorId",
                table: "ProgressUpdates");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropColumn(
                name: "Program",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectFileName",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectFilePath",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectType",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsRegistrationApproved",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationApprovedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_TutorId",
                table: "Assignments",
                column: "TutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_StudentGroups_StudentGroupId",
                table: "Assignments",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedByStudentId",
                table: "GroupInvites",
                column: "InvitedByStudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_AspNetUsers_InvitedStudentId",
                table: "GroupInvites",
                column: "InvitedStudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupJoinRequests_AspNetUsers_StudentId",
                table: "GroupJoinRequests",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupLeaveRequests_AspNetUsers_StudentId",
                table: "GroupLeaveRequests",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_StudentId",
                table: "ProgressUpdates",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressUpdates_AspNetUsers_TutorId",
                table: "ProgressUpdates",
                column: "TutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
