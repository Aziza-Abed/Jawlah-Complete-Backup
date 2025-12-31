// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'task_local.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class TaskLocalAdapter extends TypeAdapter<TaskLocal> {
  @override
  final int typeId = 1;

  @override
  TaskLocal read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return TaskLocal(
      clientId: fields[0] as String?,
      taskId: fields[1] as int,
      title: fields[2] as String?,
      status: fields[3] as String,
      completionNotes: fields[4] as String?,
      photoUrl: fields[5] as String?,
      completedAt: fields[6] as DateTime?,
      syncVersion: fields[7] as int,
      isSynced: fields[8] as bool,
      updatedAt: fields[9] as DateTime,
      syncedAt: fields[10] as DateTime?,
      description: fields[11] as String?,
      priority: fields[12] as String,
      dueDate: fields[13] as DateTime?,
      zoneId: fields[14] as int?,
      latitude: fields[15] as double?,
      longitude: fields[16] as double?,
      locationDescription: fields[17] as String?,
      taskType: fields[18] as String?,
      requiresPhotoProof: fields[19] as bool,
      estimatedDurationMinutes: fields[20] as int?,
    );
  }

  @override
  void write(BinaryWriter writer, TaskLocal obj) {
    writer
      ..writeByte(21)
      ..writeByte(0)
      ..write(obj.clientId)
      ..writeByte(1)
      ..write(obj.taskId)
      ..writeByte(2)
      ..write(obj.title)
      ..writeByte(3)
      ..write(obj.status)
      ..writeByte(4)
      ..write(obj.completionNotes)
      ..writeByte(5)
      ..write(obj.photoUrl)
      ..writeByte(6)
      ..write(obj.completedAt)
      ..writeByte(7)
      ..write(obj.syncVersion)
      ..writeByte(8)
      ..write(obj.isSynced)
      ..writeByte(9)
      ..write(obj.updatedAt)
      ..writeByte(10)
      ..write(obj.syncedAt)
      ..writeByte(11)
      ..write(obj.description)
      ..writeByte(12)
      ..write(obj.priority)
      ..writeByte(13)
      ..write(obj.dueDate)
      ..writeByte(14)
      ..write(obj.zoneId)
      ..writeByte(15)
      ..write(obj.latitude)
      ..writeByte(16)
      ..write(obj.longitude)
      ..writeByte(17)
      ..write(obj.locationDescription)
      ..writeByte(18)
      ..write(obj.taskType)
      ..writeByte(19)
      ..write(obj.requiresPhotoProof)
      ..writeByte(20)
      ..write(obj.estimatedDurationMinutes);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is TaskLocalAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
