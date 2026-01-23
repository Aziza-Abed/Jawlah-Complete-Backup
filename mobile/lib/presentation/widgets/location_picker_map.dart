import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import '../../core/theme/app_colors.dart';
import '../../data/services/location_service.dart';

/// A widget that displays a map and allows users to pick or confirm a location
/// Uses OpenStreetMap (free, no API key required)
/// Used for attendance check-in/out, task completion, and issue reporting
class LocationPickerMap extends StatefulWidget {
  /// Initial position for the map (defaults to current location if null)
  final Position? initialPosition;

  /// Title to display in the app bar
  final String title;

  /// Description text to show above the map
  final String description;

  /// Whether to allow tapping the map to adjust location
  final bool allowDragging;

  const LocationPickerMap({
    super.key,
    this.initialPosition,
    this.title = 'تأكيد الموقع',
    this.description = 'انقر على الخريطة لتعديل الموقع أو انقر على "تأكيد" للمتابعة',
    this.allowDragging = true,
  });

  @override
  State<LocationPickerMap> createState() => _LocationPickerMapState();
}

class _LocationPickerMapState extends State<LocationPickerMap> {
  late final MapController _mapController;
  LatLng? _selectedPosition;
  Position? _currentPosition;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _mapController = MapController();
    _initializeLocation();
  }

  Future<void> _initializeLocation() async {
    try {
      setState(() {
        _isLoading = true;
        _error = null;
      });

      Position? position;
      if (widget.initialPosition != null) {
        position = widget.initialPosition;
      } else {
        position = await LocationService.getCurrentLocation();
      }

      if (position == null) {
        setState(() {
          _error = 'فشل الحصول على الموقع الحالي. يرجى التحقق من إعدادات GPS.';
          _isLoading = false;
        });
        return;
      }

      setState(() {
        _currentPosition = position;
        _selectedPosition = LatLng(position!.latitude, position.longitude);
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = 'حدث خطأ في تحميل الموقع: $e';
        _isLoading = false;
      });
    }
  }

  void _onMapTap(TapPosition tapPosition, LatLng position) {
    if (widget.allowDragging) {
      setState(() {
        _selectedPosition = position;
      });
    }
  }

  void _onConfirm() {
    if (_selectedPosition != null && _currentPosition != null) {
      // Return the selected position as a Position object
      final result = Position(
        latitude: _selectedPosition!.latitude,
        longitude: _selectedPosition!.longitude,
        timestamp: DateTime.now(),
        accuracy: _currentPosition!.accuracy,
        altitude: _currentPosition!.altitude,
        altitudeAccuracy: _currentPosition!.altitudeAccuracy,
        heading: _currentPosition!.heading,
        headingAccuracy: _currentPosition!.headingAccuracy,
        speed: _currentPosition!.speed,
        speedAccuracy: _currentPosition!.speedAccuracy,
      );
      Navigator.of(context).pop(result);
    }
  }

  Future<void> _resetToCurrentLocation() async {
    final position = await LocationService.getCurrentLocation();
    if (position != null) {
      final newPosition = LatLng(position.latitude, position.longitude);
      setState(() {
        _currentPosition = position;
        _selectedPosition = newPosition;
      });
      _mapController.move(newPosition, 17.0);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          widget.title,
          style: const TextStyle(
            fontFamily: 'Cairo',
            fontWeight: FontWeight.bold,
          ),
        ),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      body: _isLoading
          ? const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  CircularProgressIndicator(color: AppColors.primary),
                  SizedBox(height: 16),
                  Text(
                    'جاري تحميل الموقع...',
                    style: TextStyle(
                      fontFamily: 'Cairo',
                      fontSize: 16,
                    ),
                  ),
                ],
              ),
            )
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(
                          Icons.location_off,
                          size: 64,
                          color: AppColors.error,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          _error!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            fontFamily: 'Cairo',
                            fontSize: 16,
                          ),
                        ),
                        const SizedBox(height: 24),
                        ElevatedButton.icon(
                          onPressed: _initializeLocation,
                          icon: const Icon(Icons.refresh),
                          label: const Text(
                            'إعادة المحاولة',
                            style: TextStyle(fontFamily: 'Cairo'),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            foregroundColor: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ),
                )
              : Column(
                  children: [
                    // Description banner
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(16),
                      color: AppColors.info.withOpacity(0.1),
                      child: Row(
                        children: [
                          const Icon(
                            Icons.info_outline,
                            color: AppColors.info,
                            size: 24,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              widget.description,
                              style: const TextStyle(
                                fontFamily: 'Cairo',
                                fontSize: 14,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),

                    // Map
                    Expanded(
                      child: Stack(
                        children: [
                          FlutterMap(
                            mapController: _mapController,
                            options: MapOptions(
                              initialCenter: _selectedPosition!,
                              initialZoom: 17.0,
                              minZoom: 5.0,
                              maxZoom: 18.0,
                              onTap: _onMapTap,
                              interactionOptions: const InteractionOptions(
                                flags: InteractiveFlag.all,
                              ),
                            ),
                            children: [
                              // OpenStreetMap tiles
                              TileLayer(
                                urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                                userAgentPackageName: 'com.example.followup',
                                maxNativeZoom: 19,
                              ),

                              // Selected location marker
                              MarkerLayer(
                                markers: [
                                  Marker(
                                    point: _selectedPosition!,
                                    width: 50,
                                    height: 50,
                                    child: const Icon(
                                      Icons.location_on,
                                      color: Colors.green,
                                      size: 50,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),

                          // Reset to current location button
                          Positioned(
                            top: 16,
                            left: 16,
                            child: FloatingActionButton.small(
                              onPressed: _resetToCurrentLocation,
                              backgroundColor: Colors.white,
                              foregroundColor: AppColors.primary,
                              child: const Icon(Icons.my_location),
                            ),
                          ),

                          // Zoom controls
                          Positioned(
                            right: 16,
                            top: 16,
                            child: Column(
                              children: [
                                FloatingActionButton.small(
                                  heroTag: 'zoom_in_picker',
                                  backgroundColor: Colors.white,
                                  onPressed: () {
                                    final currentZoom = _mapController.camera.zoom;
                                    _mapController.move(
                                      _mapController.camera.center,
                                      currentZoom + 1,
                                    );
                                  },
                                  child: const Icon(Icons.add, color: AppColors.primary),
                                ),
                                const SizedBox(height: 8),
                                FloatingActionButton.small(
                                  heroTag: 'zoom_out_picker',
                                  backgroundColor: Colors.white,
                                  onPressed: () {
                                    final currentZoom = _mapController.camera.zoom;
                                    _mapController.move(
                                      _mapController.camera.center,
                                      currentZoom - 1,
                                    );
                                  },
                                  child: const Icon(Icons.remove, color: AppColors.primary),
                                ),
                              ],
                            ),
                          ),

                          // Coordinates display
                          Positioned(
                            bottom: 16,
                            left: 16,
                            right: 16,
                            child: Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(8),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black.withOpacity(0.1),
                                    blurRadius: 8,
                                    offset: const Offset(0, 2),
                                  ),
                                ],
                              ),
                              child: Column(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      const Text(
                                        'خط العرض:',
                                        style: TextStyle(
                                          fontFamily: 'Cairo',
                                          fontSize: 12,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                      Text(
                                        _selectedPosition!.latitude
                                            .toStringAsFixed(6),
                                        style: const TextStyle(
                                          fontFamily: 'Cairo',
                                          fontSize: 12,
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 4),
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      const Text(
                                        'خط الطول:',
                                        style: TextStyle(
                                          fontFamily: 'Cairo',
                                          fontSize: 12,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                      Text(
                                        _selectedPosition!.longitude
                                            .toStringAsFixed(6),
                                        style: const TextStyle(
                                          fontFamily: 'Cairo',
                                          fontSize: 12,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),

                    // Action buttons
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.05),
                            blurRadius: 10,
                            offset: const Offset(0, -2),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: OutlinedButton(
                              onPressed: () => Navigator.of(context).pop(),
                              style: OutlinedButton.styleFrom(
                                padding:
                                    const EdgeInsets.symmetric(vertical: 14),
                                side: const BorderSide(
                                    color: AppColors.textSecondary),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(8),
                                ),
                              ),
                              child: const Text(
                                'إلغاء',
                                style: TextStyle(
                                  fontFamily: 'Cairo',
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            flex: 2,
                            child: ElevatedButton.icon(
                              onPressed: _onConfirm,
                              icon: const Icon(Icons.check_circle_outline),
                              label: const Text(
                                'تأكيد الموقع',
                                style: TextStyle(
                                  fontFamily: 'Cairo',
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: AppColors.success,
                                foregroundColor: Colors.white,
                                padding:
                                    const EdgeInsets.symmetric(vertical: 14),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(8),
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
    );
  }

  @override
  void dispose() {
    _mapController.dispose();
    super.dispose();
  }
}
