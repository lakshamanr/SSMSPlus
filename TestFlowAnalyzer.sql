-- Test SQL Script for Flow Analyzer
-- This script demonstrates all control-flow constructs that the analyzer can detect

-- Example 1: Simple BEGIN/END block
BEGIN
    PRINT 'Hello World';
    SELECT GETDATE();
END

-- Example 2: Nested BEGIN/END blocks
BEGIN
    PRINT 'Outer block';

    BEGIN
        PRINT 'Inner block';

        BEGIN
            PRINT 'Deeply nested block';
        END
    END
END

-- Example 3: IF/ELSE statements
DECLARE @Value INT = 10;

IF @Value > 5
BEGIN
    PRINT 'Value is greater than 5';
END
ELSE
BEGIN
    PRINT 'Value is 5 or less';
END

-- Example 4: IF with nested IF
IF @Value > 0
BEGIN
    PRINT 'Positive';

    IF @Value > 10
        PRINT 'Greater than 10';
    ELSE
        PRINT '10 or less';
END

-- Example 5: WHILE loop
DECLARE @Counter INT = 1;

WHILE @Counter <= 5
BEGIN
    PRINT 'Counter: ' + CAST(@Counter AS VARCHAR);
    SET @Counter = @Counter + 1;
END

-- Example 6: WHILE with BREAK and CONTINUE
DECLARE @i INT = 0;

WHILE @i < 10
BEGIN
    SET @i = @i + 1;

    IF @i = 3
        CONTINUE;

    IF @i = 7
        BREAK;

    PRINT @i;
END

-- Example 7: TRY/CATCH blocks
BEGIN TRY
    -- This might cause an error
    DECLARE @Result INT = 1 / 0;
END TRY
BEGIN CATCH
    PRINT 'Error occurred: ' + ERROR_MESSAGE();
END CATCH

-- Example 8: Nested TRY/CATCH
BEGIN TRY
    PRINT 'Outer TRY';

    BEGIN TRY
        PRINT 'Inner TRY';
        -- Potential error here
        SELECT 1/0;
    END TRY
    BEGIN CATCH
        PRINT 'Inner CATCH: ' + ERROR_MESSAGE();
    END CATCH
END TRY
BEGIN CATCH
    PRINT 'Outer CATCH: ' + ERROR_MESSAGE();
END CATCH

-- Example 9: GOTO and labels
DECLARE @Status INT = 1;

IF @Status = 1
    GOTO ProcessSuccess;
ELSE
    GOTO ProcessError;

ProcessSuccess:
    PRINT 'Processing completed successfully';
    GOTO EndProcess;

ProcessError:
    PRINT 'Error in processing';
    GOTO EndProcess;

EndProcess:
    PRINT 'Process ended';

-- Example 10: Complex nested structure
DECLARE @OrderCount INT = 100;
DECLARE @ErrorCount INT = 0;

BEGIN TRY
    -- Process orders
    WHILE @OrderCount > 0
    BEGIN
        IF @OrderCount % 10 = 0
        BEGIN
            PRINT 'Checkpoint: ' + CAST(@OrderCount AS VARCHAR) + ' orders remaining';

            BEGIN TRY
                -- Simulate processing
                IF @OrderCount = 50
                    GOTO SpecialHandling;

                -- Regular processing
                SET @OrderCount = @OrderCount - 1;
            END TRY
            BEGIN CATCH
                SET @ErrorCount = @ErrorCount + 1;

                IF @ErrorCount > 5
                BEGIN
                    PRINT 'Too many errors, aborting';
                    BREAK;
                END
            END CATCH
        END
        ELSE
        BEGIN
            SET @OrderCount = @OrderCount - 1;
        END
    END

    GOTO ProcessComplete;

    SpecialHandling:
        PRINT 'Special handling for order 50';
        SET @OrderCount = @OrderCount - 1;
        -- Return to main loop would need different logic in real code

    ProcessComplete:
        PRINT 'All orders processed';
        PRINT 'Total errors: ' + CAST(@ErrorCount AS VARCHAR);
END TRY
BEGIN CATCH
    PRINT 'Fatal error: ' + ERROR_MESSAGE();
    RETURN;
END CATCH

-- Example 11: Stored Procedure with complex flow
CREATE PROCEDURE ProcessCustomerOrder
    @CustomerId INT,
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CustomerStatus VARCHAR(20);
    DECLARE @OrderTotal DECIMAL(10,2);

    BEGIN TRY
        -- Check customer status
        SELECT @CustomerStatus = Status FROM Customers WHERE CustomerId = @CustomerId;

        IF @CustomerStatus IS NULL
        BEGIN
            PRINT 'Customer not found';
            RETURN -1;
        END

        IF @CustomerStatus = 'Inactive'
        BEGIN
            PRINT 'Customer is inactive';
            GOTO HandleInactiveCustomer;
        END

        -- Process order
        WHILE EXISTS (SELECT 1 FROM OrderItems WHERE OrderId = @OrderId AND Processed = 0)
        BEGIN
            BEGIN TRY
                -- Process each item
                UPDATE OrderItems
                SET Processed = 1
                WHERE OrderId = @OrderId
                AND Processed = 0;

                IF @@ROWCOUNT = 0
                    BREAK;
            END TRY
            BEGIN CATCH
                PRINT 'Error processing item: ' + ERROR_MESSAGE();
                CONTINUE;
            END CATCH
        END

        GOTO OrderComplete;

        HandleInactiveCustomer:
            PRINT 'Sending notification for inactive customer';
            -- Logic here
            RETURN -2;

        OrderComplete:
            SELECT @OrderTotal = SUM(Price * Quantity)
            FROM OrderItems
            WHERE OrderId = @OrderId;

            PRINT 'Order processed. Total: ' + CAST(@OrderTotal AS VARCHAR);
            RETURN 0;
    END TRY
    BEGIN CATCH
        PRINT 'Fatal error in order processing: ' + ERROR_MESSAGE();
        RETURN -99;
    END CATCH
END
GO

-- Example 12: Multiple nested levels
BEGIN
    PRINT 'Level 1';

    BEGIN
        PRINT 'Level 2';

        BEGIN
            PRINT 'Level 3';

            BEGIN
                PRINT 'Level 4';

                BEGIN
                    PRINT 'Level 5';
                END
            END
        END
    END
END
