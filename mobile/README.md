# FollowUp Mobile

تطبيق الهاتف للعمال الميدانيين — مبني بـ Flutter مع دعم العمل بدون إنترنت.

## المميزات

- تسجيل دخول مع ربط الجهاز وتحقق OTP
- عرض وتنفيذ المهام مع إرفاق صور الإثبات
- تسجيل الحضور التلقائي عبر GPS والسياج الجغرافي
- الإبلاغ عن مشاكل ميدانية مع صور وموقع
- تقديم طعون على المهام المرفوضة
- تتبع الموقع في الخلفية عبر Background Isolate
- مزامنة تلقائية مع دعم Offline-First عبر Hive
- إشعارات فورية عبر Firebase Cloud Messaging

## التشغيل

```bash
flutter pub get
flutter run
```

يتصل بالـ API على `http://10.0.2.2:5000/api/` (Android Emulator) أو عبر `--dart-define=API_BASE_URL=http://IP:5000/api/` لجهاز حقيقي.

## البنية

```
lib/
├── core/         # إعدادات (API, Theme, Routing, Errors)
├── data/
│   ├── models/       # نماذج البيانات (Task, Issue, Attendance)
│   ├── repositories/ # مستودعات محلية (Hive) وبعيدة (API)
│   └── services/     # خدمات (Auth, Sync, Tracking, Firebase)
├── features/     # شاشات مقسمة حسب الميزة
├── presentation/ # عناصر واجهة مشتركة
└── providers/    # إدارة الحالة (Provider pattern)
```
