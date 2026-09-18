namespace QLSystem.Hardware
{
    /// <summary>
    /// مسؤول عن توليد أوامر فتح درج النقود الإلكتروني (Tiroir-Caisse RJ11)
    /// </summary>
    public static class CashDrawerHelper
    {
        /// <summary>
        /// توليد نبضة كهربائية قياسية ESC/POS لفتح درج النقود عبر الطابعة الحرارية
        /// ESC p m t1 t2 -> 0x1B, 0x70, 0x00, 0x19, 0xFA
        /// </summary>
        public static byte[] GetOpenDrawerCommand()
        {
            return new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
        }
    }
}
