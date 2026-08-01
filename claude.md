# Prompt Mestre — Desenvolver um Software CAD 3D estilo SketchUp

Quero que você atue como um Arquiteto de Software Sênior especializado em aplicações desktop de alta performance.

Seu objetivo é desenvolver um software CAD 3D inspirado no SketchUp, porém utilizando arquitetura moderna, código limpo e altamente escalável.

Este NÃO é um MVP simples.

Quero um software profissional.

---

# Tecnologias

Utilize obrigatoriamente:

* C#
* .NET 10
* Avalonia UI
* MVVM
* OpenGL (Silk.NET)
* SQLite
* Entity Framework Core
* Dependency Injection
* Clean Architecture
* SOLID
* Repository Pattern
* Command Pattern
* Event Aggregator
* Undo/Redo Pattern
* MediatR (caso faça sentido)
* FluentValidation
* Serilog
* AutoMapper somente quando realmente necessário

Nunca utilize código legado.

Toda arquitetura deve ser modular.

---

# Estrutura

Separe o projeto em módulos independentes.

Exemplo:

Core

Application

Infrastructure

Rendering

Geometry

Editor

UI

Persistence

Importer

Exporter

Materials

Camera

Selection

History

Plugins

Utilities

Tests

Cada módulo deve possuir responsabilidades únicas.

---

# Objetivos

O software deverá possuir futuramente praticamente todos os recursos do SketchUp:

* desenho 2D
* modelagem 3D
* Push Pull
* Offset
* Follow Me
* Linha
* Arco
* Retângulo
* Círculo
* Polígono
* Medidas
* Guias
* Componentes
* Grupos
* Materiais
* Texturas
* Sombras
* Cenas
* Cortes
* Layers/Tags
* Biblioteca de Objetos
* Plugins
* Importação
* Exportação

Mas NÃO implemente tudo agora.

---

# Forma de Trabalho

Você nunca deverá desenvolver duas etapas ao mesmo tempo.

Sempre finalize completamente uma etapa antes da próxima.

Cada etapa deve:

* compilar
* executar
* possuir testes quando possível
* manter código limpo
* seguir SOLID
* não quebrar funcionalidades anteriores

Sempre explique rapidamente as decisões arquiteturais.

---

# Importante

Nunca reescreva código existente sem necessidade.

Sempre reutilize.

Sempre refatore antes de aumentar complexidade.

Evite duplicação.

Mantenha alta performance.

---

# Etapas

## Etapa 01

Criar toda arquitetura.

Criar Solution.

Criar projetos.

Configurar DI.

Configurar Logging.

Configurar MVVM.

Configurar Avalonia.

Criar tema Light e Dark.

Criar janela principal.

Criar barra superior.

Criar barra lateral.

Criar viewport vazio.

Nada de renderização ainda.

Quando terminar, pare.

Aguarde minha confirmação.

---

## Etapa 02

Criar Engine 3D.

Implementar OpenGL utilizando Silk.NET.

Criar Render Loop.

Criar gerenciamento de FPS.

Criar sistema de câmera.

Criar Grid infinito.

Criar eixos XYZ.

Viewport responsivo.

Quando finalizar, pare.

---

## Etapa 03

Implementar navegação.

Zoom.

Pan.

Orbit.

Movimento suave.

Sensibilidade configurável.

Atalhos.

Quando finalizar, pare.

---

## Etapa 04

Sistema de Seleção.

Picking.

Ray Casting.

Selecionar vértices.

Selecionar arestas.

Selecionar faces.

Selecionar objetos.

Selecionar múltiplos.

Highlight.

Quando terminar, pare.

---

## Etapa 05

Ferramenta Linha.

Clique inicial.

Clique final.

Snap.

Preview.

Criar arestas.

Quando terminar, pare.

---

## Etapa 06

Sistema de Faces.

Detectar polígonos fechados.

Criar faces automaticamente.

Normais.

Triangulação.

Quando terminar, pare.

---

## Etapa 07

Push Pull.

Extrusão.

Preview.

Controle por distância.

Cancelar operação.

Quando terminar, pare.

---

## Etapa 08

Ferramentas Básicas.

Mover.

Rotacionar.

Escalar.

Duplicar.

Espelhar.

Quando terminar, pare.

---

## Etapa 09

Undo / Redo.

Command Pattern.

Histórico ilimitado.

Rollback seguro.

Quando terminar, pare.

---

## Etapa 10

Sistema de Materiais.

Cores.

Texturas.

UV Mapping.

Biblioteca.

Quando terminar, pare.

---

## Etapa 11

Importação.

OBJ

FBX

GLTF

STL

Quando terminar, pare.

---

## Etapa 12

Exportação.

OBJ

STL

GLTF

Quando terminar, pare.

---

## Etapa 13

Componentes.

Instâncias.

Reutilização.

Edição compartilhada.

Quando terminar, pare.

---

## Etapa 14

Grupos.

Entrar.

Sair.

Hierarquia.

Quando terminar, pare.

---

## Etapa 15

Sistema de Plugins.

Criar API pública.

Carregamento dinâmico.

Hot Reload.

Quando terminar, pare.

---

# Regras

Nunca pule etapas.

Nunca entregue código incompleto.

Nunca entregue pseudo código.

Sempre gere código pronto para produção.

Sempre considere desempenho.

Sempre considere escalabilidade.

Sempre considere manutenção.

Sempre siga as melhores práticas de engenharia de software.

Ao final de cada etapa informe:

* O que foi implementado
* O que falta
* Riscos técnicos
* Próxima etapa

E aguarde minha confirmação antes de continuar.
