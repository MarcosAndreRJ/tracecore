# 25 — Matriz inicial de perfis e permissões

Perfis são conveniências administrativas. A autorização real deve usar permissões granulares.

| Capacidade | Usuário Técnico | Especialista | Revisor | Gestor | Admin Funcional | Admin |
|---|---:|---:|---:|---:|---:|---:|
| Pesquisar conhecimento | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Criar/editar próprio caso (`caso.criar`, `caso.editar`) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Relacionar casos (`caso.relacionar`) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Diagnosticar caso (`caso.diagnosticar`) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Reabrir caso (`caso.reabrir`) | - | ✓ | ✓ | ✓ | ✓ | ✓ |
| Criar rascunho de solução | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Revisar solução | - | ✓ opcional | ✓ | ✓ opcional | ✓ | ✓ |
| Publicar solução | - | - | ✓ | ✓ opcional | ✓ | ✓ |
| Ver analytics operacional | escopo | escopo | escopo | ✓ | ✓ | ✓ |
| Ver analytics individual | próprio/limitado | limitado | limitado | ✓ escopo | ✓ | ✓ |
| Gerenciar clientes (`cliente.gerenciar`) | - | - | - | - | ✓ | ✓ |
| Gerenciar catálogo (`catalogo.gerenciar`) | - | escopo | - | escopo | ✓ | ✓ |
| Gerenciar usuários (`usuario.gerenciar`) | - | - | - | limitado | ✓ | ✓ |
| Gerenciar papéis | - | - | - | - | limitado | ✓ |
| Ver auditoria | próprio/limitado | limitado | limitado | escopo | ✓ | ✓ |
| Configurar IA | - | - | - | - | ✓ | ✓ |
| Ver conteúdo sensível | por ACL | por ACL | por ACL | por ACL | por ACL | por ACL |

## Regras

- Papel `Admin` é único e herda dinamicamente todas as permissões do sistema.
- Existe proteção estrita no `UserService` impedindo desativar ou revogar o papel do último usuário `Admin` ativo.
- Permissão de publicação é restrita a revisores e administradores.
- Mudanças de papel/permissão devem auditar o evento completo em `audit_events`.

