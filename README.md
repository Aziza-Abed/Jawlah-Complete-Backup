# نظام جولة - Jawlah System

مشروع تخرج لإدارة عمال البلدية.

## ما هو المشروع؟

نظام لإدارة عمال النظافة والصيانة في بلدية البيرة. يسمح للعمال بتسجيل الحضور والإبلاغ عن المشاكل، ويسمح للمشرفين بمتابعة العمال وتوزيع المهام.

## مكونات المشروع

```
Jawlah-Repo/
├── backend/          # ASP.NET Core 9 API
├── mobile/           # Flutter تطبيق الموبايل
├── web/              # React لوحة تحكم المشرفين
└── AzizaWEB/         # HTML/JS صفحات بسيطة (بديل)
```

## كيفية التشغيل

### 1. Backend (السيرفر)

```bash
cd backend/Jawlah.API

# تعيين connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=JawlahDB;..."

# تعيين JWT secret
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-32-characters-min"

# تشغيل
dotnet run
```

السيرفر يعمل على: `http://localhost:5000`

### 2. Mobile (تطبيق الموبايل)

```bash
cd mobile

# تحميل المكتبات
flutter pub get

# تشغيل
flutter run
```

تأكد من تغيير `baseUrl` في `lib/core/config/api_config.dart` ليشير للسيرفر.

### 3. Web Dashboard (لوحة التحكم)

```bash
cd web

# تحميل المكتبات
npm install

# تشغيل
npm run dev
```

## قاعدة البيانات

المشروع يستخدم SQL Server مع Entity Framework. عند أول تشغيل، يتم إنشاء الجداول تلقائياً.

### استيراد مناطق العمل (GIS)

ضع ملف الـ shapefile في:
```
backend/Jawlah.API/GisData/your-shapefile.shp
```

المناطق تُستورد تلقائياً عند بدء التشغيل إذا كانت الجداول فارغة.

## المستخدمين الافتراضيين

بعد تشغيل السيرفر، استخدم Swagger أو endpoint الـ register لإنشاء أول مستخدم Admin.

```
POST /api/auth/register
{
  "username": "admin",
  "password": "Admin123!",
  "fullName": "مدير النظام",
  "role": "Admin"
}
```

## المتطلبات

- .NET 9 SDK
- SQL Server (LocalDB يكفي للتطوير)
- Flutter 3.x
- Node.js 18+

## ملاحظات للتطوير

- `DeveloperMode:DisableGeofencing = true` في appsettings.json يسمح بتسجيل الدخول من أي موقع (للاختبار)
- كل الـ API endpoints تتطلب JWT token ما عدا `/api/auth/login` و `/api/health/ping`

## هيكل الـ API

| Endpoint | الوصف |
|----------|-------|
| `/api/auth` | تسجيل دخول وخروج |
| `/api/attendance` | الحضور والانصراف |
| `/api/tasks` | المهام |
| `/api/issues` | البلاغات والمشاكل |
| `/api/zones` | مناطق العمل |
| `/api/users` | إدارة المستخدمين |
| `/api/tracking` | تتبع GPS |
| `/api/notifications` | الإشعارات |
| `/api/reports` | التقارير |
| `/api/dashboard` | إحصائيات |

---

مشروع تخرج - جامعة بيرزيت
