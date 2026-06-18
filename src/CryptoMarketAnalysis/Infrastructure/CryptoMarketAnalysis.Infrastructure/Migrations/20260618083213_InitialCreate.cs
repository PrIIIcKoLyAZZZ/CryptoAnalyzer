#pragma warning disable
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoMarketAnalysis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crypto_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crypto_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "market_data_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "market_data_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_data_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    price_usd = table.Column<decimal>(type: "numeric(28,10)", nullable: false),
                    market_cap_usd = table.Column<decimal>(type: "numeric(28,2)", nullable: true),
                    volume_24h_usd = table.Column<decimal>(type: "numeric(28,2)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_market_data_points_crypto_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "crypto_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_market_data_points_market_data_sources_market_data_source_id",
                        column: x => x.market_data_source_id,
                        principalTable: "market_data_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crypto_assets_symbol",
                table: "crypto_assets",
                column: "symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_market_data_points_asset_id_timestamp_utc",
                table: "market_data_points",
                columns: new[] { "asset_id", "timestamp_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_market_data_points_market_data_source_id_timestamp_utc",
                table: "market_data_points",
                columns: new[] { "market_data_source_id", "timestamp_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_market_data_points_asset_id_market_data_source_id_timestamp_utc",
                table: "market_data_points",
                columns: new[] { "asset_id", "market_data_source_id", "timestamp_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_market_data_sources_code",
                table: "market_data_sources",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_data_points");

            migrationBuilder.DropTable(
                name: "crypto_assets");

            migrationBuilder.DropTable(
                name: "market_data_sources");
        }
    }
}
