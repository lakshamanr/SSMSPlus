# SQL Flow Analyzer - User Guide

## Overview

The **SQL Flow Analyzer** is a powerful tool that helps you visualize and understand the control flow structure of your SQL scripts. It analyzes BEGIN/END blocks, conditional statements, loops, error handling, and jump statements to provide a clear hierarchical view of your code's execution flow.

---

## Table of Contents

1. [Installation](#installation)
2. [Getting Started](#getting-started)
3. [Features](#features)
4. [How to Use](#how-to-use)
5. [Understanding the Views](#understanding-the-views)
6. [Examples](#examples)
7. [Tips & Best Practices](#tips--best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Installation

### Prerequisites
- SQL Server Management Studio (SSMS) 18, 19, or 20
- Windows operating system

### Installation Steps

1. **Build the Solution**
   - Open `SSMSPlus.sln` in Visual Studio
   - Build the solution in Release mode
   - The VSIX will be generated in the output directory

2. **Install the Extension**
   - The extension automatically deploys to SSMS during build (if configured)
   - OR manually copy the extension files to:
     ```
     C:\Program Files (x86)\Microsoft SQL Server Management Studio [version]\Common7\IDE\Extensions\SSMSPlus
     ```

3. **Verify Installation**
   - Launch SSMS
   - Look for "SSMS Plus" menu in the main menu bar
   - The menu should contain "BEGIN/END Flow Analyzer" option

---

## Getting Started

### Opening the Flow Analyzer

**Method 1: Using the Menu**
1. Open SSMS
2. Click **SSMS Plus** in the menu bar
3. Select **BEGIN/END Flow Analyzer**

**Method 2: Using Keyboard Shortcut** (if configured)
- Press the assigned shortcut key

### First Analysis

1. Open or create a SQL script in SSMS
2. Open the Flow Analyzer window
3. Click the **🔍 Analyze** button
4. The tool will analyze your script and display the results

---

## Features

### Supported SQL Constructs

The Flow Analyzer recognizes and analyzes:

| Construct | Description | Detection |
|-----------|-------------|-----------|
| **BEGIN...END** | Block structures | ✅ Nested blocks supported |
| **IF...ELSE** | Conditional branching | ✅ Nested conditions supported |
| **WHILE** | Loop structures | ✅ Detects nested loops |
| **TRY...CATCH** | Error handling | ✅ Nested try/catch supported |
| **GOTO/Labels** | Jump statements | ✅ Cross-references resolved |
| **BREAK** | Loop exit | ✅ |
| **CONTINUE** | Loop continuation | ✅ |
| **RETURN** | Procedure/function exit | ✅ |

### Analysis Capabilities

- **Hierarchical Tree View**: See the nested structure of your code
- **Execution Order**: Understand the step-by-step flow
- **Nesting Level Tracking**: Identify deeply nested structures
- **Cross-Reference Resolution**: See where GOTO statements jump to
- **Statistics**: Get counts and metrics about your code
- **Error Detection**: Identify mismatched BEGIN/END, undefined labels, etc.

---

## How to Use

### Basic Workflow

#### Step 1: Prepare Your SQL Script

```sql
-- Example: Open or paste your SQL script in SSMS
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
        PRINT 'Special case reached';
END
```

#### Step 2: Analyze the Script

1. **Select Text** (Optional)
   - Select specific portion of SQL to analyze
   - OR leave nothing selected to analyze the entire document

2. **Click Analyze**
   - Click the **🔍 Analyze** button in the Flow Analyzer window
   - The tool processes your script immediately

#### Step 3: Review Results

The tool displays three tabs with different views of your code's control flow.

---

## Understanding the Views

### 1. Control Flow Tree Tab

**What It Shows**: Hierarchical tree structure of all control flow constructs

**Features**:
- **Expandable Nodes**: Click to expand/collapse nested structures
- **Node Information**: Each node shows:
  - Type (BEGIN/END, IF, WHILE, etc.)
  - Condition (for IF/WHILE)
  - Line number
  - Nesting level
- **Visual Hierarchy**: Indentation shows parent/child relationships

**Example Display**:
```
└─ BEGIN...END (Level 0)
   ├─ IF @Value > 5 (Line 10, Level 1)
   │  └─ BEGIN...END (Level 2)
   │     └─ PRINT 'Greater' (Line 11, Level 3)
   └─ WHILE @Counter < 10 (Line 15, Level 1)
      └─ BEGIN...END (Level 2)
```

**How to Use**:
- Click nodes to expand/collapse
- Review nesting levels to identify complex areas
- Check conditions to understand branching logic

### 2. Execution Order Tab

**What It Shows**: Step-by-step execution sequence of control flow statements

**Features**:
- **Numbered Steps**: Each statement has an execution order number
- **Location Information**: Shows line number and nesting level
- **Sequential View**: See the order statements will execute

**Example Display**:
```
#0: BEGIN...END (Line 1, Level 0)
#1: IF @Value > 5 (Line 3, Level 1)
#2: BEGIN...END (Line 4, Level 2)
#3: WHILE @Counter < 10 (Line 8, Level 1)
#4: GOTO Label1 (Line 10, Level 1)
#5: LABEL: Label1 (Line 15, Level 1)
```

**How to Use**:
- Follow the numbers to trace execution flow
- Identify where jumps (GOTO) change the flow
- Understand the sequence of nested blocks

### 3. Statistics Tab

**What It Shows**: Summary metrics and analysis details

**Sections**:

#### Control Flow Constructs
- **BEGIN/END Blocks**: Count of block structures
- **IF Statements**: Number of conditional branches
- **WHILE Loops**: Loop count
- **TRY/CATCH Blocks**: Error handling blocks
- **GOTO Statements**: Jump statement count
- **Labels**: Label definition count
- **Maximum Nesting Level**: Deepest nesting found

#### Errors (if any)
- Missing END statements
- Mismatched BEGIN/END pairs
- Undefined GOTO targets
- Other parsing errors

#### Warnings (if any)
- Duplicate labels
- Unreachable code
- Orphaned ELSE statements

**How to Use**:
- Review counts to understand code complexity
- Check nesting level to identify deep nesting
- Fix errors and warnings to improve code quality

---

## Examples

### Example 1: Simple Stored Procedure

**SQL Code**:
```sql
CREATE PROCEDURE ProcessOrder @OrderId INT
AS
BEGIN
    DECLARE @Status VARCHAR(20);

    SELECT @Status = Status FROM Orders WHERE OrderId = @OrderId;

    IF @Status = 'Pending'
    BEGIN
        PRINT 'Processing order...';
        UPDATE Orders SET Status = 'Processing' WHERE OrderId = @OrderId;
    END
    ELSE
    BEGIN
        PRINT 'Order already processed';
    END
END
```

**Analysis Results**:
- **Control Flow Tree**:
  - 1 root BEGIN/END block
  - 1 IF/ELSE statement
  - 2 nested BEGIN/END blocks
- **Statistics**:
  - BEGIN/END Blocks: 3
  - IF Statements: 1
  - Max Nesting Level: 2

### Example 2: Complex Error Handling

**SQL Code**:
```sql
BEGIN TRY
    BEGIN
        -- Outer operation
        IF @Value > 0
        BEGIN
            BEGIN TRY
                -- Inner operation
                SELECT 1/0;  -- Will cause error
            END TRY
            BEGIN CATCH
                PRINT 'Inner error handled';
            END CATCH
        END
    END
END TRY
BEGIN CATCH
    PRINT 'Outer error: ' + ERROR_MESSAGE();
END CATCH
```

**Analysis Results**:
- **Control Flow Tree**:
  - Shows nested TRY/CATCH structure
  - IF block within outer TRY
  - Inner TRY/CATCH within IF
- **Cross-References**:
  - Outer TRY linked to outer CATCH
  - Inner TRY linked to inner CATCH
- **Statistics**:
  - TRY/CATCH Blocks: 2
  - Nesting Level: 4

### Example 3: Loop with GOTO

**SQL Code**:
```sql
DECLARE @Counter INT = 0;

LoopStart:
    IF @Counter >= 10
        GOTO LoopEnd;

    PRINT @Counter;
    SET @Counter = @Counter + 1;
    GOTO LoopStart;

LoopEnd:
    PRINT 'Loop completed';
```

**Analysis Results**:
- **Control Flow Tree**:
  - Shows GOTO statements
  - Shows label definitions
- **Execution Order**:
  - #1: LABEL: LoopStart
  - #2: IF @Counter >= 10
  - #3: GOTO LoopEnd
  - #4: GOTO LoopStart
  - #5: LABEL: LoopEnd
- **Cross-References**:
  - GOTO LoopStart → targets LoopStart label
  - GOTO LoopEnd → targets LoopEnd label

---

## Tips & Best Practices

### For Analyzing Code

1. **Start Small**
   - Analyze individual procedures first
   - Then analyze larger scripts
   - Build understanding incrementally

2. **Use Selection**
   - Select specific sections to analyze parts of large scripts
   - Focus on complex areas
   - Isolate problematic code

3. **Review Nesting Levels**
   - Deep nesting (Level 5+) may indicate refactoring opportunities
   - Consider breaking complex nested blocks into separate procedures
   - Simplify logic where possible

4. **Check for Errors**
   - Always review the Statistics tab for errors
   - Fix mismatched BEGIN/END before deployment
   - Resolve undefined GOTO targets

5. **Understand GOTO Flow**
   - Use the tool to trace GOTO jumps
   - Consider replacing GOTO with structured constructs
   - Document complex jump patterns

### For Code Quality

1. **Identify Complex Areas**
   - High nesting levels = high complexity
   - Many conditional branches = harder to test
   - Use the tool to measure complexity

2. **Document Control Flow**
   - Use the tree view to create documentation
   - Screenshot the execution order for training
   - Share analysis with team members

3. **Refactoring Opportunities**
   - Deeply nested code → Extract to procedures
   - Multiple GOTO statements → Use WHILE loops
   - Long BEGIN/END blocks → Break into smaller units

---

## Troubleshooting

### Common Issues

#### "No SQL text found in the active editor"

**Cause**: No SQL file is open or selected in SSMS

**Solution**:
1. Open a SQL script in SSMS
2. Ensure the SQL editor has focus
3. Click Analyze again

#### "Error analyzing SQL: Parser error"

**Cause**: The SQL contains syntax the parser doesn't recognize

**Solution**:
1. Check the error message in the Statistics tab
2. Verify SQL syntax is correct
3. Try analyzing a smaller section
4. Report complex cases that should be supported

#### Tool Window Doesn't Open

**Cause**: Extension not properly installed or initialized

**Solution**:
1. Restart SSMS
2. Check if "SSMS Plus" menu exists
3. Reinstall the extension if needed
4. Check SSMS extension directory for files

#### Missing BEGIN/END Pairs

**Cause**: Actual SQL syntax error or parser limitation

**Solution**:
1. Review the Errors list in Statistics tab
2. Check SQL for matching BEGIN/END
3. Ensure all blocks are properly closed
4. Fix the SQL syntax

### Performance Tips

1. **Large Scripts**
   - Select portions to analyze instead of entire file
   - Close unused tool windows
   - Analyze in sections

2. **Complex Structures**
   - Allow time for deep nesting analysis
   - Consider simplifying extremely complex code
   - Use Refresh button to re-analyze after changes

---

## Keyboard Shortcuts

| Action | Shortcut | Notes |
|--------|----------|-------|
| Open Tool | (Configure in VSCT) | Customizable |
| Analyze | Click button | No default keyboard shortcut |
| Refresh | Click button | Re-analyzes current script |

---

## Support and Feedback

### Getting Help

1. Check this user guide
2. Review example SQL scripts
3. Check the Statistics tab for specific error messages
4. Contact the development team

### Reporting Issues

When reporting issues, please include:
- SSMS version
- Sample SQL script (if possible)
- Error message from Statistics tab
- Steps to reproduce

### Feature Requests

The Flow Analyzer is designed to be extensible. Suggested enhancements:
- Support for additional SQL constructs
- Export analysis to documentation
- Integration with code review tools
- Performance metrics
- Code complexity scoring

---

## Appendix: Supported T-SQL Constructs

### Fully Supported

✅ BEGIN...END blocks (all nesting levels)
✅ IF...ELSE statements (nested)
✅ WHILE loops (nested)
✅ TRY...CATCH blocks (nested)
✅ GOTO statements
✅ Label definitions (labelname:)
✅ BREAK statements
✅ CONTINUE statements
✅ RETURN statements

### Limitations

⚠️ Dynamic SQL (EXEC) - Not analyzed (string content not parsed)
⚠️ Cursors - Basic support (DECLARE CURSOR, FETCH, CLOSE)
⚠️ Transactions - Not specifically tracked
⚠️ Variables - Declarations tracked but not values
⚠️ CASE expressions - Treated as regular statements

---

## Quick Reference Card

### Analysis Workflow
1. Open SQL file in SSMS
2. Open Flow Analyzer (SSMS Plus menu)
3. Select text (optional) or analyze entire file
4. Click Analyze button
5. Review three tabs: Tree, Execution Order, Statistics

### Understanding Results
- **Tree View** = Structure and hierarchy
- **Execution Order** = Sequential flow
- **Statistics** = Metrics and errors

### Common Actions
- **Expand/Collapse** = Click tree nodes
- **Re-analyze** = Click Refresh button
- **Select portion** = Highlight SQL text before analyzing
- **Fix errors** = Check Statistics tab

---

## Version Information

- **Feature Name**: SQL Flow Analyzer
- **Component**: SSMSPlusFlowAnalyzer
- **SSMS Compatibility**: 18, 19, 20
- **Framework**: .NET Framework 4.8
- **UI**: WPF

---

**End of User Guide**

For technical documentation, see the developer guide.
For troubleshooting, see TROUBLESHOOTING.md in the repository.
