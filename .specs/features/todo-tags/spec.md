# Tags de Tarefas Specification

**Approved**: 2026-11-26 - user: "aprovo a spec, pode seguir para tasks"

## Problem Statement

Hoje uma tarefa só pode ser agrupada por status (`isCompleted`) e prioridade. Não há como marcar tarefas por contexto ("casa", "trabalho", "urgente") nem listar só as de um contexto. Tags livres na própria tarefa resolvem isso sem criar um cadastro novo.

## Goals

- [ ] Um cliente atribui até 10 tags a uma tarefa no `POST` e no `PUT` e as recebe de volta em toda resposta de tarefa.
- [ ] Um cliente filtra `GET /api/v1/todos` por uma ou mais tags, combinadas com E, junto dos filtros que já existem.
- [ ] Clientes atuais, que não enviam `tags`, continuam funcionando sem perder dados.

## Out of Scope

| Feature | Reason |
| ------- | ------ |
| Cadastro de tags (`/api/v1/tags`: criar, renomear, apagar, cor) | Decisão: tag é rótulo livre, existe enquanto alguma tarefa a usa |
| Endpoint que lista as tags distintas em uso (com contagem) | Não pedido; pode virar outra feature |
| Endpoints para adicionar/remover uma tag por vez (`POST/DELETE /todos/{id}/tags`) | Decisão: tags são editadas pelo campo `tags` do `POST`/`PUT` |
| Filtro por tags com OU | Decisão: filtro combina as tags com E |
| Parâmetro `search` casar com tags | `search` continua olhando só título e descrição |
| Renomear uma tag em todas as tarefas de uma vez | Exige cadastro de tags, fora do escopo |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --------------------- | -------------- | --------- | ---------- |
| O que é uma tag | Texto livre ligado à tarefa, sem cadastro próprio | Mais simples; sem CRUD extra | y - user: "Rótulo livre (Recomendado)" |
| Como as tags são editadas | Campo `tags` no `POST` e no `PUT`; no `PUT` a lista enviada substitui a anterior; `tags` ausente (ou `null`) no `PUT` mantém as atuais | Clientes antigos não apagam tags sem querer | y - user: "Campo tags no POST/PUT (Recomendado)" |
| Filtro por várias tags | `?tags=a&tags=b` devolve tarefas que têm todas as tags pedidas (E) | Refina a busca, como os outros filtros | y - user: "Várias tags, E (Recomendado)" |
| Formato e limites | Trim, minúsculas (cultura invariante), repetidas descartadas; letras Unicode, dígitos, `-` e `_`; 1 a 30 caracteres; até 10 por tarefa, contadas depois de descartar repetidas | Evita "Casa" e "casa" como tags diferentes | y - user: "Minúsculas, 30 chars, 10/tarefa (Recomendado)" |
| `tags` ausente ou `null` no `POST` | Tarefa criada sem tags (`tags: []`) | Compatível com clientes atuais | n - padrão do agente |
| Ordem das tags na resposta | Ordem alfabética (ordinal) da forma normalizada | Resposta determinística, fácil de testar | n - padrão do agente |
| Valor do filtro `tags` | Normalizado do mesmo jeito (trim + minúsculas); valores em branco são ignorados; um valor que nenhuma tag pode ter só não casa com nada (sem 400) | Segue o estilo da listagem, que normaliza a query em vez de rejeitar | n - padrão do agente |
| Tarefas que já existem no banco | Passam a ter `tags: []` depois da migration | Nenhum dado a migrar | n - padrão do agente |
| Remoção da tarefa | Remove as tags dela junto | Tags não existem fora da tarefa | n - padrão do agente |
| `updatedAt` | Muda em todo `PUT`, como já acontece hoje, com ou sem mudança nas tags | Comportamento atual do `Update` | n - padrão do agente |
| Concorrência | Última escrita vence, como nos demais campos | N/A for this scope: a API não tem controle de concorrência hoje | n - padrão do agente |
| Observabilidade | Nenhum log ou métrica nova | N/A for this scope: mudança de dados já é coberta pelos logs atuais | n - padrão do agente |
| Auth e rate limit | Os mesmos do grupo `/api/v1/todos` | N/A for this scope: nada muda | n - padrão do agente |

**Open questions:** none - all resolved or logged above. As linhas marcadas `n` são padrões do agente para você corrigir na revisão.

---

## User Stories

### P1: Atribuir tags a uma tarefa ⭐ MVP

**User Story**: Como cliente da API, quero enviar tags ao criar ou editar uma tarefa para marcar o contexto dela.

**Why P1**: Sem guardar e devolver tags, nada mais da feature existe.

**Acceptance Criteria**:

1. WHEN o cliente faz `POST /api/v1/todos` com `tags: ["Casa", " urgente "]` THEN o sistema SHALL responder 201 com `tags: ["casa", "urgente"]`. <!-- TAG-01 -->
2. WHEN o cliente faz `POST /api/v1/todos` sem o campo `tags` THEN o sistema SHALL responder 201 com `tags: []`. <!-- TAG-02 -->
3. WHEN o cliente envia `tags: ["casa", "Casa", "CASA"]` THEN o sistema SHALL guardar e devolver `tags: ["casa"]`. <!-- TAG-03 -->
4. The system SHALL devolver `tags` em ordem alfabética ordinal em toda `TodoResponse` (criação, edição, `GET /{id}`, listagem, `GET /pending/oldest`). <!-- TAG-04 -->
5. WHEN o cliente faz `PUT /api/v1/todos/{id}` com `tags: ["trabalho"]` numa tarefa com `["casa", "urgente"]` THEN o sistema SHALL responder 200 com `tags: ["trabalho"]`. <!-- TAG-05 -->
6. WHEN o cliente faz `PUT /api/v1/todos/{id}` com `tags: []` THEN o sistema SHALL responder 200 com `tags: []`. <!-- TAG-06 -->
7. WHEN o cliente faz `PUT /api/v1/todos/{id}` sem o campo `tags` (ou com `tags: null`) THEN o sistema SHALL manter as tags que a tarefa já tinha. <!-- TAG-07 -->
8. WHEN o cliente faz `GET /api/v1/todos/{id}` depois de gravar tags THEN o sistema SHALL devolver as mesmas tags gravadas (persistidas no banco). <!-- TAG-08 -->
9. The system SHALL aceitar uma tag com letras acentuadas, dígitos, `-` e `_`, por exemplo `"reunião-2026_q1"`. <!-- TAG-09 -->

**Independent Test**: Criar uma tarefa com `tags: ["Casa"]`, ler por id e ver `["casa"]`; editar com `PUT` sem `tags` e ver `["casa"]` mantido.

---

### P1: Rejeitar tags inválidas ⭐ MVP

**User Story**: Como cliente da API, quero um erro claro quando envio uma tag fora das regras para corrigir a requisição.

**Why P1**: As regras de formato fazem parte do contrato desde o primeiro dia.

**Acceptance Criteria**:

1. IF uma tag é vazia, só espaços ou `null` THEN o sistema SHALL responder 400 ProblemDetails com `detail` `"A tag não pode ser vazia."` e não gravar nada. <!-- TAG-10 -->
2. IF uma tag tem mais de 30 caracteres depois do trim THEN o sistema SHALL responder 400 ProblemDetails com `detail` `"A tag '<tag>' deve ter no máximo 30 caracteres."`. <!-- TAG-11 -->
3. WHEN uma tag tem exatamente 30 caracteres THEN o sistema SHALL aceitá-la. <!-- TAG-12 -->
4. IF uma tag contém caractere fora de letras, dígitos, `-` e `_` (por exemplo espaço interno, `#`, `.`) THEN o sistema SHALL responder 400 ProblemDetails com `detail` `"A tag '<tag>' contém caracteres inválidos. Use letras, dígitos, '-' ou '_'."`. <!-- TAG-13 -->
5. IF a tarefa ficaria com mais de 10 tags distintas THEN o sistema SHALL responder 400 ProblemDetails com `detail` `"A tarefa pode ter no máximo 10 tags."`. <!-- TAG-14 -->
6. WHEN a lista tem 11 entradas que viram 10 tags distintas depois da normalização THEN o sistema SHALL aceitá-la. <!-- TAG-15 -->
7. IF um `PUT` é rejeitado por tag inválida THEN o sistema SHALL manter a tarefa inteira como estava (título, descrição e tags). <!-- TAG-16 -->

**Independent Test**: `POST` com `tags: ["a b"]` devolve 400 com a mensagem de caracteres inválidos, e `GET /api/v1/todos` não mostra tarefa nova.

---

### P1: Filtrar a listagem por tags ⭐ MVP

**User Story**: Como cliente da API, quero listar só as tarefas de um contexto para ver o que é de "casa" ou de "trabalho".

**Why P1**: Marcar sem poder filtrar não entrega o valor da feature.

**Acceptance Criteria**:

1. WHEN o cliente faz `GET /api/v1/todos?tags=casa` THEN o sistema SHALL devolver só as tarefas que têm a tag `casa`. <!-- TAG-17 -->
2. WHEN o cliente faz `GET /api/v1/todos?tags=casa&tags=urgente` THEN o sistema SHALL devolver só as tarefas que têm `casa` E `urgente`. <!-- TAG-18 -->
3. WHEN o valor do filtro é `" CASA "` THEN o sistema SHALL tratá-lo como `casa`. <!-- TAG-19 -->
4. WHEN o filtro `tags` vem junto com `isCompleted`, `priority` ou `search` THEN o sistema SHALL devolver só as tarefas que atendem a todos os filtros. <!-- TAG-20 -->
5. WHEN o filtro `tags` está presente THEN o sistema SHALL calcular `totalCount` e a paginação sobre as tarefas filtradas, na ordem `createdAt desc, id desc`. <!-- TAG-21 -->
6. WHEN todos os valores de `tags` estão em branco (`?tags=` ou `?tags=%20`) THEN o sistema SHALL listar como se o filtro não existisse. <!-- TAG-22 -->
7. WHEN nenhuma tarefa tem a tag pedida THEN o sistema SHALL responder 200 com `items: []` e `totalCount: 0`. <!-- TAG-23 -->
8. The system SHALL casar a tag do filtro só com a tag inteira: `?tags=casa` não devolve uma tarefa que só tem `casamento`. <!-- TAG-24 -->
9. WHEN o filtro `tags` traz mais de 10 tags distintas depois da normalização THEN o sistema SHALL responder 200 com `items: []` e `totalCount: 0`, sem consultar o banco. <!-- TAG-28 -->

**Independent Test**: Criar três tarefas (`[casa]`, `[casa, urgente]`, `[trabalho]`); `?tags=casa` devolve duas, `?tags=casa&tags=urgente` devolve uma.

---

## Edge Cases

- IF o corpo traz `tags` com tipo errado (por exemplo `"tags": "casa"`) THEN o sistema SHALL responder 400, como já faz hoje para JSON inválido. <!-- TAG-25 -->
- WHEN uma tarefa com tags é removida (`DELETE /api/v1/todos/{id}`) THEN o sistema SHALL responder 204 e a tag deixa de aparecer em qualquer filtro que só ela satisfazia. <!-- TAG-26 -->
- WHEN `Complete` ou `Reopen` é chamado numa tarefa com tags THEN o sistema SHALL manter as tags inalteradas. <!-- TAG-27 -->

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| TAG-01 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-02 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-03 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-04 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-05 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-06 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-07 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-08 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-09 | P1: Atribuir tags | Tasks | In Tasks |
| TAG-10 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-11 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-12 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-13 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-14 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-15 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-16 | P1: Rejeitar tags inválidas | Tasks | In Tasks |
| TAG-17 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-18 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-19 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-20 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-21 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-22 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-23 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-24 | P1: Filtrar por tags | Tasks | In Tasks |
| TAG-25 | Edge Cases | Tasks | In Tasks |
| TAG-26 | Edge Cases | Tasks | In Tasks |
| TAG-27 | Edge Cases | Tasks | In Tasks |
| TAG-28 | P1: Filtrar por tags | Tasks | In Tasks |

**Coverage:** 28 total, 28 mapped to tasks (T1-T6, TAG-28 na correção do finding F1) e checks C1-C28, 0 unmapped

---

## Sources

- Pedido do usuário: "adicionar tags às tarefas do TODO" - define a feature.
- Respostas do usuário às quatro perguntas de produto (2026-11-26) - modelo, edição, filtro e regras; registradas em Assumptions e em `context.md`.
- Código atual: `TodoItem`, `CreateTodoRequest`, `UpdateTodoRequest`, `TodoQuery`, `TodoRepository.ListAsync`, `GlobalExceptionHandler` (`DomainException` → 400).

---

## Success Criteria

- [ ] Todos os 27 critérios têm um teste (unitário ou de integração) que falha se o critério for falso.
- [ ] Os testes atuais de `POST`/`PUT` sem `tags` continuam passando sem alteração.
- [ ] A migration nova aplica num banco existente e as tarefas antigas aparecem com `tags: []`.
