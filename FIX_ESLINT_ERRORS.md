# Quick Fix: ESLint Errors

The build is failing due to ESLint warnings being treated as errors. The quickest fix is to add eslint-disable comments. However, I'll use useCallback for proper React patterns where possible.

## Files to Fix:
1. TagAssignmentModal.tsx - useCallback for loadTagTypes ✅
2. Admin/Users.tsx - Remove unused variables (prefix with _)
3. ContactLogs/Detail.tsx - Remove unused import, useCallback for loadContactLog
4. ContactLogs/Form.tsx - Remove unused variable, useCallback for loadData
5. ContactLogs/List.tsx - useCallback for applyFilters
6. JournalLogs/Detail.tsx - Remove unused import, useCallback for loadJournalLog
7. JournalLogs/Form.tsx - Remove unused variable, useCallback for loadData
8. JournalLogs/List.tsx - useCallback for applyFilters

Let me fix these systematically.
