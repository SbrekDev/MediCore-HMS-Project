using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientConsentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsentGivenAt",
                table: "Patients",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentStatus",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentGivenAt",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ConsentStatus",
                table: "Patients");
        }
    }
}
