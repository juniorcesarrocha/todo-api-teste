# Tags de Tarefas Proofs

**HEAD**: 14050fb
**Date**: 2026-11-26
**Runner**: execução manual equivalente ao check_proofs.py (Python ausente na máquina); grupos rodados um de cada vez, em bash.

## Build

`dotnet build` → exit 0 (0 avisos, 0 erros)

## Run integration

`dotnet test tests/TodoApi.IntegrationTests --filter "FullyQualifiedName~TodoTagsEndpointsTests" --logger "console;verbosity=normal"` → exit 0, 33/33 aprovados

| Check | Result |
| ----- | ------ |
| C1 | PASS |
| C2 | PASS |
| C3 | PASS |
| C4 | PASS |
| C5 | PASS |
| C6 | PASS |
| C7 | PASS |
| C8 | PASS |
| C9 | PASS |
| C10 | PASS |
| C11 | PASS |
| C12 | PASS |
| C13 | PASS |
| C14 | PASS |
| C15 | PASS |
| C16 | PASS |
| C17 | PASS |
| C18 | PASS |
| C19 | PASS |
| C20 | PASS |
| C21 | PASS |
| C22 | PASS |
| C23 | PASS |
| C24 | PASS |
| C25 | PASS |
| C26 | PASS |
| C27 | PASS |

### Output (tail)

```
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.AllResponses_ReturnTagsInOrdinalOrder [4 s]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_WithBlankTagFilter_IgnoresFilter(tagsQuery: "&tags=") [166 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_WithBlankTagFilter_IgnoresFilter(tagsQuery: "&tags=%20") [14 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithEmptyTags_ClearsTags [22 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTags_ReturnsNormalizedTags [7 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithDuplicateTags_KeepsOne [11 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagAtMaxLength_Accepts [7 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithoutTags_ReturnsEmptyTags [8 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithElevenEntriesTenDistinct_Accepts [15 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByOneTag_ReturnsOnlyTagged [109 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400(tagJson: "\"\"") [52 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400(tagJson: "\"   \"") [8 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithBlankTag_Returns400(tagJson: "null") [8 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithTags_ReplacesTags [14 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.CompleteAndReopen_KeepTags [28 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagTooLong_Returns400 [5 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithoutTags_KeepsTags(body: "{ \"title\": \"Manter\", \"priority\": \"Medium\" "···) [14 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithoutTags_KeepsTags(body: "{ \"title\": \"Manter\", \"priority\": \"Medium\","···) [12 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterMatchesWholeTagOnly [19 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByUnknownTag_ReturnsEmpty [35 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithTagsOfWrongType_Returns400 [12 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Get_AfterPost_ReturnsPersistedTags [10 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTags_PaginatesFilteredResults [53 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400(tag: "a.b") [5 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400(tag: "#casa") [4 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithInvalidCharacters_Returns400(tag: "a b") [3 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Delete_RemovesTaggedTodoFromFilter [37 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithAccentsDigitsHyphenUnderscore_Accepts [8 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Post_WithMoreThanTenTags_Returns400 [6 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.Put_WithInvalidTag_KeepsTodoUnchanged [23 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTwoTags_RequiresAll [40 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterByTagsCombinesWithOtherFilters [58 ms]
  Aprovado TodoApi.IntegrationTests.TodoTagsEndpointsTests.List_FilterNormalizesValue [14 ms]
Total de testes: 33
     Aprovados: 33
```
