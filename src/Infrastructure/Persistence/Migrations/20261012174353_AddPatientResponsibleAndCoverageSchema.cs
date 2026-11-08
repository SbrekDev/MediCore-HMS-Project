using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientResponsibleAndCoverageSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResponsiblePatientId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AffiliateNumber",
                table: "CoverageEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ResponsiblePatientId",
                table: "Patients",
                column: "ResponsiblePatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_ResponsiblePatientId",
                table: "Patients",
                column: "ResponsiblePatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_ResponsiblePatientId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_ResponsiblePatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ResponsiblePatientId",
                table: "Patients");

            migrationBuilder.AlterColumn<string>(
                name: "AffiliateNumber",
                table: "CoverageEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
