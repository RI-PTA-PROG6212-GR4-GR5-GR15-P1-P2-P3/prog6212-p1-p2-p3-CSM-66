using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDayAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Participants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Enrolments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Confirmed",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Registered");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Enrolments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Registered",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Confirmed");
        }
    }
}
