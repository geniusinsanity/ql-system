# QL System - Logiciel de Gestion pour Quincaillerie (Algérie) 🇩🇿

برنامج مكتبي احترافي متكامل لإدارة محلات الكانكري، العتاد، مواد البناء، والترصيص الصحي في الجزائر، مصمم بلغة **#C** ليعمل بكفاءة وسرعة فائقة على الأجهزة القديمة والحديثة (Windows 7 SP1, 10, 11) مع وضع عدم الاتصال بالإنترنت 100% (Offline-First).

---

## 🚀 المميزات الرئيسية للكانكري

1. **نظام الكاسة السريع (Caisse / Comptoir):**
   - تحكم كامل بواسطة لوحة المفاتيح:
     - `F1`: تركيز مؤشر البحث بالاسم أو المرجع أو الباركود.
     - `F2`: دفع نقداً (Espèces) وطباعة الوصل فوراً.
     - `F3`: بيع بالكريدي واختيار الزبون.
     - `F5`: إفراغ السلة لعملية جديدة.
   - حساب تلقائي لمبلغ الصرف المرجع للزبون (*Rendu de monnaie*).

2. **محرك بحث فوري فائق السرعة (<2ms):**
   - مبني على **SQLite** بفهارس محسنة للبحث في آلاف المقاسات والمراجع (`VIS-440`, `PVC-32`, `M8`...).

3. **حزمة السلع المسبقة (Pack de Démarrage):**
   - أكثر من **200 سلعة كانكري حقيقية** مدمجة ومصنفة (براغي، بلاكو، بي في سي، كوابل كهرباء، قواطع، أدوات، سيليكون، ودهانات).
   - مزودة بأكواد باركود قياسية EAN-13 ومولد باركود داخلي لطباعة التيكيات للسلع السائبة.

4. **تسيير ديون الزبائن والمقاولين (Crédit & Dettes):**
   - متابعة ديون الحرفيين والزبائن (البلومبي، الماصو، التريسيان).
   - تسجيل دفعات التسديد الجزئي مع بونات استلام وكشف حساب تاريخي.

5. **نظام التجربة 7 أيام والتفعيل الدائم (Trial & Licensing):**
   - يعمل 7 أيام مجاناً من تاريخ أول تشغيل على الحاسوب.
   - حماية ضد التلاعب بساعة وتاريخ الويندوز (*Anti-Clock Rollback*).
   - بصمة عتاد فريدة لكل جهاز (*Machine ID*).
   - أداة المطور المستقلة `QLSystem.KeyGen` لتوليد مفاتيح التفعيل وإرسالها للزبائن عبر واتساب أو الهاتف دون الحاجة للإنترنت.

6. **الطباعة المتوافقة مع أجهزة المحلات:**
   - تذاكر كاسة حرارية 80mm و 58mm عبر أوامر **ESC/POS** المباشرة.
   - نبضة فتح درج النقود الإلكتروني (*Tiroir-caisse RJ11*).
   - فواتير وبونات تسليم A4 و A5 للمقاولين.

---

## 📁 هيكلة المشروع (Architecture Modulaire)

المشروع مقسم إلى وحدات نظيفة ومستقلة لتسهيل التطوير في **VS Code**:

```
QLSystem/
├── .vscode/                       # إعدادات VS Code والمهام الجاهزة
│   ├── tasks.json                 # مهام البناء والتشغيل والتصدير
│   └── settings.json              # إعدادات C# والـ Formatting
├── QLSystem.sln                   # ملف الحل الرئيسي
│
├── src/
│   ├── QLSystem.Core/             # 1. النماذج، الواجهات، والمنطق التجاري
│   │   ├── Enums/                 # PaymentType, UnitType, LicenseStatus, UserRole
│   │   ├── Models/                # Product, Customer, Sale, Purchase, StoreSettings
│   │   ├── Interfaces/            # IProductRepository, ISaleRepository, ILicenseService...
│   │   └── DTOs/                  # CartItemDto, DailyReportDto
│   │
│   ├── QLSystem.Data/             # 2. قاعدة بيانات SQLite المحلية
│   │   ├── DatabaseContext.cs     # إدارة الاتصال وجداول SQLite
│   │   ├── Repositories/          # ProductRepository, SaleRepository, CustomerRepository...
│   │   ├── Seed/SeedDatabase.cs   # 200+ سلعة جزائرية جاهزة بالباركود
│   │   └── Backup/BackupService.cs# النسخ الاحتياطي التلقائي للبيانات
│   │
│   ├── QLSystem.Licensing/        # 3. محرك الترخيص والـ 7 أيام التجريبية
│   │   ├── HardwareFingerprint.cs # استخراج بصمة الجهاز QL-ALG-XXXX-XXXX
│   │   ├── AntiClockTamper.cs     # كشف التلاعب بساعة وتاريخ الويندوز
│   │   ├── TrialManager.cs        # إدارة العداد التنازلي والقفل
│   │   └── LicenseValidator.cs    # التحقق التشفيري من كود التفعيل
│   │
│   ├── QLSystem.Hardware/         # 4. الربط مع العتاد والطابعات
│   │   ├── EscPosPrinter.cs       # التذاكر الحرارية 80mm/58mm والقص التلقائي
│   │   ├── CashDrawerHelper.cs    # نبضة فتح درج النقود RJ11
│   │   ├── BarcodeService.cs      # توليد وتنسيق ملصقات الباركود
│   │   └── InvoiceDocumentService.cs # فواتير A4/A5 للمقاولين
│   │
│   ├── QLSystem.KeyGen/           # 5. أداة المطور لتوليد السيريال (خاصة بك)
│   │   └── Program.cs             # واجهة سريعة لإدخال كود الزبون وتوليد التفعيل
│   │
│   └── QLSystem.App/              # 6. واجهة المستخدم الرسومية العصرية (WPF)
│       ├── ViewModels/            # CaisseViewModel, StockViewModel, CreditViewModel...
│       ├── Views/                 # CaisseView, StockView, CreditView, TrialLockView...
│       ├── Styles/                # ألوان وتصميم Windows 11 Fluent
│       └── Helpers/               # RelayCommand, ObservableObject
```

---

## 🛠️ كيفية العمل والتطوير عبر VS Code

1. **فتح المشروع:**
   - افتح مجلد المشروع في VS Code:
     ```bash
     code .
     ```
2. **بناء المشروع (Build):**
   - اضغط `Ctrl + Shift + B` واختر **Build Solution**.

3. **تشغيل أداة توليد المفاتيح (KeyGen):**
   - من قائمة Terminal -> Run Task -> اختر **Run KeyGen Tool**، أو نفذ:
     ```bash
     dotnet run --project src/QLSystem.KeyGen/QLSystem.KeyGen.csproj
     ```

4. **تصدير النسخة النهائية لنظام Windows (Publish):**
   - من قائمة Terminal -> Run Task -> اختر **Publish Windows 64-bit Release**، أو نفذ:
     ```bash
     dotnet publish src/QLSystem.App/QLSystem.App.csproj -c Release -r win-x64 --self-contained false -o ./publish/win-x64
     ```

---

## 💰 سيناريو البيع للزبون (Process Commercial)

1. تقوم بتثبيت البرنامج على كمبيوتر المحل (يشتغل مباشرة 7 أيام مجاناً بدون الحاجة للإنترنت).
2. صاحب المحل يجد السلع الأساسية جاهزة، ويبدأ في البيع وتجربة السرعة.
3. في اليوم الثامن: يظهر له قفل الشاشة الأنيق موضحاً أن بياناته محفوظة، ويعرض كود جهازه: `QL-ALG-7410-9921-X9A2` مع رقم هاتفك وواتساب.
4. الزبون يتصل بك ويدفع ثمن النسخة.
5. تفتح أداة `QLSystem.KeyGen` في حاسوبك، تدخل كود جهازه، فتحصل على كود التفعيل `ACT-XXXX-XXXX-XXXX-XXXX` وتبعثه له.
6. يدخله الزبون في الشاشة، فيتفعل البرنامج فوراً ومدى الحياة مع بقاء كل سلع ومبيعات المحل كما هي!
