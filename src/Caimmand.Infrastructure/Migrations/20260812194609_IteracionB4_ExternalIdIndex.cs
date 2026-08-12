using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caimmand.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IteracionB4_ExternalIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX \"IX_Cases_Context_GIN\" ON \"Cases\" USING GIN (\"Context\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Cases_Context_GIN\";");
        }
    }
}
