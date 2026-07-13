using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caimmand.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultSla = table.Column<TimeSpan>(type: "interval", nullable: true),
                    DefaultPriority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayIcon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseDefinitionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Context = table.Column<string>(type: "jsonb", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitions_Code",
                table: "CaseDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CaseDefinitionCode",
                table: "Cases",
                column: "CaseDefinitionCode");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_SourceSystem",
                table: "Cases",
                column: "SourceSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Status",
                table: "Cases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_CaseId",
                table: "TimelineEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_CaseId_Sequence",
                table: "TimelineEvents",
                columns: new[] { "CaseId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseDefinitions");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "TimelineEvents");
        }
    }
}
