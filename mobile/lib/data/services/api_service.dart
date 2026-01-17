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

  // add token to requests
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
        // if we get 401 try to refresh the token
        if (error.response?.statusCode == 401) {
          // try refreshing token
          bool refreshed = await renewToken();
          if (refreshed) {
            // token refreshed just return the error and let user retry manually
          } else {
            // refresh failed logout user
            await cleanAuthData();
          }
        }
        return handler.next(error);
      },
    );
  }

  // refresh the token when it expires
  Future<bool> renewToken() async {
    try {
      final refreshToken = await StorageHelper.getRefreshToken();
      if (refreshToken == null || refreshToken.isEmpty) {
        return false;
      }

      final response = await dioClient.post(
        '/auth/refresh',
        data: refreshToken,
        options: Options(
          headers: {'Content-Type': 'text/plain'},
        ),
      );

      if (response.statusCode == 200 && response.data != null) {
        final data = response.data['data'];
        if (data != null && data['token'] != null) {
          _token = data['token'];
          await StorageHelper.saveToken(_token!);
          return true;
        }
      }
      return false;
    } catch (e) {
      debugPrint('error refreshing token: $e');
      return false;
    }
  }

  Future<void> cleanAuthData() async {
    _token = null;
    await StorageHelper.removeToken();
    await StorageHelper.removeUser();
    await StorageHelper.removeRefreshToken();
  }

  void updateToken(String? token) {
    _token = token;
  }

  Future<void> loadToken() async {
    _token = await StorageHelper.getToken();
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
