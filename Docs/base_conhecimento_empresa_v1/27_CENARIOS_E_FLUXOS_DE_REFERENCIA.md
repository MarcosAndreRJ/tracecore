# 27 — Cenários e fluxos de referência

Estes cenários não são regras rígidas de diagnóstico. Servem para orientar a modelagem do produto e os testes de jornada.

## Cenário A — “Não consigo entrar no sistema”

### Entrada
Cliente informa que um usuário não consegue acessar.

### Contexto inicial desejado
- cliente;
- produto/canal: web, desktop ou mobile;
- ambiente;
- versão, quando aplicável;
- usuário afetado ou escopo;
- texto exato da mensagem;
- horário de ocorrência.

### Primeira redução de incerteza
1. afeta um usuário ou vários?
2. a interface abre?
3. o erro ocorre antes ou depois do envio da credencial?
4. autenticação está saudável?
5. API correspondente responde?
6. o mesmo usuário acessa por outro canal?
7. houve bloqueio/expiração/configuração recente?

### Possíveis domínios envolvidos
- identidade/autenticação;
- rede/infra;
- frontend;
- API;
- banco/serviço de identidade;
- configuração do cliente;
- versão do desktop/mobile.

### O que a plataforma deve mostrar
- casos semelhantes por erro e contexto;
- known issues de versão;
- verificações de baixo risco;
- resultados históricos;
- responsável somente quando evidência apontar um domínio.

## Cenário B — Desktop “não conecta ao servidor”

### Sinais úteis
- host/endpoint configurado;
- resolução DNS;
- porta;
- TLS/certificado;
- proxy/firewall;
- reachability da API;
- versão do desktop;
- configuração local;
- status do serviço.

### Fluxo
Relato → identificar se falha é local ou coletiva → testar endpoint → comparar configuração com cliente saudável → verificar infraestrutura → verificar compatibilidade de versão → relacionar caso/solução → resolver/escalar.

## Cenário C — Web carrega, mas operação falha

Exemplo: tela abre, porém salvar retorna erro.

A plataforma deve separar:
- UI carregada;
- autenticação válida;
- chamada de API específica;
- regra de negócio;
- persistência;
- integração chamada pela operação.

Evidências como HTTP status, correlation ID e timestamp devem permitir atravessar frontend → API → banco/integração usando observabilidade.

## Cenário D — Integração SAP não processa

### Perguntas iniciais
- todas as operações ou tipo específico?
- uma unidade/cliente ou todos?
- fila acumulada?
- SAP acessível?
- autenticação/certificado válido?
- payload rejeitado?
- mudança de schema/configuração?
- retry está ocorrendo?

### Comportamento desejado
A busca deve encontrar casos pelo nome funcional e também por códigos técnicos, sem exigir que o atendente saiba se a falha está no produto, no adapter ou no SAP.

## Cenário E — Lentidão intermitente

A plataforma deve evitar solução genérica “reinicie”.

Coletar:
- período;
- usuários afetados;
- operação;
- latência observada;
- release recente;
- métricas de API;
- slow queries;
- consumo de recurso;
- dependências externas;
- concorrência.

Casos anteriores devem ser comparados por padrão temporal, componente e evidência, não apenas pela palavra “lento”.

## Cenário F — Mesmo problema reaparece meses depois

### Objetivo central do produto
Ao abrir o novo caso, o sistema identifica:
- mesma mensagem/sintoma;
- mesmo componente;
- mesma versão ou família;
- cliente diferente ou igual;
- solução anteriormente usada;
- tentativa que falhou;
- causa raiz anterior;
- ação preventiva que ficou pendente.

O técnico deve conseguir reaproveitar o caminho anterior e registrar se a recorrência confirma a mesma causa ou representa uma nova variação.

## Cenário G — Escalonamento entre departamentos

Antes de transferir, a plataforma gera automaticamente um resumo:

```text
Problema: ...
Cliente/ambiente/versão: ...
Impacto: ...
Sintomas confirmados: ...
Hipóteses descartadas: ...
Hipóteses ainda abertas: ...
Testes já executados: ...
Evidências principais: ...
Casos/soluções semelhantes: ...
Motivo do escalonamento: ...
```

O receptor não deve precisar pedir novamente informações já registradas.

## Cenário H — Solução histórica ficou desatualizada

Novo caso encontra artigo antigo, mas a versão atual é incompatível. O sistema deve:
- penalizar/alertar no ranking;
- mostrar versão validada;
- permitir feedback “não se aplica”; 
- abrir tarefa de revisão se houver recorrência;
- manter artigo antigo acessível para casos históricos.

