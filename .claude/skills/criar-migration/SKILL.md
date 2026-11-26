---
name: criar-migration
description: Cria uma migration do EF Core neste projeto (TodoApi). Use sempre que alterar o schema do banco — adicionar/remover/renomear entidades, propriedades, índices ou configurações em Persistence/Configurations, ou mudar convenções do TodoDbContext — e quando o usuário pedir para "criar/gerar/adicionar uma migration".
---

# Criar migration do EF Core

O `TodoDbContext` fica em `src/TodoApi.Infrastructure` e as migrations ficam em
`src/TodoApi.Infrastructure/Persistence/Migrations`. O projeto de inicialização é
`src/TodoApi.Api`.

## Passo a passo

1. **Restaure as ferramentas locais primeiro.** O `dotnet-ef` é uma ferramenta local
   fixada em `dotnet-tools.json` na raiz do repositório, então ele só fica disponível
   depois do restore:

   ```powershell
   dotnet tool restore
   ```

2. **Gere a migration** (substitua `<Nome>` por um nome descritivo em PascalCase,
   por exemplo `AddPrioridadeToTodoItem`):

   ```powershell
   dotnet ef migrations add <Nome> --project src/TodoApi.Infrastructure --startup-project src/TodoApi.Api --output-dir Persistence/Migrations
   ```

   - `--project src/TodoApi.Infrastructure`: onde estão o `DbContext` e as migrations.
   - `--startup-project src/TodoApi.Api`: projeto que monta o host e a configuração.
   - `--output-dir Persistence/Migrations`: mantém as migrations na pasta já existente.

3. **Revise o arquivo gerado** e o `TodoDbContextModelSnapshot`. Confira se a migration
   contém só as mudanças esperadas.

4. **Valide:**

   ```powershell
   dotnet build
   dotnet test
   ```

   O build usa `TreatWarningsAsErrors=true`, então qualquer warning na migration quebra o build.

## Lembretes importantes

- **Mudou o schema? Precisa de migration — senão os testes de integração quebram.**
  O `TodoApiFactory` cria um SQLite em memória por factory e aplica as migrations
  em `CreateHost`. Se o modelo mudou e não existe migration correspondente, o banco
  dos testes fica com o schema antigo e os testes de integração falham.
- A convenção global que grava todo `DateTimeOffset` como binário/long também faz parte
  do schema: alterá-la exige uma migration.
- Em Development, a API aplica as migrations automaticamente ao iniciar
  (`dotnet run --project src/TodoApi.Api`). No ambiente `Testing` isso fica desligado.
- Para desfazer a última migration ainda não aplicada:

  ```powershell
  dotnet ef migrations remove --project src/TodoApi.Infrastructure --startup-project src/TodoApi.Api
  ```
