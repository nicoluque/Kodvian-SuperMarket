using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddCashSessionHandoverAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClosedByOperatorSessionId",
                table: "CashSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUsuarioId",
                table: "CashSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentOperatorSessionId",
                table: "CashSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentUsuarioId",
                table: "CashSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenedByOperatorSessionId",
                table: "CashSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenedByUsuarioId",
                table: "CashSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashSessionHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CashSessionId = table.Column<int>(type: "integer", nullable: false),
                    FromOperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    FromUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    ToOperatorSessionId = table.Column<int>(type: "integer", nullable: false),
                    ToUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessionHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashSessionHandovers_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashSessionHandovers_OperatorSessions_FromOperatorSessionId",
                        column: x => x.FromOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashSessionHandovers_OperatorSessions_ToOperatorSessionId",
                        column: x => x.ToOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashSessionHandovers_Usuarios_FromUsuarioId",
                        column: x => x.FromUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashSessionHandovers_Usuarios_ToUsuarioId",
                        column: x => x.ToUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_ClosedByOperatorSessionId",
                table: "CashSessions",
                column: "ClosedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_ClosedByUsuarioId",
                table: "CashSessions",
                column: "ClosedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_CurrentOperatorSessionId",
                table: "CashSessions",
                column: "CurrentOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_CurrentUsuarioId",
                table: "CashSessions",
                column: "CurrentUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_OpenedByOperatorSessionId",
                table: "CashSessions",
                column: "OpenedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_OpenedByUsuarioId",
                table: "CashSessions",
                column: "OpenedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionHandovers_CashSessionId",
                table: "CashSessionHandovers",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionHandovers_CreatedAt",
                table: "CashSessionHandovers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionHandovers_FromOperatorSessionId",
                table: "CashSessionHandovers",
                column: "FromOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionHandovers_FromUsuarioId",
                table: "CashSessionHandovers",
                column: "FromUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionHandovers_ToOperatorSessionId",
                table: "CashSessionHandovers",
                column: "ToOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionHandovers_ToUsuarioId",
                table: "CashSessionHandovers",
                column: "ToUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_OperatorSessions_ClosedByOperatorSessionId",
                table: "CashSessions",
                column: "ClosedByOperatorSessionId",
                principalTable: "OperatorSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_OperatorSessions_CurrentOperatorSessionId",
                table: "CashSessions",
                column: "CurrentOperatorSessionId",
                principalTable: "OperatorSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_OperatorSessions_OpenedByOperatorSessionId",
                table: "CashSessions",
                column: "OpenedByOperatorSessionId",
                principalTable: "OperatorSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_Usuarios_ClosedByUsuarioId",
                table: "CashSessions",
                column: "ClosedByUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_Usuarios_CurrentUsuarioId",
                table: "CashSessions",
                column: "CurrentUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_Usuarios_OpenedByUsuarioId",
                table: "CashSessions",
                column: "OpenedByUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_OperatorSessions_ClosedByOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_OperatorSessions_CurrentOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_OperatorSessions_OpenedByOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_Usuarios_ClosedByUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_Usuarios_CurrentUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_Usuarios_OpenedByUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropTable(
                name: "CashSessionHandovers");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_ClosedByOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_ClosedByUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_CurrentOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_CurrentUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_OpenedByOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_OpenedByUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "ClosedByOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "ClosedByUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "CurrentOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "CurrentUsuarioId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "OpenedByOperatorSessionId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "OpenedByUsuarioId",
                table: "CashSessions");
        }
    }
}
