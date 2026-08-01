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
  Structa.Camera          câmera 3D (view/projection) + navegação orbit/pan/zoom
  Structa.Editor          orquestração de ferramentas de edição, Scene (malhas da cena)
  Structa.Selection       picking por ray casting: vértices, arestas, faces, objetos, multi-seleção
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
- [x] **Etapa 03** — Navegação: órbita, pan, zoom, movimento suave (damping), sensibilidade configurável, atalho Home
- [x] **Etapa 04** — Seleção: picking por ray casting (vértice/aresta/face/objeto), multi-seleção, highlight
- [ ] **Etapa 05** — Ferramenta Linha — próxima
- [ ] Faces, Push/Pull, ferramentas de transformação, undo/redo, materiais, import/export, componentes, grupos, plugins

### Navegação (estilo SketchUp)

| Ação | Controle |
| --- | --- |
| Órbita | Arrastar com o botão do meio do mouse |
| Pan | Shift + arrastar com o botão do meio |
| Zoom | Roda do mouse |
| Resetar vista | Tecla `Home` |

### Seleção

| Ação | Controle |
| --- | --- |
| Selecionar | Clique com o botão esquerdo (modo definido na aba **Entidades** da barra lateral: Vértice/Aresta/Face/Objeto) |
| Somar à seleção | Shift + clique |
| Limpar seleção | Tecla `Esc` |

O conteúdo selecionável hoje é geometria de teste (um cubo e uma folha), só para validar o sistema — a criação de geometria pelo usuário começa na Etapa 05.

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
