# Tags de Tarefas Context

**Gathered:** 2026-11-26
**Spec:** `.specs/features/todo-tags/spec.md`
**Status:** Aguardando aprovação da spec

---

## Feature Boundary

Tags livres na tarefa: gravadas e devolvidas pelos endpoints de tarefa e usadas como filtro na listagem. Nada de cadastro de tags.

---

## Implementation Decisions

### Modelo

- Tag é rótulo livre ligado à tarefa, sem cadastro próprio. User: "Rótulo livre (Recomendado)".

### Edição

- Campo `tags` no `POST` e no `PUT`. No `PUT`, a lista enviada substitui a anterior; `tags` ausente mantém as atuais. User: "Campo tags no POST/PUT (Recomendado)".

### Filtro

- `?tags=a&tags=b`, combinação E. User: "Várias tags, E (Recomendado)".

### Regras

- Trim, minúsculas, repetidas descartadas; letras, dígitos, `-` e `_`; 1 a 30 caracteres; até 10 por tarefa. User: "Minúsculas, 30 chars, 10/tarefa (Recomendado)".

### Agent's Discretion

Nenhuma área delegada.

### Declined / Undiscussed Gray Areas → Assumptions

Ordem na resposta, `tags` ausente no `POST`, normalização do filtro, tarefas antigas, remoção em cascata: padrões do agente na tabela de Assumptions da spec (marcados `n`).

---

## Specific References

No specific requirements - open to standard approaches.

---

## Deferred Ideas

- Endpoint que lista as tags distintas em uso, com contagem.
- Cadastro de tags (renomear, cor).
