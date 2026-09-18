# 12 — Segurança, auditoria e LGPD

## 1. Modelo de acesso

Adotar RBAC com permissões granulares e escopos.

Exemplos de permissões:
- `Cases.View`
- `Cases.Create`
- `Cases.Edit`
- `Cases.Resolve`
- `Cases.ViewRestricted`
- `Knowledge.Create`
- `Knowledge.Review`
- `Knowledge.Publish`
- `Analytics.ViewExecutive`
- `Users.Manage`
- `Audit.View`
- `Ai.Use`
- `Ai.Admin`

## 2. Princípio do menor privilégio

A pessoa recebe somente o necessário para a função. Perfis não devem ser “Admin para resolver rápido”.

## 3. Conteúdo restrito

Itens podem possuir classificação:
- Interno;
- Restrito;
- Sensível.

Casos de cliente podem ter restrição adicional por contrato/área. O mecanismo de busca/RAG precisa respeitar a mesma autorização.

## 4. Dados que não devem aparecer em texto livre

Orientar e sanitizar:
- senhas;
- tokens;
- chaves API;
- cookies de sessão;
- strings de conexão com segredo;
- dados pessoais desnecessários;
- dumps integrais de produção sem tratamento.

Criar scanners/redaction básicos para padrões conhecidos antes de indexação de IA.

## 5. Auditoria

Auditar no mínimo:
- login/logoff e falhas relevantes;
- criação/edição/encerramento de casos;
- mudança de severidade/owner;
- publicação/depreciação de conhecimento;
- gestão de usuários, perfis e permissões;
- alterações de configuração;
- exportações;
- ações administrativas;
- uso de ferramentas de IA com fontes e correlação;
- leitura de conteúdo altamente restrito se política exigir.

## 6. LGPD e dados de colaboradores/clientes

A plataforma deve aplicar:
- finalidade;
- necessidade/minimização;
- controle de acesso;
- retenção;
- segurança;
- rastreabilidade.

“Ver tudo que o usuário faz” deve significar **ações relevantes dentro da plataforma para operação, segurança e gestão**, e não coleta indiscriminada sem finalidade. Métricas individuais devem ter acesso restrito e contexto.

## 7. Retenção e anonimização

Configurar políticas por tipo de dado. Quando detalhe individual não for mais necessário para analytics, preferir agregação/anomização quando compatível com a finalidade.

## 8. Segurança web

- cookies Secure/HttpOnly/SameSite;
- CSRF nos fluxos aplicáveis;
- CSP;
- proteção XSS;
- validação server-side;
- rate limiting;
- upload com validação de MIME/extensão/tamanho e antivírus quando disponível;
- headers seguros;
- TLS obrigatório.

## 9. Segredos

Segredos fora de `appsettings.json` versionado. Usar secret store apropriado ao ambiente. Rotação documentada.

## 10. IA

- não enviar segredo;
- limitar dados pessoais;
- permitir configuração de provedores aprovados;
- registrar região/termos do provedor conforme governança;
- bloquear ferramentas não autorizadas;
- tratar documentos recuperados como conteúdo não confiável para instruções;
- redaction antes de embeddings quando necessário.

