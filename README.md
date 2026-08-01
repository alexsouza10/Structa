# Structa

Structa é um software CAD 3D desktop inspirado no SketchUp, construído com arquitetura moderna, modular e escalável.

Este não é um MVP: o objetivo é uma base de engenharia sólida — Clean Architecture, SOLID, DI, testes — sobre a qual as ferramentas de modelagem 3D vão sendo adicionadas de forma incremental, uma etapa por vez.

## Stack

- **C# / .NET 10**
- **Avalonia UI** — interface desktop multiplataforma, MVVM (CommunityToolkit.Mvvm)
- **OpenGL via Silk.NET** — engine de renderização 3D
- **SQLite + Entity Framework Core** — persistência local
- **MediatR** — Command/Query pattern
- **FluentValidation** — validação de commands
- **Serilog** — logging estruturado (console + arquivo)

## Arquitetura

Solution organizada em módulos independentes, cada um com responsabilidade única, seguindo Clean Architecture:

```
src/
  Structa.Core            entidades e contratos de domínio (sem dependências)
  Structa.Application     casos de uso (MediatR), validação, portas de repositório
  Structa.Infrastructure  DI, EventAggregator, logging
  Structa.Persistence     EF Core + SQLite, implementação dos repositórios
  Structa.Geometry        primitivas geométricas
  Structa.Rendering       engine OpenGL (Silk.NET): shaders, buffers, grid, eixos, FPS
  Structa.Camera          câmera 3D (view/projection)
  Structa.Editor          orquestração de ferramentas de edição
  Structa.Selection       seleção de entidades (picking)
  Structa.History         undo/redo (Command Pattern)
  Structa.Materials       materiais e texturas
  Structa.Importer        importação de modelos (OBJ, FBX, glTF, STL)
  Structa.Exporter        exportação de modelos
  Structa.Plugins         API de plugins
  Structa.Utilities       utilitários compartilhados
  Structa.UI              app Avalonia (composition root): Views, ViewModels, temas

tests/
  Structa.Core.Tests
  Structa.Application.Tests
```

A dependência flui sempre para dentro (Core não depende de nada; UI depende de tudo). Módulos de ferramentas futuras (Geometry, Rendering, Selection etc.) só ganham implementação quando a etapa correspondente do roadmap é alcançada — até lá existem como projetos vazios, prontos para receber código.

## Status atual

- [x] **Etapa 01** — Arquitetura completa, DI, logging, MVVM, temas Light/Dark, janela principal (barra superior, barra lateral, viewport)
- [x] **Etapa 02** — Engine 3D: render loop, FPS, sistema de câmera, grid infinito, eixos XYZ
- [ ] **Etapa 03** — Navegação (zoom, pan, orbit) — próxima
- [ ] Seleção / picking, ferramentas de desenho (linha, arco, retângulo, círculo), faces, Push/Pull, ferramentas de transformação, undo/redo, materiais, import/export, componentes, grupos, plugins

## Como rodar

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet build Structa.slnx
dotnet run --project src/Structa.UI/Structa.UI.csproj
```

## Testes

```bash
dotnet test Structa.slnx
```

## Persistência local

O app grava preferências (ex.: tema) em um banco SQLite local, criado automaticamente em:

```
%LocalAppData%\Structa\structa.db
```

Logs ficam em `logs/` dentro da pasta de saída do build.
