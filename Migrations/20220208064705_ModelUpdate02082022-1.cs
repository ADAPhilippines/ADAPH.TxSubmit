using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADAPH.TxSubmit.Migrations
{
    public partial class ModelUpdate020820221 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "TxBytes",
                table: "Transactions",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TxSize",
                table: "Transactions",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TxBytes",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TxSize",
                table: "Transactions");
        }
    }
}
