using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WireGuardUI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOAuth2Tenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OAuth2Tenant",
                table: "EmailSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OAuth2Tenant",
                table: "EmailSettings");
        }
    }
}
