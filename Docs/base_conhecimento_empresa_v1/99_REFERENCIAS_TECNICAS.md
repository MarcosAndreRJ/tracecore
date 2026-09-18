# 99 — Referências técnicas consultadas para a baseline

Data de consulta: 17/09/2026.

## .NET

- Microsoft — Download .NET: https://dotnet.microsoft.com/download
- Microsoft — Política de suporte do .NET: https://dotnet.microsoft.com/platform/support/policy
- Microsoft Learn — ASP.NET Core 10: https://learn.microsoft.com/aspnet/core/?view=aspnetcore-10.0

A baseline usa .NET 10 porque é a versão LTS ativa no momento da documentação.

## MySQL

- MySQL 8.4 Reference Manual: https://dev.mysql.com/doc/refman/8.4/en/
- MySQL Releases: Innovation and LTS: https://dev.mysql.com/doc/refman/8.4/en/mysql-releases.html
- MySQL Community Server 8.4 LTS: https://dev.mysql.com/downloads/mysql/8.4.html

A baseline usa a linha MySQL 8.4 LTS para privilegiar estabilidade. A versão de patch deve ser mantida atualizada conforme política de segurança da empresa.

## Provider .NET/MySQL

- MySQL Connector/NET / EF Core support: https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html
- Pomelo.EntityFrameworkCore.MySql: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql

Como a compatibilidade de providers EF pode variar por release, a baseline técnica propõe Dapper + MySqlConnector e deixa EF Core sujeito a spike/ADR. Isso preserva .NET 10 e MySQL sem acoplar o domínio a um provider específico.

