# SQL Flow Analyzer - Troubleshooting Guide

## Issue: Analyzer Not Showing Results After Clicking Analyze

If you're experiencing issues where the analyzer doesn't show results after selecting SQL text and clicking Analyze, follow these debugging steps:

---

## Step 1: Check the Log Files

The extension writes detailed logs that will show exactly what's happening.

### Log Location
```
C:\Users\<YOUR_USERNAME>\AppData\Local\SSMS Plus\log\
```

### What to Look For

1. **Open the most recent log file** (sorted by date)

2. **Search for these key messages:**

   ```
   "Starting SQL flow analysis"
   "Getting SQL text from active document"
   "Analyzing SQL text"
   "UpdateDisplay: Processing X nodes"
   ```

3. **Common Issues in Logs:**

   **Issue A: "No SQL text retrieved from active document"**
   - **Cause**: SSMS editor not accessible or no document open
   - **Solution**: See "Fix SQL Text Retrieval" below

   **Issue B: "Analysis complete. Nodes: 0, Errors: 0"**
   - **Cause**: SQL doesn't contain control flow structures
   - **Solution**: Make sure SQL has BEGIN/END, IF, WHILE, etc.

   **Issue C: "Error analyzing SQL"**
   - **Cause**: Parser exception
   - **Solution**: Check error message and stack trace in logs

   **Issue D: "Root VM has no children to display"**
   - **Cause**: Nodes exist but tree structure is wrong
   - **Solution**: Report this as a bug with SQL sample

---

## Step 2: Verify SQL Text is Being Retrieved

### Test SQL Sample
Use this simple test to verify the analyzer can read SQL:

```sql
BEGIN
    PRINT 'Test';
END
```

### Steps:
1. Open SSMS
2. Create a new query window
3. Paste the test SQL above
4. Select ALL the text (Ctrl+A)
5. Open Flow Analyzer
6. Click Analyze

### Expected Result:
- Status: "Analysis complete: Total Nodes: 2, BEGIN/END: 1..."
- Tree Tab: Shows 1 BEGIN/END block
- Execution Order: Shows 2 items
- Statistics: Shows BEGIN/END Blocks: 1

### If Still Not Working:
Check the log file for the exact error message.

---

## Step 3: Check the Status Message

Look at the status bar in the Flow Analyzer window (top of the window):

| Status Message | Meaning | Action |
|----------------|---------|--------|
| "Ready. Click 'Analyze'..." | Initial state | Normal |
| "Analyzing SQL control flow..." | Analysis in progress | Wait |
| "Analysis complete: Total Nodes: X..." | Success | Results should appear |
| "No SQL text found..." | Can't access editor | See Step 4 |
| "Analysis completed with X error(s)" | Parse errors | Check Statistics tab |
| "Error: ..." | Exception occurred | Check log files |

---

## Step 4: Fix SQL Text Retrieval Issues

If the analyzer can't get SQL text from SSMS:

### Common Causes:

1. **No Active Document**
   - Solution: Open a SQL file or create new query (Ctrl+N)

2. **Wrong Document Type**
   - Solution: Make sure it's a SQL query window, not a different file type

3. **SSMS Not Properly Initialized**
   - Solution: Restart SSMS

4. **Extension Not Loaded**
   - Solution: Check if "SSMS Plus" menu exists

### Debug Steps:

1. **Verify SSMS Plus is loaded:**
   - Look for "SSMS Plus" menu in SSMS menu bar
   - If not present, extension didn't load

2. **Create new query window:**
   ```
   File → New → Query with Current Connection (Ctrl+N)
   ```

3. **Type simple SQL:**
   ```sql
   SELECT 1
   ```

4. **Select the SQL text** (Ctrl+A)

5. **Try analyzing again**

---

## Step 5: Check for Errors in Statistics Tab

After clicking Analyze, switch to the **Statistics** tab in the Flow Analyzer window.

### Look For:

1. **Errors Section (Red)**
   - Shows parser errors
   - Shows SQL syntax issues
   - Shows retrieval errors

2. **Warnings Section (Orange)**
   - Shows potential issues
   - Non-critical problems

3. **Counts**
   - If all counts are 0, SQL might not have control flow structures

---

## Step 6: Common Issues and Solutions

### Issue: "No control flow structures found"

**Cause**: Your SQL doesn't contain BEGIN/END, IF, WHILE, etc.

**Test**: Use this SQL:
```sql
DECLARE @x INT = 1;
IF @x = 1
BEGIN
    PRINT 'Found control flow';
END
```

**Expected**: Should show 1 IF statement and 1 BEGIN/END block

---

### Issue: Analyze button doesn't do anything

**Possible Causes:**
1. Button event not wired up
2. View Model not initialized
3. Exception being swallowed

**Debug:**
1. Check log file for ANY messages when clicking Analyze
2. If no log messages appear, View Model isn't initialized
3. Try restarting SSMS

---

### Issue: Results appear briefly then disappear

**Cause**: Threading issue or collection being cleared

**Check Log For:**
- "UpdateDisplay: Processing X nodes"
- "Added X root-level nodes to tree"

**If Numbers Are Positive**: Bug in UI binding - report this

---

### Issue: Can only analyze once, then stops working

**Cause**: Possible threading deadlock

**Solution:**
1. Close Flow Analyzer window
2. Reopen from menu
3. Try again

---

## Step 7: Rebuild and Test

If you've made code changes:

### Rebuild Steps:
```
1. Close all SSMS instances
2. Build solution in Visual Studio (Ctrl+Shift+B)
3. Check for build errors
4. Launch SSMS
5. Test again
```

### Verify Installation:
```
C:\Program Files (x86)\Microsoft SQL Server Management Studio [version]\Common7\IDE\Extensions\SSMSPlus
```

Should contain:
- SSMSPlus.dll
- SSMSPlusFlowAnalyzer.dll
- SSMSPlusCore.dll
- Other dependencies

---

## Step 8: Enable Verbose Logging

To get more detailed logs:

1. **Open**: `_settings.config` in SSMSPlus directory

2. **Change log level** (if configuration exists):
   ```xml
   <LogLevel>Debug</LogLevel>
   ```

3. **Restart SSMS**

4. **Reproduce the issue**

5. **Check logs** - will have much more detail

---

## Step 9: Test with Sample SQL

Use the provided test file: `TestFlowAnalyzer.sql`

### Steps:
1. Open `TestFlowAnalyzer.sql` in SSMS
2. Select **Example 1** (lines 5-9):
   ```sql
   BEGIN
       PRINT 'Hello World';
       SELECT GETDATE();
   END
   ```
3. Click Analyze

### Expected Results:
- Status: "Analysis complete: Total Nodes: 2..."
- Tree: Shows 1 BEGIN/END block with 2 child statements
- Execution Order: Shows 2-3 items
- Statistics: BEGIN/END Blocks: 1

If this works, the analyzer is functioning correctly!

---

## Step 10: Collect Diagnostic Information

If still not working, collect this information for bug report:

### Information to Collect:

1. **SSMS Version**
   ```
   Help → About → Copy Info
   ```

2. **Log File**
   ```
   C:\Users\<YOU>\AppData\Local\SSMS Plus\log\<latest>.log
   ```

3. **SQL Sample**
   - The SQL you're trying to analyze
   - Simplify to minimum that reproduces issue

4. **Status Message**
   - What the status bar shows after clicking Analyze

5. **Error Messages**
   - From Statistics tab
   - From log file

6. **Observed Behavior**
   - What happens when you click Analyze
   - Does status change?
   - Do tabs show anything?

---

## Quick Checklist

Before reporting a bug, verify:

- [ ] SSMS Plus menu exists
- [ ] Flow Analyzer window opens
- [ ] SQL file/query is open in SSMS
- [ ] SQL text is selected or cursor is in query window
- [ ] Analyze button is enabled
- [ ] Status message changes when clicking Analyze
- [ ] Checked log files for errors
- [ ] Tested with simple SQL (BEGIN/END block)
- [ ] Rebuilt solution after code changes
- [ ] Restarted SSMS after rebuild

---

## Known Limitations

The analyzer **does not** support:
- Dynamic SQL (EXEC @sql)
- Cursor operations (limited)
- Nested stored procedures (calls not analyzed)
- Comments inside control flow keywords
- Very malformed SQL

---

## Contact Support

If none of these steps resolve the issue:

1. **Create GitHub Issue** with:
   - SSMS version
   - SQL sample that fails
   - Log file snippet
   - Steps to reproduce

2. **Include diagnostic info** from Step 10

3. **Label** as "bug" and "Flow Analyzer"

---

## Additional Resources

- **User Guide**: `FlowAnalyzer-UserGuide.md`
- **Quick Start**: `FlowAnalyzer-QuickStart.md`
- **Test SQL**: `TestFlowAnalyzer.sql`
- **Logs**: `C:\Users\<YOU>\AppData\Local\SSMS Plus\log\`

---

**Last Updated**: After adding comprehensive logging and threading fixes
