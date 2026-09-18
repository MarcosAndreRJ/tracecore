# TraceCore — Credenciais de Desenvolvimento e Teste

> [!WARNING]
> Este documento é estritamente para uso em ambiente de desenvolvimento local e testes.
> Em conformidade com a regra **BR-101**, credenciais padrão jamais devem ser expostas na interface web ou em ambientes de homologação/produção.

## Usuário Administrador Padrão (Seed)

- **Perfil:** Administrador de Segurança (Acesso irrestrito a usuários, permissões, casos e diagnósticos)
- **E-mail:** `admin@tracecore.local`
- **Senha:** `Password123!`
- **Origem do Seed:** `TraceCore.Infrastructure/Migrations/M20260917_03_SeedRolePermissionsAndAdmin.cs` / `InMemoryRepositories.cs`
