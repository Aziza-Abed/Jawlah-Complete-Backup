# Flutter-specific ProGuard rules

# Keep Flutter engine classes
-keep class io.flutter.** { *; }
-keep class io.flutter.plugins.** { *; }

# Firebase Messaging
-keep class com.google.firebase.** { *; }

# Geolocator
-keep class com.baseflow.geolocator.** { *; }

# Background Service
-keep class id.flutter.flutter_background_service.** { *; }
