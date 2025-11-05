# SSMS Plus - Data Statistics Feature - Troubleshooting Guide

## How to Access the Feature

1. **Execute a SQL query** in SSMS (F5 or click Execute)
2. **Wait for results** to appear in the result grid
3. **Click on any cell** in the result grid
4. A **floating window** should appear at the bottom of the screen showing statistics

## Checking if It's Working

### View Debug Logs

The feature writes detailed logs to help debug issues. Check the log file at:

```
C:\Users\<YOUR_USERNAME>\AppData\Local\SSMS Plus\ResultGridMonitor.log
```

The log will show:
- When monitoring is triggered
- How many windows and grids are found
- Which grids are hooked
- When cells are selected
- Any errors that occur

### What the Logs Should Show

When working correctly, you should see messages like:
```
[ResultGridMonitor] MonitorActiveWindow called
[ResultGridMonitor] TryFindAndHookResultGrid started
[ResultGridMonitor] Found X windows via Application.Current
[ResultGridMonitor] Found Y DataGrids in this window
[ResultGridMonitor] Hooking into grid: <hash>
[ResultGridMonitor] Successfully hooked N grids
[ResultGridMonitor] Grid_SelectedCellsChanged event fired
[ResultGridMonitor] Statistics calculated: HasData=True, NumericCells=5
[ResultGridMonitor] Statistics window shown
```

## Common Issues

### Issue 1: No logs are created
**Problem**: The feature isn't being triggered at all
**Solution**: Make sure you rebuilt the solution and installed the updated extension

### Issue 2: Logs show "Application.Current is null"
**Problem**: SSMS's result grid might not be WPF-based
**Solution**: Try Approach 2 in the logs (HwndSource enumeration)

### Issue 3: Logs show "Found 0 DataGrids"
**Problem**: SSMS's result grid might use a different control type (not WPF DataGrid)
**Possible causes**:
- SSMS uses a custom grid control
- Results are displayed in a Windows Forms grid
- Need to search for different control types

### Issue 4: Grid is found but events never fire
**Problem**: Successfully hooked but cell selection doesn't trigger events
**Solution**: The grid might not support SelectionUnit.CellOrRowHeader

## Next Steps

1. **Check the log file** at the location above
2. **Share the log contents** so we can see what's happening
3. Based on the logs, we can:
   - Try different approaches to find grids
   - Search for different control types
   - Use UI Automation instead of WPF visual tree

## Manual Test Command (Future Enhancement)

We can add a menu item like "SSMS Plus > Scan for Result Grids" to manually trigger the scan for testing purposes.
