using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caimmand.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IteracionB5_ExternalIdTextIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Cases_Context_GIN\";");
            migrationBuilder.Sql("CREATE INDEX \"IX_Cases_Context_ExternalId\" ON \"Cases\" ((\"Context\" ->> 'externalId'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Cases_Context_ExternalId\";");
            migrationBuilder.Sql("CREATE INDEX \"IX_Cases_Context_GIN\" ON \"Cases\" USING GIN (\"Context\");");
        }
    }
}
