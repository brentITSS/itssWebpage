# Remaining ESLint Errors to Fix

We need to fix these before the workflow will pass:

1. ✅ TagAssignmentModal.tsx - useCallback for loadTagTypes (IN PROGRESS - needs useEffect)
2. Admin/Users.tsx - Remove unused: roles, editingUser, handleUpdateUser  
3. ContactLogs/Detail.tsx - Remove unused AttachmentDto, useCallback for loadContactLog
4. ContactLogs/Form.tsx - Remove unused contactLog, useCallback for loadData
5. ContactLogs/List.tsx - useCallback for applyFilters
6. JournalLogs/Detail.tsx - Remove unused AttachmentDto, useCallback for loadJournalLog  
7. JournalLogs/Form.tsx - Remove unused journalLog, useCallback for loadData
8. JournalLogs/List.tsx - useCallback for applyFilters

**We're NOT ready yet** - need to fix all these first.
