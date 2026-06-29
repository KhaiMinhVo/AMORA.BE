using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingPayOsTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""PetTransactions"" (""Id"", ""UserId"", ""TransactionType"", ""DiamondsDelta"", ""MetadataJson"", ""CreatedAt"", ""UpdatedAt"")
                SELECT 
                    gen_random_uuid(),
                    ""UserId"",
                    'PayOsPurchase',
                    ""DiamondsReceived"",
                    '{""orderCode"":""' || ""OrderCode"" || '"",""amount"":""' || ""AmountVnd"" || '"",""backfilled"":true}',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM ""PaymentTransactions"" pt
                WHERE pt.""Status"" = 1 
                  AND pt.""Provider"" = 'PayOS'
                  AND NOT EXISTS (
                      SELECT 1 FROM ""PetTransactions"" ptx 
                      WHERE ptx.""UserId"" = pt.""UserId"" 
                        AND ptx.""TransactionType"" = 'PayOsPurchase'
                        AND ptx.""DiamondsDelta"" = pt.""DiamondsReceived""
                        AND ptx.""MetadataJson"" LIKE '%' || CAST(pt.""OrderCode"" AS VARCHAR) || '%'
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
