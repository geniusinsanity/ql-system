namespace QLSystem.Core.Enums
{
    /// <summary>
    /// طريقة الدفع في نقطة البيع (Mode de paiement)
    /// </summary>
    public enum PaymentType
    {
        /// <summary>
        /// نقداً (Espèces)
        /// </summary>
        Cash = 0,

        /// <summary>
        /// بالدين / على الحساب (Crédit / Dette client)
        /// </summary>
        Credit = 1,

        /// <summary>
        /// دفع مجزأ - جزء كاش وجزء كريدي (Paiement mixte)
        /// </summary>
        Split = 2,

        /// <summary>
        /// شيك بنكي أو بريدي (Chèque)
        /// </summary>
        Cheque = 3,

        /// <summary>
        /// تحويل بنكي أو بريد كاش / BaridiMob
        /// </summary>
        Transfer = 4
    }
}
