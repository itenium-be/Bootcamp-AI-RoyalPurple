using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Itenium.SkillForge.Data.Migrations
{
    /// <inheritdoc />
    public partial class SkillCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing skills — they will be reseeded. Required because CategoryId
            // defaults to 0 and no SkillCategories row with Id=0 exists yet.
            migrationBuilder.Sql("DELETE FROM \"Skills\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Teams_TeamId",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_TeamId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Skills");

            migrationBuilder.AddColumn<int>(
                name: "LevelCount",
                table: "Skills",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCategories_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Skills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SkillLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Descriptor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillLevels_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillPrerequisites",
                columns: table => new
                {
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    PrerequisiteSkillId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillPrerequisites", x => new { x.SkillId, x.PrerequisiteSkillId });
                    table.ForeignKey(
                        name: "FK_SkillPrerequisites_Skills_PrerequisiteSkillId",
                        column: x => x.PrerequisiteSkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SkillPrerequisites_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CategoryId",
                table: "Skills",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_TeamId",
                table: "SkillCategories",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillLevels_SkillId",
                table: "SkillLevels",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillPrerequisites_PrerequisiteSkillId",
                table: "SkillPrerequisites",
                column: "PrerequisiteSkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_SkillCategories_CategoryId",
                table: "Skills",
                column: "CategoryId",
                principalTable: "SkillCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Skills_SkillCategories_CategoryId",
                table: "Skills");

            migrationBuilder.DropTable(
                name: "SkillPrerequisites");

            migrationBuilder.DropTable(
                name: "SkillLevels");

            migrationBuilder.DropIndex(
                name: "IX_Skills_CategoryId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "LevelCount",
                table: "Skills");

            migrationBuilder.DropTable(
                name: "SkillCategories");

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Skills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_TeamId",
                table: "Skills",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Teams_TeamId",
                table: "Skills",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
