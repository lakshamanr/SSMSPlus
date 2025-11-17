# SQL Flow Analyzer - Quick Start Guide

Get up and running with the SQL Flow Analyzer in 5 minutes!

---

## 🚀 Quick Start (3 Steps)

### Step 1: Open the Tool
1. Launch **SQL Server Management Studio (SSMS)**
2. Click **SSMS Plus** in the menu bar
3. Select **BEGIN/END Flow Analyzer**

### Step 2: Load Your SQL
1. Open a SQL script in SSMS (or create a new query)
2. Either:
   - Leave the entire script to analyze everything
   - Select specific SQL text to analyze just that portion

### Step 3: Analyze
1. Click the **🔍 Analyze** button
2. View results in three tabs:
   - **Control Flow Tree** - See the structure
   - **Execution Order** - See the sequence
   - **Statistics** - See the metrics

**That's it!** You're now analyzing SQL control flow.

---

## 📊 Understanding the Results

### Control Flow Tree Tab
```
└─ BEGIN...END (Level 0)
   ├─ IF condition (Line 5, Level 1)
   │  └─ THEN block
   └─ WHILE loop (Line 10, Level 1)
      └─ Loop body
```
- **What**: Shows structure and nesting
- **Use**: Find complex nested areas
- **Tip**: Click nodes to expand/collapse

### Execution Order Tab
```
#1: BEGIN...END (Line 1, Level 0)
#2: IF @Value > 5 (Line 3, Level 1)
#3: WHILE @Counter < 10 (Line 7, Level 1)
#4: GOTO Label1 (Line 10, Level 1)
```
- **What**: Shows step-by-step flow
- **Use**: Trace execution path
- **Tip**: Numbers show the order

### Statistics Tab
```
BEGIN/END Blocks: 5
IF Statements: 3
WHILE Loops: 2
TRY/CATCH Blocks: 1
GOTO Statements: 1
Max Nesting Level: 4
```
- **What**: Shows counts and metrics
- **Use**: Measure complexity
- **Tip**: High nesting = complex code

---

## ✅ What It Analyzes

| Feature | Supported |
|---------|-----------|
| BEGIN...END blocks | ✅ Yes (nested) |
| IF...ELSE | ✅ Yes (nested) |
| WHILE loops | ✅ Yes (nested) |
| TRY...CATCH | ✅ Yes (nested) |
| GOTO/Labels | ✅ Yes |
| BREAK/CONTINUE | ✅ Yes |
| RETURN | ✅ Yes |

---

## 💡 Quick Tips

1. **Analyze Selected Text**
   - Select SQL → Click Analyze
   - Great for large files

2. **Check Nesting Levels**
   - Level 5+ = Consider refactoring
   - Deep nesting = harder to maintain

3. **Fix Errors First**
   - Check Statistics tab for errors
   - Red messages = things to fix

4. **Use for Code Review**
   - Screenshot the tree view
   - Share execution order with team
   - Document complex logic

---

## 🔧 Common Use Cases

### Use Case 1: Understanding Stored Procedures
**Before**: Complex procedure, hard to follow
**After**: Visual tree shows all branches and loops
**Benefit**: Quick comprehension of logic flow

### Use Case 2: Finding Code Complexity
**Before**: Unsure which procedures are complex
**After**: Statistics show nesting levels
**Benefit**: Prioritize refactoring efforts

### Use Case 3: Debugging Control Flow
**Before**: Logic error in nested IF/WHILE
**After**: Execution order shows the path
**Benefit**: Faster debugging

### Use Case 4: Code Reviews
**Before**: Manual review of control flow
**After**: Visual analysis of structure
**Benefit**: Consistent quality checks

---

## ❓ Troubleshooting

| Problem | Solution |
|---------|----------|
| "No SQL text found" | Open a SQL file first |
| Tool window doesn't open | Restart SSMS |
| Parse errors | Check SQL syntax |
| Missing constructs | See limitations in full guide |

---

## 📖 Example

**Input SQL**:
```sql
BEGIN
    DECLARE @Count INT = 0;

    WHILE @Count < 10
    BEGIN
        IF @Count = 5
            GOTO SpecialCase;

        PRINT @Count;
        SET @Count = @Count + 1;
    END

    SpecialCase:
        PRINT 'Done';
END
```

**Output**:
- **Tree**: Shows BEGIN, WHILE, IF, GOTO hierarchy
- **Execution Order**: 5 steps showing flow
- **Statistics**: 2 BEGIN/END, 1 IF, 1 WHILE, 1 GOTO, Max Level: 2

---

## 🎯 Next Steps

1. ✅ **Analyze your first script** - Open a procedure and click Analyze
2. 📚 **Read full guide** - See `FlowAnalyzer-UserGuide.md` for details
3. 🔍 **Explore features** - Try all three tabs
4. 💪 **Use regularly** - Make it part of your workflow

---

## 📞 Need More Help?

- **Full User Guide**: `FlowAnalyzer-UserGuide.md`
- **Examples**: `TestFlowAnalyzer.sql` in repository
- **Issues**: Report via GitHub Issues

---

**Happy Analyzing! 🎉**
