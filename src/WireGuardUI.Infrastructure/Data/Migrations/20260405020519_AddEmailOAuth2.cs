using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WireGuardUI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOAuth2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthType",
                table: "EmailSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OAuth2ClientId",
                table: "EmailSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OAuth2ClientSecret",
                table: "EmailSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OAuth2RefreshToken",
                table: "EmailSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthType",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "OAuth2ClientId",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "OAuth2ClientSecret",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "OAuth2RefreshToken",
                table: "EmailSettings");
        }
    }
}
