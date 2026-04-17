namespace Modules.PurchasesCore
{
    public readonly struct PurchaseProductInfo
    {
        public PurchaseProductInfo(
            string productId,
            string title,
            string description,
            string price,
            string priceValue,
            string priceCurrencyCode,
            string currencyImageUrl,
            bool isConsumed)
        {
            ProductId = productId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Price = price ?? string.Empty;
            PriceValue = priceValue ?? string.Empty;
            PriceCurrencyCode = priceCurrencyCode ?? string.Empty;
            CurrencyImageUrl = currencyImageUrl ?? string.Empty;
            IsConsumed = isConsumed;
        }

        public string ProductId { get; }
        public string Title { get; }
        public string Description { get; }
        public string Price { get; }
        public string PriceValue { get; }
        public string PriceCurrencyCode { get; }
        public string CurrencyImageUrl { get; }
        public bool IsConsumed { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ProductId);
    }
}
