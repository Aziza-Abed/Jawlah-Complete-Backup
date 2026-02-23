-- ============================================================
-- Post-Migration Verification Queries
-- Run these AFTER applying migration MigrateStatusEnumValues
-- ============================================================

-- ── 1. Task Status Distribution ──
-- Expected: only values 0 (Pending), 1 (InProgress), 2 (UnderReview), 3 (Completed), 4 (Rejected)
PRINT '=== Task Status Distribution ===';
SELECT
    [Status],
    CASE [Status]
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'UnderReview'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Rejected'
        ELSE '*** INVALID ***'
    END AS StatusName,
    COUNT(*) AS TaskCount
FROM [Tasks]
GROUP BY [Status]
ORDER BY [Status];

-- ── 2. Issue Status Distribution ──
-- Expected: only values 0 (New), 1 (Forwarded), 2 (Resolved)
PRINT '=== Issue Status Distribution ===';
SELECT
    [Status],
    CASE [Status]
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Forwarded'
        WHEN 2 THEN 'Resolved'
        ELSE '*** INVALID ***'
    END AS StatusName,
    COUNT(*) AS IssueCount
FROM [Issues]
GROUP BY [Status]
ORDER BY [Status];

-- ── 3. Orphan / Invalid Task Status Check ──
-- Should return 0 rows
PRINT '=== Invalid Task Status Values (should be empty) ===';
SELECT TaskId, Title, [Status]
FROM [Tasks]
WHERE [Status] NOT IN (0, 1, 2, 3, 4);

-- ── 4. Orphan / Invalid Issue Status Check ──
-- Should return 0 rows
PRINT '=== Invalid Issue Status Values (should be empty) ===';
SELECT IssueId, Title, [Status]
FROM [Issues]
WHERE [Status] NOT IN (0, 1, 2);

-- ── 5. Summary Pass/Fail ──
PRINT '=== Migration Validation Summary ===';
SELECT
    CASE
        WHEN (SELECT COUNT(*) FROM [Tasks] WHERE [Status] NOT IN (0,1,2,3,4)) = 0
         AND (SELECT COUNT(*) FROM [Issues] WHERE [Status] NOT IN (0,1,2)) = 0
        THEN 'PASS - All status values are valid'
        ELSE 'FAIL - Invalid status values found (see queries above)'
    END AS ValidationResult;
