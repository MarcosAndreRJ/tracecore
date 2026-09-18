# Banco de dados

`schema_inicial.sql` é um esqueleto técnico para acelerar a primeira implementação. Não substitui migrations.

Antes do primeiro release:
1. fechar ADR-P001 (estratégia de IDs);
2. transformar DDL em migrations versionadas;
3. validar índices com dados representativos;
4. revisar FKs e retenção;
5. confirmar charset/collation corporativos;
6. testar restore/backup.

O script usa `BINARY(16)` como referência para `Guid`. Se a ADR escolher outra estratégia, ajustar antes de consolidar migrations.
