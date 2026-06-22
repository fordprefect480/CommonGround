using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class FilterMembershipPaymentSessionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembershipPayment_StripeCheckoutSessionId",
                table: "MembershipPayments");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayment_StripeCheckoutSessionId",
                table: "MembershipPayments",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "[StripeCheckoutSessionId] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembershipPayment_StripeCheckoutSessionId",
                table: "MembershipPayments");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayment_StripeCheckoutSessionId",
                table: "MembershipPayments",
                column: "StripeCheckoutSessionId",
                unique: true);
        }
    }
}
