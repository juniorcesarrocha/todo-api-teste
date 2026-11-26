# Tags de Tarefas Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, the walk, check_proofs, the Verifier and the screens sub-agent).

**If the skill cannot be activated, STOP and tell the user - do not proceed without it.**

---

**Spec**: `.specs/features/todo-tags/spec.md`
**Design**: inline abaixo (sem `design.md`: só um ponto de arquitetura, a forma de persistir as tags)
**Status**: Approved
**Approved**: 2026-11-26 - user: "Aprovo as tasks"
**Build**: `dotnet build`
**Run integration**: `dotnet test tests/TodoApi.IntegrationTests --filter "FullyQualifiedName~TodoTagsEndpointsTests" --logger "console;verbosity=normal"`

O `**Build**` é só `dotnet build`. `TreatWarningsAsErrors=true` já faz o papel de lint. O `dotnet format --verify-no-changes` fica de fora porque já falha no código atual (FINALNEWLINE em 5 arquivos de teste), antes desta feature.

Todos os checks usam `run integration`, não uma prova isolada, porque o `dotnet test` sai com 0 quando o filtro não seleciona nenhum teste. A linha de aprovação vem do logger em pt-BR: `Aprovado <nome totalmente qualificado>`.

---

## Design (inline)

- **Domínio.** `TodoItem` ganha `IReadOnlyList<string> Tags`, guardado num `List<string>` privado, e as constantes `TagMaxLength = 30` e `MaxTags = 10`. `Create` e `Update` recebem `IEnumerable<string?>? tags` antes de `now`. No `Create`, `null` vira lista vazia. No `Update`, `null` mantém as tags atuais. A normalização segue esta ordem: trim, `ToLowerInvariant`, valida o formato com `^[\p{L}\p{Nd}_-]+$`, remove repetidas, valida a quantidade e ordena (ordinal). Tudo é validado antes de atribuir qualquer campo. Por isso um `Update` rejeitado não altera nada (TAG-16). Os erros são `DomainException` com as mensagens da spec, e o `GlobalExceptionHandler` já os converte em 400 com `detail`.
- **Persistência.** As tags ficam numa *primitive collection* do EF Core, uma coluna JSON `Tags TEXT NOT NULL DEFAULT '[]'` na tabela `Todos`. Uma tabela `TodoTags` não foi escolhida porque o filtro exato `t.Tags.Contains(tag)` já é traduzido para `json_each` no SQLite. Assim são uma coluna e uma migration, sem join, e a remoção em cascata (TAG-26) vem de graça. A contrapartida é que o filtro não usa índice. Isso é aceitável no volume de uma lista de TODO. Se a forma de mapear o campo privado não funcionar no EF 10, a T5 para e me pergunta antes de mudar a abordagem.
- **Filtro.** `TodoQuery` ganha `string[]? Tags`. O minimal API faz o bind de `?tags=a&tags=b` em array. `Normalize()` aplica trim e minúsculas, descarta os valores em branco e remove repetidos, e uma lista vazia vira `null`. O repositório aplica um `Where(t => t.Tags.Contains(tag))` por tag, o que dá a combinação E com tag inteira.
- **Contrato.** `CreateTodoRequest` e `UpdateTodoRequest` ganham `IReadOnlyList<string?>? Tags = null` no fim. `TodoResponse` ganha `IReadOnlyList<string> Tags` no fim. Clientes atuais continuam compatíveis.

---

## Test Coverage Matrix

> Generated from codebase, project guidelines, and spec - confirm before Execute. Guidelines found: `CLAUDE.md`, `.claude/rules/testing.md` (fakes escritos à mão, sem biblioteca de mock; integração via `TodoApiFactory` com SQLite em memória e migrations reais). Sem limite de cobertura configurado.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| ---------- | ------------------ | -------------------- | ---------------- | ----------- |
| Domain (`TodoItem`) | unit | Todos os ramos da normalização/validação; cada regra de TAG-01..16 e TAG-27 com um teste | `tests/TodoApi.UnitTests/Domain/TodoItemTagsTests.cs` | `dotnet test tests/TodoApi.UnitTests --filter "FullyQualifiedName~Tags"` |
| Application (`TodoService`, `TodoQuery`, mapping) | unit | Create/Update repassam tags; `null` no Update mantém; `Normalize` do filtro | `tests/TodoApi.UnitTests/Application/*TagsTests.cs` | `dotnet test tests/TodoApi.UnitTests --filter "FullyQualifiedName~Tags"` |
| Infrastructure (config EF, migration, `TodoRepository`) | integration | Persistência ida e volta; filtro E, tag inteira, paginação e combinação com outros filtros | `tests/TodoApi.IntegrationTests/TodoTagsEndpointsTests.cs` | `dotnet test tests/TodoApi.IntegrationTests --filter "FullyQualifiedName~TodoTagsEndpointsTests"` |
| Api (endpoints, bind da query) | integration | Todo critério da spec pela API: caminho feliz, bordas e 400s | `tests/TodoApi.IntegrationTests/TodoTagsEndpointsTests.cs` | idem |
| `TodoApi.Api.http` | none | - (build gate only) | - | build gate only |

## Gate Check Commands

> Generated from codebase - confirm before Execute.

| Gate Level | When to Use | Command |
| ---------- | ----------- | ------- |
| Quick | Tarefas só com teste unitário | `dotnet build; dotnet test tests/TodoApi.UnitTests` |
| Full | Tarefas com teste de integração | `dotnet build; dotnet test` |
| Build | Tarefas sem teste, e o fim de cada fase | `dotnet build; dotnet test` |

Os gates rodam a suíte inteira, não só os testes de tags, para que uma regressão nos testes atuais apareça na hora.

---

## Execution Plan

### Phase 1: Domain

```
T1
```

### Phase 2: Application

```
T2 → T3 → T4
```

### Phase 3: Infrastructure + API

```
T5 → T6 → T7
```

---

## Task Breakdown

### T1: Tags no `TodoItem`

**What**: `TodoItem` ganha `Tags`, as constantes de limite e a normalização/validação em `Create`/`Update`. Os chamadores atuais passam `null`.
**Where**: `src/TodoApi.Domain/Entities/TodoItem.cs` (os chamadores em `TodoService.cs` e `TodoItemTests.cs`/`TodoServiceTests.cs` só ganham o argumento novo)
**Depends on**: None
**Reuses**: padrão de `ApplyChanges` e mensagens de `DomainException` já existentes
**Requirement**: TAG-01, TAG-02, TAG-03, TAG-04, TAG-07, TAG-09, TAG-10, TAG-11, TAG-12, TAG-13, TAG-14, TAG-15, TAG-16, TAG-27

**Done when**:

- [x] `TodoItemTagsTests` cobre: normaliza caixa e espaços; `null` no Create vira vazia; remove repetidas; ordena; `null` no Update mantém; acentos/dígitos/`-`/`_` aceitos; vazia/espaços/`null` → mensagem de vazia; 31 caracteres → mensagem de tamanho; 30 aceito; espaço interno, `#` e `.` → mensagem de caracteres; 11 distintas → mensagem de limite; 11 entradas com 10 distintas aceitas; Update rejeitado mantém título e tags; Complete/Reopen mantêm tags
- [x] Os testes atuais de `TodoItemTests` e `TodoServiceTests` passam sem mudança de asserção
- [x] Gate check passes: `dotnet build; dotnet test tests/TodoApi.UnitTests`
- [x] Test count: os 22 testes unitários atuais + os novos, nenhum removido

**Tests**: unit
**Gate**: quick

**Commit**: `feat(domain): adiciona tags normalizadas ao TodoItem`

**Status**: ✅ Done - 42 unitários (22 + 20 novos), 17 integração.
Desvios: `Tags` é propriedade `IReadOnlyList<string>` com `private set` (não campo `List` privado), para a T5 consultar `t.Tags.Contains`. `TodoItemConfiguration` ganhou `builder.Ignore(t => t.Tags)` temporário: sem ele o EF acusa `PendingModelChangesWarning` e os 17 testes de integração quebram até a T5.

---

### T2: `Tags` na `TodoResponse`

**What**: `TodoResponse` ganha `Tags` e o `ToResponse` passa a copiar `item.Tags`.
**Where**: `src/TodoApi.Application/Todos/Dtos/TodoResponse.cs` (+ `TodoMappings.cs`, uma linha)
**Depends on**: T1
**Reuses**: `TodoMappings.ToResponse`
**Requirement**: TAG-04

**Done when**:

- [x] Teste unitário em `TodoServiceTagsTests`: `GetByIdAsync` devolve as tags da entidade em ordem
- [x] Gate check passes: `dotnet build; dotnet test tests/TodoApi.UnitTests`
- [x] Test count: nenhum teste removido

**Tests**: unit
**Gate**: quick

**Commit**: `feat(application): expõe tags na TodoResponse`

**Status**: ✅ Done - 43 unitários, 17 integração.

---

### T3: `Tags` nos requests e no `TodoService`

**What**: `CreateTodoRequest` e `UpdateTodoRequest` ganham `Tags = null`, e o `TodoService` repassa `request.Tags` para `Create`/`Update`.
**Where**: `src/TodoApi.Application/Todos/TodoService.cs` (+ os dois records de request)
**Depends on**: T2
**Reuses**: `CreateAsync`/`UpdateAsync` atuais
**Requirement**: TAG-01, TAG-02, TAG-05, TAG-06, TAG-07

**Done when**:

- [x] `TodoServiceTagsTests`: Create com tags devolve normalizadas; Create sem tags devolve `[]`; Update com lista substitui; Update com `[]` limpa; Update com `null` mantém
- [x] Gate check passes: `dotnet build; dotnet test tests/TodoApi.UnitTests`
- [x] Test count: nenhum teste removido

**Tests**: unit
**Gate**: quick

**Commit**: `feat(application): aceita tags na criação e edição de tarefas`

**Status**: ✅ Done - 48 unitários, 17 integração.

---

### T4: Filtro `Tags` no `TodoQuery`

**What**: `TodoQuery` ganha `string[]? Tags`, `Normalize()` aplica trim e minúsculas, descarta os valores em branco e remove repetidos (lista vazia → `null`), e o `InMemoryTodoRepository` passa a aplicar o filtro E.
**Where**: `src/TodoApi.Application/Todos/Dtos/TodoQuery.cs` (+ `tests/TodoApi.UnitTests/Fakes/InMemoryTodoRepository.cs`)
**Depends on**: T3
**Reuses**: `TodoQuery.Normalize`
**Requirement**: TAG-19, TAG-22

**Done when**:

- [x] `TodoQueryTagsTests`: `" CASA "` → `["casa"]`; `["", " "]` → `null`; `["a", "A"]` → `["a"]`
- [x] Gate check passes: `dotnet build; dotnet test tests/TodoApi.UnitTests`
- [x] Test count: nenhum teste removido

**Tests**: unit
**Gate**: quick

**Commit**: `feat(application): adiciona filtro de tags ao TodoQuery`

**Status**: ✅ Done - 52 unitários, 17 integração.

---

### T5: Persistir tags (mapeamento EF + migration)

**What**: `TodoItemConfiguration` mapeia `Tags` como primitive collection (coluna JSON, padrão `'[]'`), e a migration `AddTodoTags` é criada com a skill `criar-migration`.
**Where**: `src/TodoApi.Infrastructure/Persistence/Configurations/TodoItemConfiguration.cs` (+ arquivos gerados em `Persistence/Migrations`)
**Depends on**: T4
**Reuses**: skill `criar-migration`; `TodoApiFactory` (roda as migrations)
**Requirement**: TAG-01..TAG-16, TAG-25, TAG-27

**Done when**:

- [x] A migration adiciona só a coluna `Tags` com `defaultValue: "[]"`, e o `Down` a remove
- [x] `TodoTagsEndpointsTests` (novo) cobre, pela API, os critérios TAG-01..TAG-16, TAG-25 e TAG-27 com os nomes listados em `## Checks`
- [x] Gate check passes: `dotnet build; dotnet test`
- [x] Test count: todos os testes atuais + os novos, nenhum removido

**Tests**: integration
**Gate**: full

**Commit**: `feat(infrastructure): persiste tags das tarefas`

**Status**: ✅ Done - 52 unitários, 40 integração (17 + 23 novos). A migration usa `defaultValueSql: "'[]'"` (vem de `HasDefaultValueSql` na configuração), com o mesmo efeito de `defaultValue: "[]"`. O `Ignore` temporário da T1 saiu.

---

### T6: Filtrar por tags no `TodoRepository`

**What**: `TodoRepository.ListAsync` aplica um `Where(t => t.Tags.Contains(tag))` por tag do filtro, antes do `CountAsync` e da paginação.
**Where**: `src/TodoApi.Infrastructure/Repositories/TodoRepository.cs`
**Depends on**: T5
**Reuses**: estrutura de filtros de `ListAsync`
**Requirement**: TAG-17..TAG-24, TAG-26

**Done when**:

- [x] `TodoTagsEndpointsTests` cobre TAG-17..TAG-24 e TAG-26. Cada teste usa tags com um prefixo único, porque a classe compartilha um banco entre os testes
- [x] Gate check passes: `dotnet build; dotnet test`
- [x] Test count: nenhum teste removido

**Tests**: integration
**Gate**: full

**Commit**: `feat(infrastructure): filtra a listagem de tarefas por tags`

**Status**: ✅ Done - 52 unitários, 50 integração. Os 8 testes de filtro falharam antes da implementação e passaram depois.

---

### T7: Exemplos de tags no `.http`

**What**: `TodoApi.Api.http` ganha um `POST` com tags, um `PUT` com tags e um `GET ?tags=a&tags=b`.
**Where**: `src/TodoApi.Api/TodoApi.Api.http`
**Depends on**: T6
**Reuses**: requisições de exemplo existentes

**Done when**:

- [ ] Três requisições novas, no mesmo estilo das atuais
- [ ] Gate check passes: `dotnet build; dotnet test`

**Tests**: none
**Gate**: build

**Commit**: `docs(api): adiciona exemplos de tags ao TodoApi.Api.http`

---

## Checks

**C1** - `POST` com `["Casa", " urgente "]` responde 201 com `["casa", "urgente"]` (TAG-01, AC 1)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTags_ReturnsNormalizedTags

**C2** - `POST` sem `tags` responde 201 com `tags: []` (TAG-02, AC 2)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithoutTags_ReturnsEmptyTags

**C3** - `["casa", "Casa", "CASA"]` vira `["casa"]` (TAG-03, AC 3)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithDuplicateTags_KeepsOne

**C4** - Criação, edição, `GET /{id}`, listagem e `/pending/oldest` devolvem as tags em ordem ordinal (TAG-04, AC 4)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.AllResponses_ReturnTagsInOrdinalOrder

**C5** - `PUT` com `["trabalho"]` sobre `["casa", "urgente"]` responde 200 com `["trabalho"]` (TAG-05, AC 5)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithTags_ReplacesTags

**C6** - `PUT` com `tags: []` responde 200 com `[]` (TAG-06, AC 6)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithEmptyTags_ClearsTags

**C7** - `PUT` sem `tags` e com `tags: null` mantém as tags atuais (TAG-07, AC 7)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithoutTags_KeepsTags

**C8** - `GET /{id}` devolve as tags gravadas, lidas do banco (TAG-08, AC 8)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Get_AfterPost_ReturnsPersistedTags

**C9** - `"reunião-2026_q1"` é aceita (TAG-09, AC 9)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts

**C10** - Tag `""`, `"  "` ou `null` → 400 com `"A tag não pode ser vazia."`, e nenhuma tarefa é criada (TAG-10, AC 1)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400

**C11** - Tag de 31 caracteres → 400 com `"A tag '<tag>' deve ter no máximo 30 caracteres."` (TAG-11, AC 2)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagTooLong_Returns400

**C12** - Tag de exatamente 30 caracteres é aceita (TAG-12, AC 3)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagAtMaxLength_Accepts

**C13** - `"a b"`, `"#casa"` e `"a.b"` → 400 com a mensagem de caracteres inválidos (TAG-13, AC 4)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400

**C14** - 11 tags distintas → 400 com `"A tarefa pode ter no máximo 10 tags."` (TAG-14, AC 5)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithMoreThanTenTags_Returns400

**C15** - 11 entradas que viram 10 distintas são aceitas (TAG-15, AC 6)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithElevenEntriesTenDistinct_Accepts

**C16** - `PUT` com tag inválida responde 400 e o `GET` mostra título, descrição e tags como antes (TAG-16, AC 7)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithInvalidTag_KeepsTodoUnchanged

**C17** - `?tags=casa` devolve só as tarefas com `casa` (TAG-17, AC 1)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByOneTag_ReturnsOnlyTagged

**C18** - `?tags=casa&tags=urgente` devolve só as tarefas com as duas (TAG-18, AC 2)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTwoTags_RequiresAll

**C19** - `?tags=%20CASA%20` filtra como `casa` (TAG-19, AC 3)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterNormalizesValue

**C20** - `tags` combinado com `isCompleted`, `priority` e `search` devolve só o que atende a todos (TAG-20, AC 4)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTagsCombinesWithOtherFilters

**C21** - Com `tags`, `totalCount` e as páginas contam só as filtradas, em `createdAt desc, id desc` (TAG-21, AC 5)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTags_PaginatesFilteredResults

**C22** - `?tags=` e `?tags=%20` listam como sem filtro (TAG-22, AC 6)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_WithBlankTagFilter_IgnoresFilter

**C23** - Tag que ninguém tem → 200, `items: []`, `totalCount: 0` (TAG-23, AC 7)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByUnknownTag_ReturnsEmpty

**C24** - `?tags=casa` não devolve tarefa que só tem `casamento` (TAG-24, AC 8)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterMatchesWholeTagOnly

**C25** - `"tags": "casa"` (tipo errado) → 400 (TAG-25, Edge Case 1)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagsOfWrongType_Returns400

**C26** - `DELETE` de tarefa com tags → 204, e o filtro por uma tag que só ela tinha devolve `totalCount: 0` (TAG-26, Edge Case 2)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Delete_RemovesTaggedTodoFromFilter

**C27** - `complete` e `reopen` mantêm as tags (TAG-27, Edge Case 3)
Proof: run integration | Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.CompleteAndReopen_KeepTags

---

## Handoff

**Settled mid-build:** none
**Dispatched:** none
**Walked:** none

---

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3

Phase 1:  T1
Phase 2:  T2 ------→ T3 ------→ T4
Phase 3:  T5 ------→ T6 ------→ T7
```

7 tarefas cabem num lote só, então a execução é inline, sem sub-agentes de lote. O Verifier roda no fim, como sempre.

---

## Task Granularity Check

| Task | Scope | Status |
| ---- | ----- | ------ |
| T1: Tags no `TodoItem` | 1 entidade (chamadores só ganham um argumento) | ⚠️ OK - coeso |
| T2: `Tags` na `TodoResponse` | 1 record + 1 linha de mapping | ⚠️ OK - coeso |
| T3: Requests + service | 2 records + 2 chamadas no service | ⚠️ OK - coeso, um contrato |
| T4: Filtro no `TodoQuery` | 1 record (+ fake de teste) | ✅ Granular |
| T5: Mapeamento EF + migration | 1 configuração + migration gerada | ✅ Granular |
| T6: Filtro no repositório | 1 método | ✅ Granular |
| T7: Exemplos `.http` | 1 arquivo | ✅ Granular |

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
| ---- | ---------------------- | ------------- | ------ |
| T1 | None | início da Phase 1 | ✅ Match |
| T2 | T1 | Phase 1 → T2 | ✅ Match |
| T3 | T2 | T2 → T3 | ✅ Match |
| T4 | T3 | T3 → T4 | ✅ Match |
| T5 | T4 | Phase 2 → T5 | ✅ Match |
| T6 | T5 | T5 → T6 | ✅ Match |
| T7 | T6 | T6 → T7 | ✅ Match |

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
| ---- | --------------------------- | --------------- | --------- | ------ |
| T1 | Domain | unit | unit | ✅ OK |
| T2 | Application | unit | unit | ✅ OK |
| T3 | Application | unit | unit | ✅ OK |
| T4 | Application | unit | unit | ✅ OK |
| T5 | Infrastructure + Api | integration | integration | ✅ OK |
| T6 | Infrastructure | integration | integration | ✅ OK |
| T7 | `.http` | none | none | ✅ OK |
