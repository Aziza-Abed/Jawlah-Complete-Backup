import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:intl/intl.dart';
import '../../core/theme/app_colors.dart';
import '../../data/models/worker_location.dart';
import '../../data/services/location_history_service.dart';

class LocationHistoryScreen extends StatefulWidget {
  final int userId;
  final String workerName;

  const LocationHistoryScreen({
    super.key,
    required this.userId,
    this.workerName = 'Worker',
  });

  @override
  State<LocationHistoryScreen> createState() => _LocationHistoryScreenState();
}

class _LocationHistoryScreenState extends State<LocationHistoryScreen> {
  final MapController _mapController = MapController();
  final LocationHistoryService _historyService = LocationHistoryService();

  List<WorkerLocation> _allLocations = [];
  List<WorkerLocation> _visibleLocations = [];
  DateTime _selectedDate = DateTime.now();
  bool _isLoading = false;
  String? _error;

  bool _isPlaying = false;
  int _currentIndex = 0;
  Timer? _playbackTimer;
  double _playbackSpeed = 1.0;

  static const LatLng _centerPalestine = LatLng(31.9066, 35.2034);

  @override
  void initState() {
    super.initState();
    _loadHistory();
  }

  @override
  void dispose() {
    _playbackTimer?.cancel();
    _mapController.dispose();
    super.dispose();
  }

  Future<void> _loadHistory() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final history = await _historyService.getHistory(
        userId: widget.userId,
        date: _selectedDate,
      );

      setState(() {
        _allLocations = history;
        _visibleLocations = history;
        _currentIndex = 0;
        _isLoading = false;

        // center map on first location if available
        if (history.isNotEmpty) {
          _mapController.move(
            LatLng(history.first.latitude, history.first.longitude),
            14.0,
          );
        }
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  void _togglePlayback() {
    if (_isPlaying) {
      _pausePlayback();
    } else {
      _startPlayback();
    }
  }

  void _startPlayback() {
    if (_allLocations.isEmpty) return;

    setState(() {
      _isPlaying = true;
    });

    _playbackTimer = Timer.periodic(
      Duration(milliseconds: (1000 / _playbackSpeed).round()),
      (timer) {
        if (_currentIndex >= _allLocations.length - 1) {
          _pausePlayback();
          return;
        }

        setState(() {
          _currentIndex++;
          _visibleLocations = _allLocations.sublist(0, _currentIndex + 1);

          // center map on current location
          final currentLocation = _allLocations[_currentIndex];
          _mapController.move(
            LatLng(currentLocation.latitude, currentLocation.longitude),
            15.0,
          );
        });
      },
    );
  }

  void _pausePlayback() {
    _playbackTimer?.cancel();
    setState(() {
      _isPlaying = false;
    });
  }

  void _resetPlayback() {
    _pausePlayback();
    setState(() {
      _currentIndex = 0;
      _visibleLocations = _allLocations.isEmpty ? [] : [_allLocations.first];

      if (_allLocations.isNotEmpty) {
        _mapController.move(
          LatLng(_allLocations.first.latitude, _allLocations.first.longitude),
          14.0,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('مسار ${widget.workerName}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.calendar_today),
            onPressed: _selectDate,
            tooltip: 'اختيار التاريخ',
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildErrorWidget()
              : _allLocations.isEmpty
                  ? _buildEmptyWidget()
                  : Stack(
                      children: [
                        // map
                        _buildMap(),

                        // stats card
                        Positioned(
                          top: 16,
                          left: 16,
                          right: 16,
                          child: _buildStatsCard(),
                        ),

                        // playback controls
                        Positioned(
                          bottom: 0,
                          left: 0,
                          right: 0,
                          child: _buildPlaybackControls(),
                        ),
                      ],
                    ),
    );
  }

  Widget _buildMap() {
    final points = _visibleLocations
        .map((loc) => LatLng(loc.latitude, loc.longitude))
        .toList();

    return FlutterMap(
      mapController: _mapController,
      options: MapOptions(
        initialCenter: _allLocations.isNotEmpty
            ? LatLng(
                _allLocations.first.latitude, _allLocations.first.longitude)
            : _centerPalestine,
        initialZoom: 14.0,
        minZoom: 10.0,
        maxZoom: 18.0,
      ),
      children: [
        // openStreetMap tile layer
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.albireh.jawlah',
        ),

        // breadcrumb trail (polyline)
        if (points.length > 1)
          PolylineLayer(
            polylines: [
              Polyline(
                points: points,
                strokeWidth: 4.0,
                color: AppColors.primary,
                borderStrokeWidth: 2.0,
                borderColor: Colors.white,
              ),
            ],
          ),

        // markers
        MarkerLayer(
          markers: [
            // start marker (green)
            if (_allLocations.isNotEmpty)
              Marker(
                point: LatLng(_allLocations.first.latitude,
                    _allLocations.first.longitude),
                width: 40,
                height: 40,
                child: const Icon(
                  Icons.play_circle,
                  size: 40,
                  color: Colors.green,
                ),
              ),

            // end marker (red) - only show if playback completed
            if (_currentIndex >= _allLocations.length - 1 &&
                _allLocations.isNotEmpty)
              Marker(
                point: LatLng(_allLocations.last.latitude,
                    _allLocations.last.longitude),
                width: 40,
                height: 40,
                child: const Icon(
                  Icons.stop_circle,
                  size: 40,
                  color: Colors.red,
                ),
              ),

            // current position marker (blue)
            if (_currentIndex > 0 && _currentIndex < _allLocations.length)
              Marker(
                point: LatLng(_allLocations[_currentIndex].latitude,
                    _allLocations[_currentIndex].longitude),
                width: 50,
                height: 50,
                child: Container(
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.white, width: 3),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.3),
                        blurRadius: 8,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  child: const Icon(
                    Icons.navigation,
                    color: Colors.white,
                    size: 24,
                  ),
                ),
              ),
          ],
        ),
      ],
    );
  }

  Widget _buildStatsCard() {
    final distance = _historyService.calculateTotalDistance(_visibleLocations);
    final duration = _allLocations.isNotEmpty &&
            _allLocations.first.lastUpdate != null &&
            _allLocations.last.lastUpdate != null
        ? _allLocations.last.lastUpdate!
            .difference(_allLocations.first.lastUpdate!)
        : null;

    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              DateFormat('EEEE, d MMMM yyyy', 'ar').format(_selectedDate),
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildStatItem(
                  Icons.location_on,
                  'نقاط',
                  '${_visibleLocations.length}',
                ),
                _buildStatItem(
                  Icons.route,
                  'المسافة',
                  '${(distance / 1000).toStringAsFixed(2)} كم',
                ),
                if (duration != null)
                  _buildStatItem(
                    Icons.access_time,
                    'المدة',
                    '${duration.inHours}س ${duration.inMinutes % 60}د',
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatItem(IconData icon, String label, String value) {
    return Column(
      children: [
        Icon(icon, color: AppColors.primary, size: 24),
        const SizedBox(height: 4),
        Text(
          label,
          style: TextStyle(fontSize: 12, color: Colors.grey[600]),
        ),
        Text(
          value,
          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
        ),
      ],
    );
  }

  Widget _buildPlaybackControls() {
    return Card(
      margin: EdgeInsets.zero,
      elevation: 8,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // time slider
            if (_allLocations.isNotEmpty)
              Row(
                children: [
                  Text(
                    _allLocations.first.lastUpdate != null
                        ? DateFormat('HH:mm')
                            .format(_allLocations.first.lastUpdate!)
                        : '--:--',
                    style: const TextStyle(fontSize: 12),
                  ),
                  Expanded(
                    child: Slider(
                      value: _currentIndex.toDouble(),
                      min: 0,
                      max: _allLocations.length > 1
                          ? (_allLocations.length - 1).toDouble()
                          : 1,
                      divisions: _allLocations.length > 1
                          ? _allLocations.length - 1
                          : 1,
                      onChanged: (value) {
                        setState(() {
                          _currentIndex = value.toInt();
                          _visibleLocations =
                              _allLocations.sublist(0, _currentIndex + 1);
                        });
                      },
                    ),
                  ),
                  Text(
                    _allLocations.last.lastUpdate != null
                        ? DateFormat('HH:mm')
                            .format(_allLocations.last.lastUpdate!)
                        : '--:--',
                    style: const TextStyle(fontSize: 12),
                  ),
                ],
              ),

            const SizedBox(height: 8),

            // control buttons
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                // reset button
                IconButton(
                  icon: const Icon(Icons.replay),
                  onPressed: _resetPlayback,
                  tooltip: 'إعادة',
                ),

                // play/Pause button
                FloatingActionButton(
                  onPressed: _togglePlayback,
                  child: Icon(_isPlaying ? Icons.pause : Icons.play_arrow),
                ),

                // speed control
                DropdownButton<double>(
                  value: _playbackSpeed,
                  items: const [
                    DropdownMenuItem(value: 0.5, child: Text('0.5x')),
                    DropdownMenuItem(value: 1.0, child: Text('1x')),
                    DropdownMenuItem(value: 2.0, child: Text('2x')),
                    DropdownMenuItem(value: 5.0, child: Text('5x')),
                  ],
                  onChanged: (value) {
                    setState(() {
                      _playbackSpeed = value!;
                      if (_isPlaying) {
                        _pausePlayback();
                        _startPlayback();
                      }
                    });
                  },
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildErrorWidget() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline, size: 64, color: Colors.red),
          const SizedBox(height: 16),
          Text('حدث خطأ: $_error'),
          const SizedBox(height: 16),
          ElevatedButton(
            onPressed: _loadHistory,
            child: const Text('إعادة المحاولة'),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyWidget() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.location_off, size: 64, color: Colors.grey),
          const SizedBox(height: 16),
          Text(
            'لا توجد بيانات موقع لهذا اليوم',
            style: TextStyle(fontSize: 18, color: Colors.grey[600]),
          ),
          const SizedBox(height: 16),
          ElevatedButton.icon(
            onPressed: _selectDate,
            icon: const Icon(Icons.calendar_today),
            label: const Text('اختيار يوم آخر'),
          ),
        ],
      ),
    );
  }

  Future<void> _selectDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now(),
      locale: const Locale('ar'),
    );

    if (picked != null && picked != _selectedDate) {
      setState(() {
        _selectedDate = picked;
      });
      _loadHistory();
    }
  }
}
