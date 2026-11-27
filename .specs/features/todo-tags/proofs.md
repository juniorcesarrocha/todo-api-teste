# Tags de Tarefas Proofs

**HEAD**: ef743fe
**Round**: 2 (depois das correções dos findings F1-F4, F6, F7)
**Date**: 2026-11-26
**Runner**: execução manual equivalente ao check_proofs.py (Python ausente na máquina); grupos rodados um de cada vez, em bash. Um check passa quando a linha aparece e nenhuma linha `Com falha` do mesmo teste aparece (relevante para as Theories).

## Build

`dotnet build` → exit 0 (0 avisos, 0 erros)

## Run integration

`dotnet test tests/TodoApi.IntegrationTests --filter "FullyQualifiedName~TodoTagsEndpointsTests" --logger "console;verbosity=normal"` → exit 0, 39/39 aprovados

| Check | Result (linhas casadas) |
| ----- | ------ |
| C1 | PASS (1) |
| C2 | PASS (1) |
| C3 | PASS (1) |
| C4 | PASS (1) |
| C5 | PASS (1) |
| C6 | PASS (1) |
| C7 | PASS (2) |
| C8 | PASS (1) |
| C9 | PASS (5) |
| C10 | PASS (3) |
| C11 | PASS (1) |
| C12 | PASS (1) |
| C13 | PASS (3) |
| C14 | PASS (1) |
| C15 | PASS (1) |
| C16 | PASS (1) |
| C17 | PASS (1) |
| C18 | PASS (1) |
| C19 | PASS (1) |
| C20 | PASS (1) |
| C21 | PASS (1) |
| C22 | PASS (2) |
| C23 | PASS (1) |
| C24 | PASS (1) |
| C25 | PASS (1) |
| C26 | PASS (1) |
| C27 | PASS (1) |
| C28 | PASS (1) |
| C29 | PASS (1) |

### Output (tail)

```
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.AllResponses_ReturnTagsInOrdinalOrder [5 s]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_WithBlankTagFilter_IgnoresFilter(tagsQuery: "&tags=") [242 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_WithBlankTagFilter_IgnoresFilter(tagsQuery: "&tags=%20") [18 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts(tag: "reunião", expected: "reunião") [18 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts(tag: "𠀀", expected: "𠀀") [10 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts(tag: "हिंदी", expected: "हिंदी") [19 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts(tag: "AÇÃO", expected: "ação") [8 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts(tag: "reunião-2026_q1", expected: "reunião-2026_q1") [10 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithEmptyTags_ClearsTags [32 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTags_ReturnsNormalizedTags [12 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithDuplicateTags_KeepsOne [18 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagAtMaxLength_Accepts [11 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithoutTags_ReturnsEmptyTags [11 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithElevenEntriesTenDistinct_Accepts [14 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByOneTag_ReturnsOnlyTagged [118 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400(tagJson: "\"\"") [53 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400(tagJson: "\"   \"") [11 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400(tagJson: "null") [10 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithTags_ReplacesTags [17 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.CompleteAndReopen_KeepTags [66 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagTooLong_Returns400 [6 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithoutTags_KeepsTags(body: "{ \"title\": \"Manter\", \"priority\": \"Medium\" "···) [18 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithoutTags_KeepsTags(body: "{ \"title\": \"Manter\", \"priority\": \"Medium\","···) [17 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterMatchesWholeTagOnly [24 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Migration_ExistingTodos_GetEmptyTags [172 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByUnknownTag_ReturnsEmpty [15 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagsOfWrongType_Returns400 [16 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Get_AfterPost_ReturnsPersistedTags [24 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTags_PaginatesFilteredResults [53 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400(tag: "a.b") [7 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400(tag: "#casa") [5 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400(tag: "a b") [5 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterWithMoreThanTenDistinctTags_ReturnsEmpty [79 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Delete_RemovesTaggedTodoFromFilter [45 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithMoreThanTenTags_Returns400 [7 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithInvalidTag_KeepsTodoUnchanged [26 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTwoTags_RequiresAll [56 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTagsCombinesWithOtherFilters [66 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterNormalizesValue [27 ms]
Total de testes: 39
     Aprovados: 39
```

Rodada 1 (HEAD 14050fb): 27/27 checks verdes, 33/33 testes.
