import 'package:flutter/foundation.dart';
import 'package:hive/hive.dart';
import 'package:uuid/uuid.dart';
import '../../models/local/issue_local.dart';

class IssueLocalRepository {
  static const String _boxName = 'issue_local';
  final _uuid = const Uuid();

  // open the hive box for issues
  Future<Box<IssueLocal>> _openBox() async {
    return await Hive.openBox<IssueLocal>(_boxName);
  }

  // save a new issue report on the phone when offline
  Future<String> addIssue(IssueLocal issue) async {
    final box = await _openBox();
    // generate a unique id
    issue.clientId = _uuid.v4();
    issue.createdAt = DateTime.now();
    // add to the local database
    await box.add(issue);
    return issue.clientId!;
  }

  // get all unsynced issues
  Future<List<IssueLocal>> getUnsyncedIssues() async {
    final box = await _openBox();
    return box.values.where((i) => !i.isSynced).toList();
  }

  // get issue by server ID
  Future<IssueLocal?> getIssueByServerId(int serverId) async {
    final box = await _openBox();
    try {
      return box.values.firstWhere((i) => i.serverId == serverId);
    } catch (e) {
      return null;
    }
  }

  // mark issue as synced
  Future<void> markAsSynced(String clientId, int serverId) async {
    final box = await _openBox();
    // handle errors to prevent crash if record not found
    try {
      final issue = box.values.firstWhere((i) => i.clientId == clientId);
      issue.serverId = serverId;
      issue.isSynced = true;
      issue.syncedAt = DateTime.now();
      await issue.save();
    } catch (e) {
      if (kDebugMode) debugPrint('Issue $clientId not found in local DB: $e');
    }
  }

  // get all issues for a user
  Future<List<IssueLocal>> getAllIssues(int userId) async {
    final box = await _openBox();
    return box.values.where((i) => i.reportedByUserId == userId).toList()
      ..sort((a, b) => b.reportedAt.compareTo(a.reportedAt));
  }

  // update issue from server (after download)
  Future<void> updateFromServer(IssueLocal issue) async {
    final box = await _openBox();
    final existingIndex =
        box.values.toList().indexWhere((i) => i.serverId == issue.serverId);

    if (existingIndex >= 0) {
      // update existing
      await box.putAt(existingIndex, issue);
    } else {
      // add new from server
      await box.add(issue);
    }
  }

  /// Merge server data with local data using field-level ownership.
  ///
  /// Field ownership (non-overlapping):
  /// - Worker fields (never overwritten by server unless fully synced):
  ///   title, description, type, severity, latitude, longitude,
  ///   locationDescription, photoUrl
  /// - Supervisor fields (always accepted from server):
  ///   status, forwardedToDepartmentId, forwardedToDepartmentName,
  ///   forwardedAt, forwardingNotes
  Future<void> mergeFromServer(IssueLocal serverIssue) async {
    final box = await _openBox();
    final existingIndex =
        box.values.toList().indexWhere((i) => i.serverId == serverIssue.serverId);

    if (existingIndex < 0) {
      // new issue from server - just add it
      await box.add(serverIssue);
      return;
    }

    final existing = box.getAt(existingIndex)!;

    if (existing.isSynced) {
      // no local changes - safe to overwrite entirely
      await box.putAt(existingIndex, serverIssue);
      return;
    }

    // FIELD-LEVEL MERGE: local has unsynced worker changes
    // Accept supervisor-owned fields from server
    existing.status = serverIssue.status;
    existing.forwardedToDepartmentId = serverIssue.forwardedToDepartmentId;
    existing.forwardedToDepartmentName = serverIssue.forwardedToDepartmentName;
    existing.forwardedAt = serverIssue.forwardedAt;
    existing.forwardingNotes = serverIssue.forwardingNotes;

    // Worker fields (title, description, type, severity, latitude,
    // longitude, locationDescription, photoUrl) stay as-is

    // Update version so next upload sends correct syncVersion
    existing.syncVersion = serverIssue.syncVersion;
    // Keep isSynced = false - worker changes still need uploading
    await existing.save();
  }

  // clear all data (for logout)
  Future<void> clearAll() async {
    final box = await _openBox();
    await box.clear();
  }
}
