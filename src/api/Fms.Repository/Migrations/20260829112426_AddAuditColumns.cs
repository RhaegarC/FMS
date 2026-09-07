using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fms.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // submissions.created_at -> created_on (rename preserves existing data + NOT NULL)
            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "submissions",
                newName: "created_on");

            // forms.updated_at -> last_modified_on (rename preserves existing data + NOT NULL)
            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "forms",
                newName: "last_modified_on");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "users",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "users",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "submissions",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "spaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "spaces",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "spaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "spaces",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "permissions",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "permissions",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "forms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "forms",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "forms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "forms");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "forms");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "forms");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "submissions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "last_modified_on",
                table: "forms",
                newName: "updated_at");
        }
    }
}
