using System.Collections.Generic;
using NikeStore.Models;

namespace NikeStore.ViewModels.Admin
{
    public class VariantRow
    {
        public int Id { get; set; }
        public string Sku { get; set; }
        public string Product { get; set; }
        public string Color { get; set; }
        public decimal Size { get; set; }
        public string SizeSystem { get; set; }
        public int Stock { get; set; }
    }

    public class InventoryPage
    {
        public string Query { get; set; }
        public int? VariantId { get; set; }
        public List<VariantRow> Variants { get; set; }
        public List<inventory_transactions> History { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
    }
}
