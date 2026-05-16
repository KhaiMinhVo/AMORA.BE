using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPetVibeDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetVibeData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Energy = table.Column<double>(type: "double precision", nullable: false),
                    Pitch = table.Column<double>(type: "double precision", nullable: false),
                    PitchVariance = table.Column<double>(type: "double precision", nullable: false),
                    IsMonotone = table.Column<bool>(type: "boolean", nullable: false),
                    DurationSec = table.Column<double>(type: "double precision", nullable: false),
                    CleanAudioUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetVibeData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetVibeData_VoicePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "VoicePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetVibeData_PostId",
                table: "PetVibeData",
                column: "PostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetVibeData");
        }
    }
}
