using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MicroCoin.SQLite.Migrations
{
    public partial class initialcreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ECKeyPair",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurveType = table.Column<ushort>(nullable: false),
                    X = table.Column<byte[]>(nullable: true),
                    Y = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ECKeyPair", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountInfos",
                columns: table => new
                {
                    AccountNumber = table.Column<uint>(type: "integer", nullable: false),
                    State = table.Column<int>(nullable: false),
                    AccountKeyId = table.Column<int>(nullable: true),
                    LockedUntilBlock = table.Column<uint>(nullable: false),
                    Price = table.Column<ulong>(type: "integer", nullable: false),
                    AccountToPayPrice = table.Column<uint>(type: "integer", nullable: false),
                    NewPublicKeyId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInfos", x => x.AccountNumber);
                    table.ForeignKey(
                        name: "FK_AccountInfos_ECKeyPair_AccountKeyId",
                        column: x => x.AccountKeyId,
                        principalTable: "ECKeyPair",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountInfos_ECKeyPair_NewPublicKeyId",
                        column: x => x.NewPublicKeyId,
                        principalTable: "ECKeyPair",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountNumber = table.Column<uint>(type: "integer", nullable: false),
                    Id = table.Column<uint>(nullable: false),
                    AccountInfoAccountNumber = table.Column<uint>(nullable: true),
                    Balance = table.Column<ulong>(type: "integer", nullable: false),
                    UpdatedByBlock = table.Column<uint>(nullable: false),
                    TransactionCount = table.Column<uint>(nullable: false),
                    Name = table.Column<byte[]>(type: "BLOB", nullable: false),
                    AccountType = table.Column<ushort>(nullable: false),
                    UpdatedBlock = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountNumber);
                    table.ForeignKey(
                        name: "FK_Accounts_AccountInfos_AccountInfoAccountNumber",
                        column: x => x.AccountInfoAccountNumber,
                        principalTable: "AccountInfos",
                        principalColumn: "AccountNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountInfos_AccountKeyId",
                table: "AccountInfos",
                column: "AccountKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInfos_NewPublicKeyId",
                table: "AccountInfos",
                column: "NewPublicKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountInfoAccountNumber",
                table: "Accounts",
                column: "AccountInfoAccountNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "AccountInfos");

            migrationBuilder.DropTable(
                name: "ECKeyPair");
        }
    }
}
