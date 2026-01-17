import 'package:flutter/material.dart';
import 'app_colors.dart';

class AppTheme {
  
  AppTheme._();

  
  static ThemeData get theme {
    return ThemeData(
      
      primaryColor: AppColors.primaryBlue,
      scaffoldBackgroundColor: AppColors.mainBackground,
      colorScheme: const ColorScheme.light(
        primary: AppColors.primaryBlue,
        secondary: AppColors.oliveGreen,
        surface: AppColors.cardBackground,
        error: AppColors.error,
      ),

      
      fontFamily: 'Cairo', 
      textTheme: const TextTheme(
        
        displayLarge: TextStyle(
          fontSize: 32,
          fontWeight: FontWeight.bold,
          color: AppColors.mainText,
          height: 1.2,
        ),
        displayMedium: TextStyle(
          fontSize: 28,
          fontWeight: FontWeight.bold,
          color: AppColors.mainText,
          height: 1.2,
        ),
        
        titleLarge: TextStyle(
          fontSize: 24,
          fontWeight: FontWeight.w600,
          color: AppColors.mainText,
          height: 1.3,
        ),
        titleMedium: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: AppColors.mainText,
          height: 1.3,
        ),
        titleSmall: TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.w500,
          color: AppColors.mainText,
          height: 1.4,
        ),
        
        bodyLarge: TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.normal,
          color: AppColors.mainText,
          height: 1.6,
        ),
        bodyMedium: TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.normal,
          color: AppColors.secondaryText,
          height: 1.6,
        ),
        bodySmall: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.normal,
          color: AppColors.secondaryText,
          height: 1.5,
        ),
        
        labelLarge: TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.w500,
          color: AppColors.mainText,
        ),
        labelMedium: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w500,
          color: AppColors.secondaryText,
        ),
        labelSmall: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.w500,
          color: AppColors.secondaryText,
        ),
      ),

      
      cardTheme: const CardThemeData(
        color: AppColors.cardBackground,
        elevation: 2,
        shadowColor: Colors.black12,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(12)),
        ),
        margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      ),

      
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primaryBlue,
          foregroundColor: AppColors.white,
          elevation: 2,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),

      
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primaryBlue,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          textStyle: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
        ),
      ),

      
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primaryBlue,
          side: const BorderSide(color: AppColors.primaryBlue, width: 1.5),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),

      
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.white,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide:
              const BorderSide(color: AppColors.secondaryText, width: 1),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide:
              const BorderSide(color: AppColors.secondaryText, width: 1),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.primaryBlue, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.error, width: 1),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.error, width: 2),
        ),
        labelStyle: const TextStyle(
          color: AppColors.secondaryText,
          fontSize: 14,
        ),
        hintStyle: const TextStyle(
          color: AppColors.secondaryText,
          fontSize: 14,
        ),
        errorStyle: const TextStyle(
          color: AppColors.error,
          fontSize: 12,
        ),
      ),

      
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.primaryBlue,
        foregroundColor: AppColors.white,
        elevation: 0,
        centerTitle: true,
        titleTextStyle: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: AppColors.white,
          fontFamily: 'Cairo',
        ),
        iconTheme: IconThemeData(
          color: AppColors.white,
          size: 24,
        ),
      ),

      
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: AppColors.white,
        selectedItemColor: AppColors.primaryBlue,
        unselectedItemColor: AppColors.secondaryText,
        selectedLabelStyle: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
        unselectedLabelStyle: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.normal,
        ),
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),

      
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.primaryBlue,
        foregroundColor: AppColors.white,
        elevation: 4,
      ),

      
      dividerTheme: const DividerThemeData(
        color: AppColors.secondaryText,
        thickness: 1,
        space: 16,
      ),

      
      iconTheme: const IconThemeData(
        color: AppColors.mainText,
        size: 24,
      ),

      
      useMaterial3: true,
      visualDensity: VisualDensity.adaptivePlatformDensity,
    );
  }
}

