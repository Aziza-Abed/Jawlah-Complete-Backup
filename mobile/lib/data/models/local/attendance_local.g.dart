// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'attendance_local.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class AttendanceLocalAdapter extends TypeAdapter<AttendanceLocal> {
  @override
  final int typeId = 0;

  @override
  AttendanceLocal read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return AttendanceLocal(
      clientId: fields[0] as String?,
      serverId: fields[1] as int?,
      userId: fields[2] as int,
      checkInTime: fields[3] as DateTime,
      checkOutTime: fields[4] as DateTime?,
      checkInLatitude: fields[5] as double,
      checkInLongitude: fields[6] as double,
      checkOutLatitude: fields[7] as double?,
      checkOutLongitude: fields[8] as double?,
      isValidated: fields[9] as bool,
      validationMessage: fields[10] as String?,
      isSynced: fields[11] as bool,
      createdAt: fields[12] as DateTime,
      syncedAt: fields[13] as DateTime?,
    );
  }

  @override
  void write(BinaryWriter writer, AttendanceLocal obj) {
    writer
      ..writeByte(14)
      ..writeByte(0)
      ..write(obj.clientId)
      ..writeByte(1)
      ..write(obj.serverId)
      ..writeByte(2)
      ..write(obj.userId)
      ..writeByte(3)
      ..write(obj.checkInTime)
      ..writeByte(4)
      ..write(obj.checkOutTime)
      ..writeByte(5)
      ..write(obj.checkInLatitude)
      ..writeByte(6)
      ..write(obj.checkInLongitude)
      ..writeByte(7)
      ..write(obj.checkOutLatitude)
      ..writeByte(8)
      ..write(obj.checkOutLongitude)
      ..writeByte(9)
      ..write(obj.isValidated)
      ..writeByte(10)
      ..write(obj.validationMessage)
      ..writeByte(11)
      ..write(obj.isSynced)
      ..writeByte(12)
      ..write(obj.createdAt)
      ..writeByte(13)
      ..write(obj.syncedAt);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is AttendanceLocalAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
