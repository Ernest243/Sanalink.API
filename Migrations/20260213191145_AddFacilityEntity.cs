using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sanalink.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Facilities table
            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                });

            // 2. Seed a default facility for existing data
            migrationBuilder.Sql(
                "INSERT INTO \"Facilities\" (\"Name\", \"IsActive\", \"CreatedAt\") VALUES ('Default Facility', true, NOW()) ON CONFLICT DO NOTHING;");

            // 3. Add FacilityId to Patients (nullable first)
            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "Patients",
                type: "integer",
                nullable: true);

            // 4. Backfill existing patients with the default facility
            migrationBuilder.Sql(
                "UPDATE \"Patients\" SET \"FacilityId\" = (SELECT \"Id\" FROM \"Facilities\" ORDER BY \"Id\" LIMIT 1) WHERE \"FacilityId\" IS NULL;");

            // 5. Make FacilityId non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "FacilityId",
                table: "Patients",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // 6. Add FacilityId to AspNetUsers (nullable)
            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            // 7. Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Patients_FacilityId",
                table: "Patients",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FacilityId",
                table: "AspNetUsers",
                column: "FacilityId");

            // 8. Add foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Facilities_FacilityId",
                table: "AspNetUsers",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Facilities_FacilityId",
                table: "Patients",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Facilities_FacilityId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Facilities_FacilityId",
                table: "Patients");

            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Patients_FacilityId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FacilityId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "AspNetUsers");
        }
    }
}
