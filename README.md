# KMG — نظام إدارة الأعمال الداخلي

باك اند ASP.NET Core (net8.0) على نفس معمارية مشروع **Elwasyet** المرجعي: 3 Layers (Api / Core / EF)
مع Generic Repository + UnitOfWork ونظام صلاحيات Ability ديناميكي.

## البنية

```
KMG.sln
 ├── KMG.Api    → Controllers, Program.cs, Authorization handlers
 ├── KMG.Core   → Models, Interfaces (+ IUnitOfWork), Services, DTOs, Enums
 └── KMG.EF     → ApplicationDbContext, Repositories (+ BaseRepository/UnitOfWork), Migrations
```

## تشغيل المشروع محليًا

1. تأكد إن عندك SQL Server / LocalDB متاح، وعدّل `ConnectionStrings:DefaultConnection` في
   `KMG.Api/appsettings.json` لو محتاج.
2. أول تشغيل للمشروع بيعمل Migrate + Seed تلقائي (الأدوار والصلاحيات وحساب صاحب العمل الافتراضي).

```bash
dotnet run --project KMG.Api
```

Swagger على `/swagger` وقت التشغيل في Development.

### حسابات الدخول (Seed)

| Username | Password | الدور |
|---|---|---|
| `owner` | `Owner@123` | صاحب العمل (كل الصلاحيات) |
| `accountant` | `Accountant@123` | المحاسب (كل حاجة عدا إدارة الموظفين) |

**غيّر الباسوردات دي قبل أي نشر حقيقي.**

### بيانات تجريبية (Dummy Data) للفرونت اند

أول ما تشغّل المشروع في وضع Development (وقاعدة البيانات فاضية)، `DummyDataSeeder`
(في `KMG.Api/Helper/DummyDataSeeder.cs`) بيزرع تلقائيًا سيناريو كامل جاهز تجربه على طول:

- 3 عملاء، 2 مورد، 4 خامات (واحدة منهم متعمدة تحت الحد الأدنى عشان تجرب تنبيه نقص المخزون)
- 6 موظفين: 2 بحساب دخول (`owner`, `accountant`) + 4 عمال بدون حساب دخول (رئيس عمال + 3 عمال)
- 3 مشاريع، **واحد من كل نوع** (تصنيع وتنفيذ / مقاولات باطن / توريد)، بحالات مختلفة (Mission, Completed)
  مع دفعات ومصاريف نثرية وحركات مخزون فعلية على كل مشروع
- مأمورية متسواة (project 1) ومأمورية لسه مفتوحة (project 2) — عشان تجرب الحالتين
- سلفة نشطة، خصم وحافز، وسجل صرف راتب فعلي واحد (بيوضح فرق مضاعفة يومية المأمورية)

البيانات دي بتتزرع مرة واحدة بس (لو لقى عملاء موجودين في الداتابيز مش بيعيد الزرع). لو عايز تبدأ
من صفر تاني: `dotnet ef database drop --force --project KMG.EF --startup-project KMG.Api` ثم
`dotnet ef database update ...` تاني وشغّل المشروع.

**مهم:** `DummyDataSeeder` بيشتغل بس لو `Environment=Development` (شوف الاستدعاء في `Program.cs`) —
لازم تتشال أو تتعطل قبل أي نشر حقيقي (Production).

## الموديولات المنفذة (Phase 1)

- Auth + Employees (بما فيهم موظفين بدون حساب دخول - العمال في `POST /api/Employee/workers`)
- Clients / Suppliers (+ سداد دفعات للموردين)
- Stock: Materials + حركات (شراء / صرف لمشروع / مرتجع من مشروع) + تنبيه نقص المخزون (`isLowStock`)
- Projects (الأنواع الثلاثة) + دفعات + مصاريف نثرية + مرفقات + سجل تدقيق (Audit Log) لكل مشروع
- Missions: عهدة، عمال المأمورية، تسوية العهدة (فرق يتحول تلقائيًا لمصروف نثري/استرجاع خزنة)
- Payroll: سلف، خصومات/حوافز، تشغيل رواتب بمنطق مضاعفة يومية المأمورية والترحيل بين الفترات
- الخزنة المركزية (Cash/Credit) — كل حركة مالية في النظام مربوطة تلقائيًا بمصدرها
- Dashboard مبسط

## غير منفذ حاليًا (مؤجل لمراحل لاحقة حسب الخطة المتفق عليها)

- SignalR + Hangfire (إشعارات لحظية ومهام مجدولة زي تنبيه نقص المخزون الدوري)
- وحدة الذكاء الاصطناعي لرصد المناقصات

## أوامر EF Core مفيدة

```bash
# إضافة migration جديدة بعد أي تعديل في الموديلز
dotnet ef migrations add <Name> --project KMG.EF --startup-project KMG.Api -o Migrations

# تطبيق آخر migration على قاعدة البيانات
dotnet ef database update --project KMG.EF --startup-project KMG.Api
```
