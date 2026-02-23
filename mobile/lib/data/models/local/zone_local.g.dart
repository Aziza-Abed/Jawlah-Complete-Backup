// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'zone_local.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class ZoneLocalAdapter extends TypeAdapter<ZoneLocal> {
  @override
  final int typeId = 5;

  @override
  ZoneLocal read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return ZoneLocal(
      zoneId: fields[0] as int,
      zoneName: fields[1] as String,
      zoneCode: fields[2] as String,
      description: fields[3] as String?,
      centerLatitude: fields[4] as double,
      centerLongitude: fields[5] as double,
      areaSquareMeters: fields[6] as double,
      district: fields[7] as String?,
      version: fields[8] as int,
      isActive: fields[9] as bool,
      boundaryGeoJson: fields[10] as String?,
      syncedAt: fields[11] as DateTime,
    );
  }

  @override
  void write(BinaryWriter writer, ZoneLocal obj) {
    writer
      ..writeByte(12)
      ..writeByte(0)
      ..write(obj.zoneId)
      ..writeByte(1)
      ..write(obj.zoneName)
      ..writeByte(2)
      ..write(obj.zoneCode)
      ..writeByte(3)
      ..write(obj.description)
      ..writeByte(4)
      ..write(obj.centerLatitude)
      ..writeByte(5)
      ..write(obj.centerLongitude)
      ..writeByte(6)
      ..write(obj.areaSquareMeters)
      ..writeByte(7)
      ..write(obj.district)
      ..writeByte(8)
      ..write(obj.version)
      ..writeByte(9)
      ..write(obj.isActive)
      ..writeByte(10)
      ..write(obj.boundaryGeoJson)
      ..writeByte(11)
      ..write(obj.syncedAt);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ZoneLocalAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
