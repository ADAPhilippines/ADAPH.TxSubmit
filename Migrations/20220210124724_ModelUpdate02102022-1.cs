using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADAPH.TxSubmit.Migrations
{
    public partial class ModelUpdate021020221 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StakingKey",
                table: "TransactionOwners",
                newName: "OwnerAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OwnerAddress",
                table: "TransactionOwners",
                newName: "StakingKey");
        }
    }
}
