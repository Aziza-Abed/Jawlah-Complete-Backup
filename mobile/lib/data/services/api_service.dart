import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../../core/config/api_config.dart';
import '../../core/utils/storage_helper.dart';

import '../../core/errors/app_exception.dart';

class ApiService {
  static final ApiService instance = ApiService._init();
  factory ApiService() => instance;
  ApiService._init();

  late final Dio dioClient;

  String? _token;
  String? _refreshToken;

  Dio get dio => dioClient;

  void setUpApi() {
    // set up the dio client with our base url and timeout
    dioClient = Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: const Duration(seconds: ApiConfig.timeoutSeconds),
        sendTimeout: const Duration(seconds: ApiConfig.timeoutSeconds),
        receiveTimeout: const Duration(seconds: ApiConfig.timeoutSeconds),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    // add the interceptor to send the token with every request
    dioClient.interceptors.add(makeAuthHelper());

    // enable logging if we are in debug mode
    // note: requestHeader is false to avoid logging tokens
    if (ApiConfig.enableLogging) {
      dioClient.interceptors.add(LogInterceptor(
        requestBody: true,
        responseBody: true,
        requestHeader: false,
        responseHeader: false,
        error: true,
      ));
    }
  }

  // add token to requests and handle 401 with refresh
  InterceptorsWrapper makeAuthHelper() {
    return InterceptorsWrapper(
      onRequest: (options, handler) async {
        // add the token if we have one
        if (_token != null && _token!.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $_token';
        }
        return handler.next(options);
      },
      onError: (error, handler) async {
        if (error.response?.statusCode != 401) {
          return handler.next(error);
        }

        final requestPath = error.requestOptions.path;

        // don't try to refresh if the failing request is auth-related
        if (requestPath.contains('auth/login') ||
            requestPath.contains('auth/refresh') ||
            requestPath.contains('auth/verify-otp') ||
            requestPath.contains('auth/forgot-password') ||
            requestPath.contains('auth/reset-password')) {
          await cleanAuthData();
          return handler.next(error);
        }

        // if no refresh token or already retried, logout
        final alreadyRetried = error.requestOptions.extra['_retry'] == true;
        if (_refreshToken == null || _refreshToken!.isEmpty || alreadyRetried) {
          await cleanAuthData();
          return handler.next(error);
        }

        // try to refresh the token once
        try {
          final refreshResponse = await dioClient.post(
            ApiConfig.refreshToken,
            data: {'refreshToken': _refreshToken},
            options: Options(
              headers: {'Authorization': ''},
            ),
          );

          final responseData = refreshResponse.data;
          if (responseData['success'] == true && responseData['data'] != null) {
            final data = responseData['data'];
            final newToken = data['token'] as String?;
            final newRefreshToken = data['refreshToken'] as String?;

            if (newToken != null) {
              _token = newToken;
              await StorageHelper.saveToken(newToken);

              if (newRefreshToken != null) {
                _refreshToken = newRefreshToken;
                await StorageHelper.saveRefreshToken(newRefreshToken);
              }

              if (kDebugMode) debugPrint('Token refreshed successfully');

              // retry the original request with new token
              error.requestOptions.headers['Authorization'] = 'Bearer $newToken';
              error.requestOptions.extra['_retry'] = true;
              final retryResponse = await dioClient.fetch(error.requestOptions);
              return handler.resolve(retryResponse);
            }
          }

          // refresh response didn't have a valid token
          await cleanAuthData();
          return handler.next(error);
        } catch (e) {
          if (kDebugMode) debugPrint('Token refresh failed: $e');
          await cleanAuthData();
          return handler.next(error);
        }
      },
    );
  }

  Future<void> cleanAuthData() async {
    _token = null;
    _refreshToken = null;
    await StorageHelper.removeToken();
    await StorageHelper.removeRefreshToken();
    await StorageHelper.removeUser();
  }

  void updateToken(String? token) {
    _token = token;
  }

  void updateRefreshToken(String? refreshToken) {
    _refreshToken = refreshToken;
  }

  Future<void> loadToken() async {
    _token = await StorageHelper.getToken();
    _refreshToken = await StorageHelper.getRefreshToken();
  }

  // HTTP methods
  Future<Response> get(
    String endpoint, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dioClient.get(
        endpoint,
        queryParameters: queryParameters,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> post(
    String endpoint, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dioClient.post(
        endpoint,
        data: data,
        queryParameters: queryParameters,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> put(
    String endpoint, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dioClient.put(
        endpoint,
        data: data,
        queryParameters: queryParameters,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> delete(
    String endpoint, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dioClient.delete(
        endpoint,
        queryParameters: queryParameters,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  // handle dio errors and convert to app exceptions
  Exception _handleError(DioException error) {
    // timeout errors
    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.sendTimeout ||
        error.type == DioExceptionType.receiveTimeout) {
      return NetworkException('انتهت مهلة الاتصال. يرجى المحاولة مرة أخرى.');
    }

    // no internet
    if (error.type == DioExceptionType.connectionError) {
      return NetworkException('لا يوجد اتصال بالإنترنت.');
    }

    // request cancelled
    if (error.type == DioExceptionType.cancel) {
      return AppException('تم إلغاء الطلب.');
    }

    // server errors
    if (error.type == DioExceptionType.badResponse) {
      final statusCode = error.response?.statusCode;
      final message = error.response?.data?['message'] ??
          error.response?.data?['error'] ??
          'Server Error';

      if (statusCode == 401 || statusCode == 403) {
        return UnauthorizedException('غير مصرح. يرجى تسجيل الدخول مرة أخرى.');
      }
      if (statusCode == 404) {
        return NotFoundException('الطلب المطلوب غير موجود.');
      }
      if (statusCode == 500) {
        return ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
          statusCode: 500,
        );
      }

      return ServerException(message, statusCode: statusCode);
    }

    // anything else
    return AppException('حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.');
  }
}
