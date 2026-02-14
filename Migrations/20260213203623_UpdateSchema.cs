using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanalink.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: LabOrders table already created in AddLabOrderEntity migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
