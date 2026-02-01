import 'package:flutter/foundation.dart';
import '../core/errors/app_exception.dart';

// base class that all providers extend from
abstract class BaseController with ChangeNotifier {
  bool _isLoading = false;
  String? _errorMessage;

  // is it loading or not
  bool get isLoading => _isLoading;

  // error message if there is one
  String? get errorMessage => _errorMessage;

  // check if there is an error
  bool get hasError => _errorMessage != null;

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

  // run async with loading and error handeling
  @protected
  Future<T?> executeWithErrorHandling<T>(Future<T> Function() action) async {
    try {
      setLoading(true);
      clearError();
      return await action();
    } on AppException catch (e) {
      setError(e.message);
      return null;
    } catch (e) {
      setError(e.toString().replaceAll('Exception: ', ''));
      return null;
    } finally {
      setLoading(false);
    }
  }

  // run void operation like delete or update
  @protected
  Future<bool> executeVoidWithErrorHandling(
      Future<void> Function() action) async {
    try {
      setLoading(true);
      clearError();
      await action();
      return true;
    } on AppException catch (e) {
      setError(e.message);
      return false;
    } catch (e) {
      setError(e.toString().replaceAll('Exception: ', ''));
      return false;
    } finally {
      setLoading(false);
    }
  }
}
