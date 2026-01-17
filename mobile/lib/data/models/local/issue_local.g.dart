// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'issue_local.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class IssueLocalAdapter extends TypeAdapter<IssueLocal> {
  @override
  final int typeId = 2;

  @override
  IssueLocal read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return IssueLocal(
      clientId: fields[0] as String?,
      serverId: fields[1] as int?,
      title: fields[2] as String,
      description: fields[3] as String,
      type: fields[4] as String,
      severity: fields[5] as String,
      reportedByUserId: fields[6] as int,
      latitude: fields[7] as double,
      longitude: fields[8] as double,
      locationDescription: fields[9] as String?,
      photoUrl: fields[10] as String?,
      reportedAt: fields[11] as DateTime,
      isSynced: fields[12] as bool,
      createdAt: fields[13] as DateTime,
      syncedAt: fields[14] as DateTime?,
    );
  }

  @override
  void write(BinaryWriter writer, IssueLocal obj) {
    writer
      ..writeByte(15)
      ..writeByte(0)
      ..write(obj.clientId)
      ..writeByte(1)
      ..write(obj.serverId)
      ..writeByte(2)
      ..write(obj.title)
      ..writeByte(3)
      ..write(obj.description)
      ..writeByte(4)
      ..write(obj.type)
      ..writeByte(5)
      ..write(obj.severity)
      ..writeByte(6)
      ..write(obj.reportedByUserId)
      ..writeByte(7)
      ..write(obj.latitude)
      ..writeByte(8)
      ..write(obj.longitude)
      ..writeByte(9)
      ..write(obj.locationDescription)
      ..writeByte(10)
      ..write(obj.photoUrl)
      ..writeByte(11)
      ..write(obj.reportedAt)
      ..writeByte(12)
      ..write(obj.isSynced)
      ..writeByte(13)
      ..write(obj.createdAt)
      ..writeByte(14)
      ..write(obj.syncedAt);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is IssueLocalAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
