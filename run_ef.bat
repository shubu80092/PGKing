@echo off
cd "d:\Shubham Data\Prahlad Project New"
dotnet ef migrations add AddAuthTables --project PGKing.Infrastructure --startup-project PGKing.UI
dotnet ef database update --project PGKing.Infrastructure --startup-project PGKing.UI
