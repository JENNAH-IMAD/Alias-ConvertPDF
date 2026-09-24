using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankStatementConverter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportTemplateField");

            migrationBuilder.DropTable(
                name: "History");

            migrationBuilder.DropTable(
                name: "PendingFileDeletions");

            migrationBuilder.DropTable(
                name: "ExportTemplates");

            migrationBuilder.DropTable(
                name: "StatementTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delimiter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Encoding = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IncludeHeader = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NumberCulture = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingFileDeletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingFileDeletions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatementTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    BalanceColumn = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditColumn = table.Column<int>(type: "integer", nullable: false),
                    DateColumn = table.Column<int>(type: "integer", nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DebitColumn = table.Column<int>(type: "integer", nullable: false),
                    Delimiter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DescriptionColumn = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NumberCulture = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParserKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReferenceColumn = table.Column<int>(type: "integer", nullable: true),
                    SkipLines = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatementTemplates_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplateField",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExportTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Format = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    SourceField = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplateField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportTemplateField_ExportTemplates_ExportTemplateId",
                        column: x => x.ExportTemplateId,
                        principalTable: "ExportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExportLabel = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InputFileName = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    InputStorageKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OutputDelimiter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OutputEncoding = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OutputFileName = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OutputStorageKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StandardStorageKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_History", x => x.Id);
                    table.ForeignKey(
                        name: "FK_History_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_History_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_History_ExportTemplates_ExportTemplateId",
                        column: x => x.ExportTemplateId,
                        principalTable: "ExportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_History_StatementTemplates_BankStatementTemplateId",
                        column: x => x.BankStatementTemplateId,
                        principalTable: "StatementTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_History_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplateField_ExportTemplateId_Position",
                table: "ExportTemplateField",
                columns: new[] { "ExportTemplateId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_History_BankAccountId",
                table: "History",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_History_BankStatementTemplateId",
                table: "History",
                column: "BankStatementTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_History_ClientId",
                table: "History",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_History_ExportTemplateId",
                table: "History",
                column: "ExportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_History_UserId_CreatedAt",
                table: "History",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementTemplates_BankId",
                table: "StatementTemplates",
                column: "BankId");
        }
    }
}
