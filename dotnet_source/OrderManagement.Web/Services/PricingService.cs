using System;
using System.Collections.Generic;
using System.Linq;
using OrderManagement.Web.Models;

namespace OrderManagement.Web.Services
{
    /// <summary>
    /// Pure pricing rules: volume discounts by customer tier and quantity, plus
    /// destination-region tax. No I/O — these are the rules the migrated Java
    /// PricingService must reproduce exactly (equivalence target).
    /// </summary>
    public class PricingService
    {
        // Tier -> (minimum line quantity -> discount fraction). Highest qualifying band wins.
        private static readonly Dictionary<string, SortedDictionary<int, decimal>> TierBreaks =
            new Dictionary<string, SortedDictionary<int, decimal>>(StringComparer.OrdinalIgnoreCase)
        {
            ["STANDARD"] = new SortedDictionary<int, decimal> { { 0, 0.00m }, { 50, 0.02m }, { 200, 0.05m } },
            ["SILVER"]   = new SortedDictionary<int, decimal> { { 0, 0.03m }, { 50, 0.06m }, { 200, 0.09m } },
            ["GOLD"]     = new SortedDictionary<int, decimal> { { 0, 0.05m }, { 50, 0.09m }, { 200, 0.13m } },
            ["PLATINUM"] = new SortedDictionary<int, decimal> { { 0, 0.08m }, { 50, 0.12m }, { 200, 0.18m } }
        };

        // Tax class -> region -> rate. STD varies by region; RED is flat; EXM is always zero.
        private static readonly Dictionary<string, decimal> StandardRateByRegion =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["US"] = 0.0725m, ["CA"] = 0.1300m, ["EU"] = 0.2000m, ["UK"] = 0.2000m
        };

        /// <summary>Resolve the discount fraction for a tier and line quantity.</summary>
        public decimal GetDiscountRate(string pricingTier, int quantity)
        {
            if (string.IsNullOrWhiteSpace(pricingTier) || !TierBreaks.ContainsKey(pricingTier))
                pricingTier = "STANDARD";

            // LINQ: pick the highest break whose threshold the quantity satisfies.
            return TierBreaks[pricingTier]
                .Where(b => quantity >= b.Key)
                .OrderByDescending(b => b.Key)
                .Select(b => b.Value)
                .First();
        }

        /// <summary>Net (post-discount) value of a single line.</summary>
        public decimal PriceLine(Product product, int quantity, string pricingTier, out decimal discountRate)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            discountRate = GetDiscountRate(pricingTier, quantity);
            decimal gross = product.ListPrice * quantity;
            return Math.Round(gross * (1m - discountRate), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>Tax for a net line value given the product's tax class and the customer region.</summary>
        public decimal CalculateTax(decimal netLineValue, string taxClass, string regionCode)
        {
            switch ((taxClass ?? "STD").ToUpperInvariant())
            {
                case "EXM":
                    return 0m;
                case "RED":
                    return Math.Round(netLineValue * 0.05m, 2, MidpointRounding.AwayFromZero);
                default:
                    decimal rate = StandardRateByRegion.TryGetValue(regionCode ?? "US", out var r) ? r : 0.0725m;
                    return Math.Round(netLineValue * rate, 2, MidpointRounding.AwayFromZero);
            }
        }
    }
}
