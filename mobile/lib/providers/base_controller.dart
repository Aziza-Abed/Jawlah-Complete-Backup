import 'package:flutter/foundation.dart';
import '../core/errors/app_exception.dart';

// base class for all providers with common loading/error handling
abstract class BaseController with ChangeNotifier {
  bool _isLoading = false;
  String? _errorMessage;

  // loading state
  bool get isLoading => _isLoading;

  // error message
  String? get errorMessage => _errorMessage;

  // set loading state
  @protected
  void setLoading(bool value) {
    if (_isLoading != value) {
      _isLoading = value;
      notifyListeners();
    }
  }

  // set error message
  @protected
  void setError(String? message) {
    _errorMessage = message;
    notifyListeners();
  }

  // clear error
  @protected
  void clearError() {
    if (_errorMessage != null) {
      _errorMessage = null;
      notifyListeners();
    }
  }

  // execute async operation with loading and error handling
  @protected
  Future<T?> executeWithErrorHandling<T>(Future<T> Function() action) async {
    try {
      setLoading(true);
      clearError();
      final result = await action();
      return result;
    } on NetworkException catch (e) {
      setError(e.message);
      return null;
    } on ServerException catch (e) {
      setError(e.message);
      return null;
    } on ValidationException catch (e) {
      setError(e.message);
      return null;
    } on UnauthorizedException catch (e) {
      setError(e.message);
      return null;
    } on AppException catch (e) {
      setError(e.toString());
      return null;
    } catch (e) {
      setError(e.toString().replaceAll('Exception: ', ''));
      return null;
    } finally {
      setLoading(false);
    }
  }

  // execute void operation (like delete, update)
  @protected
  Future<bool> executeVoidWithErrorHandling(
      Future<void> Function() action) async {
    try {
      setLoading(true);
      clearError();
      await action();
      return true;
    } on NetworkException catch (e) {
      setError(e.message);
      return false;
    } on ServerException catch (e) {
      setError(e.message);
      return false;
    } on ValidationException catch (e) {
      setError(e.message);
      return false;
    } on UnauthorizedException catch (e) {
      setError(e.message);
      return false;
    } on AppException catch (e) {
      setError(e.toString());
      return false;
    } catch (e) {
      setError(e.toString().replaceAll('Exception: ', ''));
      return false;
    } finally {
      setLoading(false);
    }
  }
}
