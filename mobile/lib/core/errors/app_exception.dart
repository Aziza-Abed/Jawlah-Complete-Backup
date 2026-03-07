// custom exceptions for better error handling
class AppException implements Exception {
  final String message;
  final String? code;
  final dynamic details;

  AppException(this.message, {this.code, this.details});

  @override
  String toString() => message;
}

// thrown when there's no internet connection or network problems
class NetworkException extends AppException {
  NetworkException([super.message = 'Network Connection Error'])
      : super(code: 'NETWORK_ERROR');
}

// thrown when the server returns an error (4xx or 5xx status codes)
class ServerException extends AppException {
  final int? statusCode;

  ServerException(super.message, {this.statusCode})
      : super(code: 'SERVER_ERROR');
}

// thrown when user is not logged in or doesn't have permission
class UnauthorizedException extends AppException {
  UnauthorizedException([super.message = 'Unauthorized Access'])
      : super(code: 'UNAUTHORIZED');
}

// thrown when requested resource doesn't exist
class NotFoundException extends AppException {
  NotFoundException([super.message = 'Resource Not Found'])
      : super(code: 'NOT_FOUND');
}

// thrown when user input is invalid
class ValidationException extends AppException {
  ValidationException(super.message) : super(code: 'VALIDATION_ERROR');
}