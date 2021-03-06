﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedGate.Shared;
using RedGate.Shared.SQL;

namespace DeadlockApp
{
    class DbSetup
    {
        public void CreateTables(string instance, string databaseName, string username, string password)
        {
            // Create database connection
            var connection = string.IsNullOrEmpty(username) ? new DBConnectionInformation(instance, databaseName) : new DBConnectionInformation(instance, databaseName, username, password);
            
            var batch1 = @"IF OBJECT_ID(N'[dbo].[Employees]', 'U') IS NOT NULL
                           BEGIN
                                DROP TABLE Employees
                           END
                           CREATE TABLE Employees (
                                EmpId INT IDENTITY,
                                EmpName VARCHAR(16),
                                Phone VARCHAR(16)
                           )";

            var batch2 = @"INSERT INTO Employees (EmpName, Phone)
                            VALUES ('Martha', '800-555-1212'), ('Jimmy', '619-555-8080')";

            var batch3 = @"IF OBJECT_ID(N'[dbo].[Suppliers]', 'U') IS NOT NULL
                           BEGIN
                               DROP TABLE Suppliers
                           END                            
                           CREATE TABLE Suppliers(
                               SupplierId INT IDENTITY,
                               SupplierName VARCHAR(64),
                               Fax VARCHAR(16)
                           )";

            var batch4 = @"INSERT INTO Suppliers (SupplierName, Fax)
                           VALUES ('Acme', '877-555-6060'), ('Rockwell', '800-257-1234')";
            using (var block = new RedGate.Shared.SQL.ExecutionBlock.ExecutionBlock())
            {
                block.AddBatch(batch1);
                block.AddBatch(batch2);
                block.AddBatch(batch3);
                block.AddBatch(batch4);
                var executor = new RedGate.Shared.SQL.ExecutionBlock.BlockExecutor();
                executor.ExecuteBlock(block,connection);
            }
            

        }

        public void CreateSprocs(string instance, string databaseName, string username, string password)
        {
            // Create database connection
            var connection = string.IsNullOrEmpty(username) ? new DBConnectionInformation(instance, databaseName) : new DBConnectionInformation(instance, databaseName, username, password);
            var batch1 = @"IF OBJECT_ID(N'[dbo].[UserADeadlock]') IS NULL
                            EXEC sp_executesql	N'CREATE PROCEDURE UserADeadlock AS
	                            BEGIN
		                            BEGIN TRANSACTION
			                            UPDATE dbo.Employees SET EmpName = ''Mary'' WHERE EmpId = 1
			                            WAITFOR DELAY ''00:01''
			                            UPDATE dbo.Suppliers SET Fax = ''555-1212'' WHERE SupplierId = 1
		                            ROLLBACK
				
	                            END'";

            var batch2 = @"IF OBJECT_ID(N'[dbo].[UserBDeadlock]') IS NULL
                            EXEC sp_executesql	N'CREATE PROCEDURE UserBDeadlock AS
		                        BEGIN
			                        BEGIN TRANSACTION
				                        UPDATE dbo.Suppliers SET Fax = ''555-1212'' WHERE SupplierId = 1
				                        WAITFOR DELAY ''00:01''
				                        UPDATE dbo.Employees SET EmpName = ''Mary'' WHERE EmpId = 1	
			                        ROLLBACK
		                        END'";

            var batch3 = @"IF OBJECT_ID(N'[dbo].[UserABlocking]') IS NULL
                            EXEC sp_executesql	N'CREATE PROCEDURE UserABlocking AS
	                            BEGIN
		                            BEGIN TRANSACTION
			                            UPDATE dbo.Suppliers SET Fax = ''555-1212'' WHERE SupplierId = 1
	                                    WAITFOR DELAY ''00:10''
		                            ROLLBACK
				
	                            END'";

            var batch4 = @"IF OBJECT_ID(N'[dbo].[UserBBlocking]') IS NULL
                            EXEC sp_executesql	N'CREATE PROCEDURE UserBBlocking AS
	                            BEGIN
		                            BEGIN TRANSACTION
			                            UPDATE dbo.Employees SET EmpName = ''Mary'' WHERE EmpId = 1	
	                                    UPDATE dbo.Suppliers SET Fax = ''555-1212'' WHERE SupplierId = 1
		                            ROLLBACK
				
	                            END'";

            var batch5 = @"IF OBJECT_ID(N'[dbo].[UserCBlocking]') IS NULL
                            EXEC sp_executesql	N'CREATE PROCEDURE UserCBlocking AS
	                            BEGIN
		                            BEGIN TRANSACTION
			                            UPDATE dbo.Employees SET EmpName = ''Martha'' WHERE EmpId = 1
		                            ROLLBACK
				
	                            END'";

            using (var block = new RedGate.Shared.SQL.ExecutionBlock.ExecutionBlock())
            {
                block.AddBatch(batch1);
                block.AddBatch(batch2);
                block.AddBatch(batch3);
                block.AddBatch(batch4);
                block.AddBatch(batch5);
                var executor = new RedGate.Shared.SQL.ExecutionBlock.BlockExecutor();
                executor.ExecuteBlock(block, connection);
            }


        }
    }
}
