using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeConcurrencyAndCodeSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "employee_code_seq");

            // Continue after the highest existing code (EMP-0010 -> next is EMP-0011). Codes are
            // compared as numbers here, not text. An empty table leaves the sequence at its start (1).
            migrationBuilder.Sql("""
                SELECT setval('employee_code_seq', m)
                FROM (SELECT MAX(CAST(SUBSTRING(employee_code FROM 5) AS bigint)) AS m
                      FROM employees
                      WHERE employee_code ~ '^EMP-[0-9]+$') existing
                WHERE m IS NOT NULL;
                """);

            // xmin is PostgreSQL's built-in row version: Npgsql skips creating it, this only records it in the model.
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "employees",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "employees");

            migrationBuilder.DropSequence(
                name: "employee_code_seq");
        }
    }
}
