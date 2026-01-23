// Basic Flutter widget test for FollowUp app

import 'package:flutter_test/flutter_test.dart';
import 'package:followup/main.dart';

void main() {
  testWidgets('App launches without crashing', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const FollowUpApp());

    // Verify app renders (basic smoke test)
    expect(find.byType(FollowUpApp), findsOneWidget);
  });
}
