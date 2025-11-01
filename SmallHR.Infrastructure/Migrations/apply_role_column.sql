-- Manual SQL script to add Role column to Employees table
-- Run this if the migration was marked as applied but the column doesn't exist

-- Check if column exists, if not, add it
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employees' 
    AND COLUMN_NAME = 'Role'
)
BEGIN
    ALTER TABLE [Employees] 
    ADD [Role] nvarchar(max) NOT NULL DEFAULT N'Employee';
    PRINT 'Role column added successfully';
END
ELSE
BEGIN
    PRINT 'Role column already exists';
END


