using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sanalink.API.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dhis2Mappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MetricName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataElementUid = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    FacilityId = table.Column<int>(type: "integer", nullable: true),
                    OrgUnitUid = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    PeriodType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dhis2Mappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncounterDiagnoses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EncounterId = table.Column<int>(type: "integer", nullable: false),
                    ICD10Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ICD10Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DiagnosisType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterDiagnoses_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenBlacklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Jti = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenBlacklists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncounterDiagnoses_EncounterId",
                table: "EncounterDiagnoses",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBlacklists_Jti",
                table: "TokenBlacklists",
                column: "Jti",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dhis2Mappings");

            migrationBuilder.DropTable(
                name: "EncounterDiagnoses");

            migrationBuilder.DropTable(
                name: "TokenBlacklists");
        }
    }
}
