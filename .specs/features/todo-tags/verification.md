# Tags de Tarefas verification

**Verdict**: FAIL
**Proofs**: `proofs.md` at 14050fb
**Verifier**: independent sub-agent (author != verifier)

Escopo lido: `spec.md`, `tasks.md` (`## Checks`, Test Coverage Matrix), `context.md`, `proofs.md` e `git diff 29060dc..14050fb`. Os exemplos próprios rodaram como testes descartáveis numa cópia (`git archive 14050fb`) fora do repositório; nada foi gravado na árvore de trabalho além deste arquivo.

## Checks

| Check | Test | Holds |
| --- | --- | --- |
| C1 | `TodoTagsEndpointsTests` `Post_WithTags_ReturnsNormalizedTags` | yes - igualdade exata com `["casa", "urgente"]` e 201 |
| C2 | `TodoTagsEndpointsTests` `Post_WithoutTags_ReturnsEmptyTags` | yes - corpo sem `tags`, verifica `"tags":[]` no JSON e 201 |
| C3 | `TodoTagsEndpointsTests` `Post_WithDuplicateTags_KeepsOne` | yes - resposta e `GET` seguinte |
| C4 | `TodoTagsEndpointsTests` `AllResponses_ReturnTagsInOrdinalOrder` | no - só usa ASCII minúsculo (`zeta/alfa/meio`, `zz/b/a`), onde ordem ordinal e ordem cultural coincidem; um `OrderBy` sem comparador ou com `StringComparer.InvariantCulture` passa |
| C5 | `TodoTagsEndpointsTests` `Put_WithTags_ReplacesTags` | yes - resposta e `GET` |
| C6 | `TodoTagsEndpointsTests` `Put_WithEmptyTags_ClearsTags` | yes - resposta e `GET` |
| C7 | `TodoTagsEndpointsTests` `Put_WithoutTags_KeepsTags` | yes - os dois corpos (sem campo e `null`), resposta e `GET` |
| C8 | `TodoTagsEndpointsTests` `Get_AfterPost_ReturnsPersistedTags` | yes - cada requisição usa um `DbContext` novo; com a coluna ignorada o `GET` devolveria `[]` |
| C9 | `TodoTagsEndpointsTests` `Post_WithAccentsDigitsHyphenUnderscore_Accepts` | yes - para o exemplo da spec (NFC); ver finding F3 para acentos em NFD |
| C10 | `TodoTagsEndpointsTests` `Post_WithBlankTag_Returns400` | yes - `""`, `"   "`, `null`, mensagem exata e nenhuma tarefa criada |
| C11 | `TodoTagsEndpointsTests` `Post_WithTagTooLong_Returns400` | no - 31 `a` sem espaços; uma implementação que mede o tamanho antes do trim passa, e o critério fixa "depois do trim" |
| C12 | `TodoTagsEndpointsTests` `Post_WithTagAtMaxLength_Accepts` | yes - 30 caracteres aceitos (o "depois do trim" fica no finding F4) |
| C13 | `TodoTagsEndpointsTests` `Post_WithInvalidCharacters_Returns400` | yes - os três exemplos da spec com a mensagem exata |
| C14 | `TodoTagsEndpointsTests` `Post_WithMoreThanTenTags_Returns400` | yes - 11 distintas, mensagem exata |
| C15 | `TodoTagsEndpointsTests` `Post_WithElevenEntriesTenDistinct_Accepts` | yes - 201 com 10 tags (asserção só da contagem, ver F6) |
| C16 | `TodoTagsEndpointsTests` `Put_WithInvalidTag_KeepsTodoUnchanged` | yes - título, descrição, prioridade, tags e `updatedAt` lidos do banco |
| C17 | `TodoTagsEndpointsTests` `List_FilterByOneTag_ReturnsOnlyTagged` | yes - contagem e ids na ordem, com tarefas distratoras |
| C18 | `TodoTagsEndpointsTests` `List_FilterByTwoTags_RequiresAll` | yes - distratores com só uma das tags (um OU devolveria 3) |
| C19 | `TodoTagsEndpointsTests` `List_FilterNormalizesValue` | yes - `%20CASA…%20` casa com a tag minúscula |
| C20 | `TodoTagsEndpointsTests` `List_FilterByTagsCombinesWithOtherFilters` | yes - um distrator por filtro (concluída, prioridade, sem tag, título) |
| C21 | `TodoTagsEndpointsTests` `List_FilterByTags_PaginatesFilteredResults` | yes - `totalCount`, `totalPages` e ids de cada página na ordem |
| C22 | `TodoTagsEndpointsTests` `List_WithBlankTagFilter_IgnoresFilter` | yes - `?tags=` e `?tags=%20`; tratar `""` como tag devolveria 0 |
| C23 | `TodoTagsEndpointsTests` `List_FilterByUnknownTag_ReturnsEmpty` | yes - 200, `items` vazio e `totalCount` 0 |
| C24 | `TodoTagsEndpointsTests` `List_FilterMatchesWholeTagOnly` | yes - `LIKE` por prefixo ou substring devolveria 2 |
| C25 | `TodoTagsEndpointsTests` `Post_WithTagsOfWrongType_Returns400` | yes - 400 e nenhuma tarefa criada |
| C26 | `TodoTagsEndpointsTests` `Delete_RemovesTaggedTodoFromFilter` | yes - 1 antes, 204, 0 depois |
| C27 | `TodoTagsEndpointsTests` `CompleteAndReopen_KeepTags` | yes - tags lidas do banco depois de cada transição |

**Resultado**: 25 checks valem, 2 não (C4, C11).

### Exemplos próprios (rodados em 14050fb, cópia descartável)

| Exemplo | Resultado | Avaliação |
| --- | --- | --- |
| `"AÇÃO"` | 201, `["ação"]` | ok |
| `"Работа"`, `"東京"`, `"٣٤"`, `"ǅ"` | 201 (`работа`, `東京`, `٣٤`, `ǆ`) | ok |
| `"_x"`, `"-x"`, `"---"` | 201 | ok pela spec (só `-`/`_` é permitido) |
| `"ação"` em NFD (`a` + U+0327, `a` + U+0303) | **400** caracteres inválidos | F3 |
| `"हिंदी"` (devanágari, com marcas combinantes) | **400** caracteres inválidos | F3 |
| `"𠀀"` (U+20000, par substituto) | **400** caracteres inválidos | F3 |
| `"İstanbul"` | 201, `["İstanbul"]` (o `İ` não vira minúsculo) | F5 |
| ordem de `["é","f","_a","a","B","-z","1"]` | `["-z","1","_a","a","b","f","é"]` (ordinal, correto) | correto hoje, mas nenhum teste trava isso (F2) |
| `?tags=x&tags=x` (repetida) | casa normalmente | ok |
| `?tags=ca_a…`, `?tags=%25…`, `?tags=%25`, `?tags=_` | 0 resultados (sem curinga) | ok |
| `PUT` com `"tags": [null]` | 400 "A tag não pode ser vazia.", tarefa intacta | ok |
| Banco migrado até `InitialCreate`, linha inserida, depois `AddTodoTags` | linha antiga lida com `Tags = []`, filtro funciona | ok, mas sem teste no suite (F7) |
| `GET /api/v1/todos` com 50/100/150/200 valores distintos de `tags` | 200 | ok |
| `GET /api/v1/todos` com 300 valores distintos de `tags` (~2,7 KB de query) | **Stack overflow - o processo inteiro cai** | F1 |

## Findings

| Finding | Severity | Evidence | Fix | Closed |
| --- | --- | --- | --- | --- |
| F1 - Um `GET /api/v1/todos` com ~300 valores distintos em `tags` derruba o processo da API com `StackOverflowException` (não capturável: não vira 500, mata o host). Nenhum check limita a quantidade de valores do filtro. | Critical | `src/TodoApi.Infrastructure/Repositories/TodoRepository.cs:39-42` - um `Where` encadeado por valor; o `ExpressionTreeFuncletizer` do EF recursa até estourar a pilha. `src/TodoApi.Application/Todos/Dtos/TodoQuery.cs:29-36` não limita a lista. Reproduzido com 300, 600, 900 e 1500 valores; 200 passa. Uma query de ~2,7 KB cabe no limite de linha do Kestrel (8 KB). | Em `TodoQuery.Normalize` (ou no repositório), se houver mais de `TodoItem.MaxTags` tags distintas no filtro, devolver lista vazia direto (nenhuma tarefa pode ter mais de 10 tags, então o E nunca casa) - ou rejeitar com 400. Trocar o laço por um predicado só (`t => tags.All(tag => t.Tags.Contains(tag))` ou contagem `t.Tags.Count(x => tags.Contains(x)) == tags.Length`). Acrescentar critério novo (TAG-28) com teste de integração usando, por exemplo, 500 valores. | 6a8aca2 |
| F2 - C4 não distingue ordem ordinal de ordem cultural. | Major | `tests/TodoApi.IntegrationTests/TodoTagsEndpointsTests.cs:69,75` - entradas `zeta/alfa/meio` e `zz/b/a`, só ASCII minúsculo. Trocar o `SortedSet(StringComparer.Ordinal)` de `TodoItem.cs:133` por `StringComparer.InvariantCulture` ou `OrderBy(t => t)` mantém o teste verde. O unitário `Create_SortsTagsInOrdinalOrder` tem o mesmo problema. | Usar entradas em que as duas ordens divergem, por exemplo `["é", "f", "_a", "a", "-z", "1"]`, esperando `["-z", "1", "_a", "a", "f", "é"]`, em todas as respostas do C4. | c274654 |
| F3 - Letras Unicode com marcas combinantes ou fora do BMP são rejeitadas: `"ação"` em NFD, `"हिंदी"`, `"𠀀"` respondem 400, contra "letras Unicode" (Assumptions) e "letras acentuadas" (TAG-09). | Major | `src/TodoApi.Domain/Entities/TodoItem.cs:164` - `^[\p{L}\p{Nd}_-]+$` não inclui `\p{M}` e avalia unidades UTF-16 (surrogates são `\p{Cs}`); `TodoItem.cs:142` não normaliza para NFC. Um cliente que envia texto em NFD (comum vindo de macOS/iOS) recebe 400 para `"reunião"`. | Normalizar com `Normalize(NormalizationForm.FormC)` antes de validar, tanto em `TodoItem.NormalizeTags` quanto em `TodoQuery.NormalizeTags` (senão o filtro não casa); incluir `\p{M}` no padrão e validar por `Rune` (ou `EnumerateRunes`) para letras fora do BMP; decidir se o limite de 30 conta caracteres de texto ou unidades UTF-16. Teste com `"reunião"` esperando `["reunião"]`. | 5b276d9 |
| F4 - C11 não prova o "depois do trim": nenhum teste de integração manda uma tag com espaços em volta perto do limite. | Minor | `tests/TodoApi.IntegrationTests/TodoTagsEndpointsTests.cs:173,184` - `new string('a', 31)` e `new string('b', 30)` sem espaços. Medir `raw.Length` antes do `Trim` passa nos dois. O unitário `Create_WithTagAtMaxLength_Accepts` (`TodoItemTagsTests.cs:80`) cobre no domínio, mas não é a prova do check. | No C12 (ou num caso novo do C11), enviar `$"  {new string('b', 30)}  "` e esperar 201 com a tag de 30 caracteres. | c274654 |
| F5 - `ToLowerInvariant` não converte `İ` (U+0130): `"İstanbul"` é gravada como `"İstanbul"`, diferente de `"istanbul"`. É o comportamento da cultura invariante, mas contraria o objetivo "evita 'Casa' e 'casa' como tags diferentes". | Minor | `src/TodoApi.Domain/Entities/TodoItem.cs:142` e `src/TodoApi.Application/Todos/Dtos/TodoQuery.cs:33` | Decisão de produto: aceitar como está e registrar na spec, ou usar case folding (por exemplo `ToLowerInvariant` depois de mapear U+0130 para `i`). | aberto - decisão de produto com o usuário |
| F6 - C15 só confere a contagem (10), não quais tags ficaram. | Minor | `tests/TodoApi.IntegrationTests/TodoTagsEndpointsTests.cs:224` - `Assert.Equal(10, ….Tags.Count)` | Conferir a lista exata ordenada (`t1, t10, t2, …, t9`). | c274654 |
| F7 - Nenhum teste prova que tarefas que já existiam ficam com `tags: []` depois da migration (Success Criteria e Assumptions). A sondagem passou, mas uma migration com `defaultValue` errado não seria pega. | Minor | `src/TodoApi.Infrastructure/Persistence/Migrations/20261126183902_AddTodoTags.cs:13-18`; nenhum check cobre | Teste de integração que migra até `20261102201317_InitialCreate`, insere uma linha por SQL, aplica `AddTodoTags` e confere `GET /{id}` com `tags: []`. | ef743fe |

## Round 2

**Round**: 2 - `proofs.md` em ef743fe: build verde, 29/29 checks (C28 para TAG-28, novo do F1; C29 para o F7), 39/39 testes do grupo. Sem segundo Verifier, como manda o fluxo.

- F1, F2, F3, F4, F6, F7: fechados pelos commits da coluna `Closed`. C4 e C11, que não valiam na rodada 1, agora usam entradas que distinguem ordinal de cultural e espaços em volta do limite.
- F5: aberto, aguardando a decisão do usuário (como tratar `İ` U+0130).
