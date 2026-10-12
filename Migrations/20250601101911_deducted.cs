using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class deducted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deducteddate",
                table: "Transactions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deducteddate",
                table: "Transactions");
        }
    }
}
