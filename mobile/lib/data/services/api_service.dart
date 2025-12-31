import 'package:dio/dio.dart';

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
    // 1. set up the dio client with our base url and timeout
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

    // 2. add the interceptor to send the token with every request
    dioClient.interceptors.add(makeAuthHelper());

    // 3. enable logging if we are in debug mode
    if (ApiConfig.enableLogging) {
      dioClient.interceptors.add(LogInterceptor(
        requestBody: true,
        responseBody: true,
        requestHeader: true,
        responseHeader: false,
        error: true,
      ));
    }
  }

  InterceptorsWrapper makeAuthHelper() {
    return InterceptorsWrapper(
      onRequest: (options, handler) async {
        if (_token != null && _token!.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $_token';
        }

        return handler.next(options);
      },
      onError: (error, handler) async {
        if (error.response?.statusCode == 401) {
          final refreshed = await renewToken();
          if (refreshed) {
            try {
              error.requestOptions.headers['Authorization'] = 'Bearer $_token';
              final retryResponse = await dioClient.fetch(error.requestOptions);
              return handler.resolve(retryResponse);
            } catch (retryError) {
              // ignore retry error if token refresh fails
            }
          }

          await cleanAuthData();
        }

        return handler.next(error);
      },
    );
  }

  bool _isRefreshing = false;

  Future<bool> renewToken() async {
    if (_isRefreshing) return false;

    _isRefreshing = true;
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
      return false;
    } finally {
      _isRefreshing = false;
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

  Future<Response> get(
    String endpoint, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      final response = await dioClient.get(
        endpoint,
        queryParameters: queryParameters,
      );
      return response;
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
      final response = await dioClient.post(
        endpoint,
        data: data,
        queryParameters: queryParameters,
      );
      return response;
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
      final response = await dioClient.put(
        endpoint,
        data: data,
        queryParameters: queryParameters,
      );
      return response;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> delete(
    String endpoint, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      final response = await dioClient.delete(
        endpoint,
        queryParameters: queryParameters,
      );
      return response;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Exception _handleError(DioException error) {
    // check what kind of error happened
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return NetworkException('انتهت مهلة الاتصال. يرجى المحاولة مرة أخرى.');

      case DioExceptionType.badResponse:
        // the server responded with an error code
        final statusCode = error.response?.statusCode;
        final message = error.response?.data?['message'] ??
            error.response?.data?['error'] ??
            'Server Error';

        if (statusCode == 401) {
          return UnauthorizedException('غير مصرح. يرجى تسجيل الدخول مرة أخرى.');
        } else if (statusCode == 403) {
          return UnauthorizedException('غير مصرح. يرجى تسجيل الدخول مرة أخرى.');
        } else if (statusCode == 404) {
          return NotFoundException('الطلب المطلوب غير موجود.');
        } else if (statusCode == 500) {
          return ServerException(
            'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
            statusCode: 500,
          );
        }

        return ServerException(message, statusCode: statusCode);

      case DioExceptionType.cancel:
        return AppException('تم إلغاء الطلب.');

      case DioExceptionType.connectionError:
        return NetworkException('لا يوجد اتصال بالإنترنت.');

      default:
        return AppException('حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.');
    }
  }
}
