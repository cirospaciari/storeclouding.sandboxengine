/****** Script for SelectTopNRows command from SSMS  ******/
SELECT COUNT_BIG(ID)
  FROM [SandBoxEngine].[dbo].[Chunk]
 
  /****** Script for SelectTopNRows command from SSMS  ******/
SELECT  COUNT_BIG(ID)
  FROM [SandBoxEngine].[dbo].[BlockData]

SELECT  MAX(ChunkID)
  FROM [SandBoxEngine].[dbo].[BlockData]


 /* 
 10c x 220b x 10c

 100c x 100b x 100c
 
 49023
 18 249 283

truncate table [SandBoxEngine].[dbo].[BlockData]
truncate table [SandBoxEngine].[dbo].[Chunk]
DBCC SHRINKDATABASE (SandBoxEngine)*/


