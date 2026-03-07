# FollowUp Web Dashboard

لوحة تحكم إدارية لنظام متابعة العمال الميدانيين — مبنية بـ React 19 + TypeScript + Vite + Tailwind CSS.

## المميزات

- لوحة تحكم للمسؤول والمشرف مع إحصائيات فورية
- إدارة المهام (إنشاء، تعيين، متابعة، موافقة/رفض)
- خريطة حية لتتبع العمال عبر SignalR
- إدارة المناطق الجغرافية مع استيراد ملفات GIS
- نظام البلاغات والطعون
- سجلات الحضور والنشاط
- واجهة عربية RTL بالكامل

## التشغيل

```bash
npm install
npm run dev
```

يعمل على `http://localhost:5173` ويتصل بالـ API على `http://localhost:5000/api`.

## البنية

```
src/
├── api/          # طبقة الاتصال بالـ API (Axios)
├── components/   # مكونات مشتركة (Layout, UI, Common)
├── contexts/     # React Context (Auth, Notifications, Toast)
├── hooks/        # Custom Hooks (useTrackingHub, usePageTitle)
├── pages/        # صفحات التطبيق (30+ صفحة)
├── routes/       # التوجيه مع حماية الصلاحيات
├── types/        # TypeScript type definitions
└── utils/        # أدوات مساعدة
```
