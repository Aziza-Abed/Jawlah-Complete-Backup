using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrateStatusEnumValues : Migration
    {
        /// <summary>
        /// Data-only migration: remap Task and Issue status integer values
        /// to match the updated C# enum definitions. No schema changes.
        ///
        /// Database confirmation:
        ///   Table [Tasks]  — Column [Status] — Type: int — Conversion: HasConversion&lt;int&gt;()
        ///   Table [Issues] — Column [Status] — Type: int — Conversion: HasConversion&lt;int&gt;()
        ///
        /// Task Status mapping (old → new):
        ///   0 Pending    → 0 Pending      (unchanged)
        ///   1 InProgress → 1 InProgress   (unchanged)
        ///   2 Completed  → 2 UnderReview  (same int value, semantic rename only)
        ///   3 Cancelled  → 4 Rejected     (remapped — merged into Rejected)
        ///   4 Approved   → 3 Completed    (remapped)
        ///   5 Rejected   → 4 Rejected     (remapped — renumbered)
        ///
        /// Issue Status mapping (old → new):
        ///   1 Reported    → 0 New        (remapped)
        ///   2 UnderReview → 1 Forwarded  (remapped)
        ///   3 Resolved    → 2 Resolved   (remapped — renumbered)
        ///   4 Dismissed   → 2 Resolved   (remapped — merged into Resolved)
        ///
        /// NOTE: This migration is lossy for two merges:
        ///   - Task: Cancelled(3) + Rejected(5) both become Rejected(4)
        ///   - Issue: Resolved(3) + Dismissed(4) both become Resolved(2)
        ///   The Down() method cannot distinguish the merged values and will
        ///   reverse them to the more common original value (Rejected=5, Resolved=3).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Task Status remapping ──
            // CASE ensures all old values are read before any writes,
            // avoiding ordering collisions (e.g., 4→3 then 3→4 would clash
            // without CASE).
            migrationBuilder.Sql(@"
                UPDATE [Tasks]
                SET [Status] = CASE
                    WHEN [Status] = 4 THEN 3   -- Approved   → Completed
                    WHEN [Status] = 5 THEN 4   -- Rejected   → Rejected (renumbered)
                    WHEN [Status] = 3 THEN 4   -- Cancelled  → Rejected (merged)
                    ELSE [Status]              -- 0, 1, 2 unchanged
                END
                WHERE [Status] IN (3, 4, 5);
            ");

            // ── Issue Status remapping ──
            migrationBuilder.Sql(@"
                UPDATE [Issues]
                SET [Status] = CASE
                    WHEN [Status] = 1 THEN 0   -- Reported    → New
                    WHEN [Status] = 2 THEN 1   -- UnderReview → Forwarded
                    WHEN [Status] = 3 THEN 2   -- Resolved    → Resolved (renumbered)
                    WHEN [Status] = 4 THEN 2   -- Dismissed   → Resolved (merged)
                    ELSE [Status]
                END
                WHERE [Status] IN (1, 2, 3, 4);
            ");
        }

        /// <inheritdoc />
        /// <remarks>
        /// LOSSY REVERSE: Cancelled/Rejected merge and Dismissed/Resolved merge
        /// cannot be fully reversed. Down() maps:
        ///   Task Rejected(4) → old Rejected(5)  [old Cancelled rows become Rejected]
        ///   Issue Resolved(2) → old Resolved(3)  [old Dismissed rows become Resolved]
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse Task status mapping (lossy: old Cancelled=3 unrecoverable)
            migrationBuilder.Sql(@"
                UPDATE [Tasks]
                SET [Status] = CASE
                    WHEN [Status] = 3 THEN 4   -- Completed → Approved
                    WHEN [Status] = 4 THEN 5   -- Rejected  → Rejected (old numbering)
                    ELSE [Status]              -- 0, 1, 2 unchanged
                END
                WHERE [Status] IN (3, 4);
            ");

            // Reverse Issue status mapping (lossy: old Dismissed=4 unrecoverable)
            migrationBuilder.Sql(@"
                UPDATE [Issues]
                SET [Status] = CASE
                    WHEN [Status] = 0 THEN 1   -- New       → Reported
                    WHEN [Status] = 1 THEN 2   -- Forwarded → UnderReview
                    WHEN [Status] = 2 THEN 3   -- Resolved  → Resolved (old numbering)
                    ELSE [Status]
                END
                WHERE [Status] IN (0, 1, 2);
            ");
        }
    }
}
